using System;
using ModIO;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This manages the UI for navigating a user through the Authentication process via email.
/// Note: Displaying the Terms of Use is not required when authenticating via email, but if you
/// intend to authenticate a user with a third party provider such as Steam or Google, you are
/// required to display the TOS for the user to agree to.
/// Note: you do not need a mod.io account setup to authenticate with your email address
/// </summary>
public class AuthenticationManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] GameObject AuthenticationMenu;
    [SerializeField] GameObject TOSPanel;
    [SerializeField] GameObject EmailPanel;
    [SerializeField] GameObject CodePanel;
    [SerializeField] GameObject InfoPanel;
    [SerializeField] GameObject LoadingPanel;
    
    [Header("Panel elements")]
    [SerializeField] InputField EmailInputField;
    [SerializeField] InputField CodeInputField;
    [SerializeField] Text TOSText;
    [SerializeField] Transform TOSLinksParent;
    [SerializeField] GameObject TOSLinkPrefab;
    [SerializeField] Text InfoPanelText;
    [SerializeField] Button InfoPanelButton;

    // singleton
    static AuthenticationManager Instance;
    
    // cache this so we dont re-get the TOS
    bool alreadyHaveTOS;
    TermsOfUse TOS;

    void Awake()
    {
        // assign this as the singleton
        Instance = this;
    }

#region Opening and Closing panels
    /// <summary>
    /// We use this to close all panels as part of the authentication menu.
    /// We mostly use this prior to opening a specific panel to ensure we dont have more than one
    /// panel open at a time.
    /// </summary>
    public void ClosePanels()
    {
        TOSPanel.SetActive(false);
        EmailPanel.SetActive(false);
        CodePanel.SetActive(false);
        InfoPanel.SetActive(false);
        LoadingPanel.SetActive(false);
    }

    /// <summary>
    /// Closes the authentication menu and it's panels
    /// </summary>
    public void CloseAuthenticationManager()
    {
        AuthenticationMenu.SetActive(false);
    }
    
    /// <summary>
    /// Opens the authentication manager and begins by displaying the Terms of Service panel
    /// </summary>
    public static void OpenAuthenticationManager()
    {
        Instance.AuthenticationMenu.SetActive(true);
        Instance.RequestTermsOfService();
    }

    /// <summary>
    /// Displays the TOS to the user prompting them to click 'I Agree'
    /// </summary>
    public void OpenTOSPanel()
    {
        ClosePanels();
        TOSText.text = TOS.termsOfUse;
        
        // There are required links we need to display to the user, we can iterate over the links
        // provided as part of the TOS object and choose to display only the required ones.
        if (!alreadyHaveTOS)
        {
            foreach(var link in TOS.links)
            {
                if(link.required)
                {
                    GameObject GO = Instantiate(TOSLinkPrefab, TOSLinksParent);
                    GO.GetComponent<Text>().text = $"{link.name}";
                    GO.GetComponent<Button>().onClick.AddListener(delegate
                    {
                        Application.OpenURL(link.url);
                    });
                }
            }
        }
        TOSPanel.SetActive(true);
        
        // Because of the layout groups on this panel we need to force a redraw
        LayoutRebuilder.ForceRebuildLayoutImmediate(TOSPanel.transform as RectTransform);
    }

    /// <summary>
    /// Opens the panel for the user to input their email address
    /// </summary>
    public void OpenEmailPanel()
    {
        ClosePanels();
        EmailPanel.SetActive(true);
        EmailInputField.text = "";
    }

    /// <summary>
    /// Opens the panel for the user to enter the 5 digit security code that was emailed to them
    /// </summary>
    public void OpenCodePanel()
    {
        ClosePanels();
        CodePanel.SetActive(true);
        CodeInputField.text = "";
    }

    /// <summary>
    /// This acts as a notice panel. It only has one button which either closes the authentication
    /// menu altogether or will open a different panel depending on the Action provided.
    /// This may display the result of the login such as "You successfully logged in" or an error
    /// such as "Something went wrong, try again"
    /// </summary>
    public void OpenInfoPanel(string message, Action buttonAction = null)
    {
        ClosePanels();
        InfoPanel.SetActive(true);
        InfoPanelText.text = message;

        InfoPanelButton.onClick.RemoveAllListeners();
        if(buttonAction != null)
        {
            InfoPanelButton.onClick.AddListener(delegate { buttonAction(); });
        }
        else
        {
            InfoPanelButton.onClick.AddListener(delegate { CloseAuthenticationManager(); });
        }
    }

    /// <summary>
    /// Opens a non-closing panel that displays "Loading..."
    /// </summary>
    public void OpenLoadingPanel()
    {
        ClosePanels();
        LoadingPanel.SetActive(true);
    }
#endregion

    /// <summary>
    /// This method is attached to the Unity Event on the 'Submit' button of the email panel.
    /// This takes the text from the input field of the email panel and requests the mod.io server
    /// to send a security code to the specified address.
    /// </summary>
    public async void SubmitEmail()
    {
        OpenLoadingPanel();
        Result result = await ModIOUnityAsync.RequestAuthenticationEmail(EmailInputField.text);
        
        if(result.Succeeded())
        {
            // If it succeeds to send the email, we continue by displaying the panel to enter the 5 digit code
            OpenCodePanel();
        }
        else
        {
            // If we fail to send an email the user may not have provided a valid email address
            OpenInfoPanel("Failed to send an email to the specified address. Please try again.", OpenEmailPanel);
        }
    }

    /// <summary>
    /// This method is attached to the Unity Event on the 'Submit' button of the code panel.
    /// This takes the text from the input field of the code panel and sends it to the mod.io
    /// server to attempt to authenticate the user by the email address provided earlier.
    /// </summary>
    public async void SubmitCode()
    {
        OpenLoadingPanel();
        Result result = await ModIOUnityAsync.SubmitEmailSecurityCode(CodeInputField.text);
        if(result.Succeeded())
        {
            TextureListMenu.isAuthenticated = true;
            
            // If we succeeded, we can now inform the user that they are logged in
            OpenInfoPanel("You have successfully logged in.", delegate
            {
                CloseAuthenticationManager();
                TextureListMenu.Instance.OpenTextureListMenu();
            });
        }
        else
        {
            // if it failed, the user may not have entered the code correctly, we can go back and try again
            OpenInfoPanel("That code was incorrect. Please try again.", OpenCodePanel);
        }
    }

    /// <summary>
    /// This sends a request to the mod.io server for an up to date version of the TOS for
    /// authenticating a user.
    /// </summary>
    public async void RequestTermsOfService()
    {
        // if we already got the TOS previously we dont need to get it again in the same session
        if(alreadyHaveTOS)
        {
            OpenTOSPanel();
            return;
        }
        
        OpenLoadingPanel();
        
        ResultAnd<TermsOfUse> request = await ModIOUnityAsync.GetTermsOfUse();
        if(request.result.Succeeded())
        {
            TOS = request.value;
            
            // If we succeed, we cache the TOS object o we can display it, then open the TOS panel
            OpenTOSPanel();
            alreadyHaveTOS = true;
        }
        else
        {
            // If this fails we may not be connected to the internet, or the mod.io server could be down
            OpenInfoPanel("Could not connect. Please check your internet connection and try again.");
        }
    }
}
