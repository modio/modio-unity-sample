using System;
using ModIO;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This acts as a handler for all of the UI a user needs to report a mod.
/// Note: when displaying User Generated Content (UGC) on certain platforms it may be a requirement
/// that you offer users the ability to report specific mods.
/// </summary>
public class ReportManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] GameObject ReportingMenus;
    [SerializeField] GameObject SummaryPanel;
    [SerializeField] GameObject InfoPanel;
    [SerializeField] GameObject LoadingPanel;
    
    [Header("Panel elements")]
    [SerializeField] Dropdown ReasonDropdown;
    [SerializeField] InputField DescriptionTextBox;
    [SerializeField] InputField ContactNameInputField;
    [SerializeField] InputField ContactEmailInputField;
    [SerializeField] Text InfoPanelText;
    [SerializeField] Button InfoPanelButton;

    ModProfile currentModBeingReported;

    // singleton
    static ReportManager Instance;

    void Awake()
    {
        // assign this instance as the singleton
        Instance = this;
    }

#region Opening and Closing panels
    /// <summary>
    /// Closes all of the panels associated with the reporting flow. We can call this before opening
    /// any new panels to be sure we dont open multiple panels at once
    /// </summary>
    public void ClosePanels()
    {
        SummaryPanel.SetActive(false);
        InfoPanel.SetActive(false);
        LoadingPanel.SetActive(false);
    }

    /// <summary>
    /// This closes the entire ReportMenu and all of it's panels
    /// </summary>
    public void CloseReportingMenu()
    {
        ReportingMenus.SetActive(false);
    }
    
    /// <summary>
    /// This opens the ReportMenu and begins by displaying the summary menu for filling out the
    /// details of a report
    /// </summary>
    public static void OpenReportManager(ModProfile profile)
    {
        Instance.ReportingMenus.SetActive(true);
        Instance.currentModBeingReported = profile;
        Instance.OpenSummaryMenu();
    }

    /// <summary>
    /// This opens the first panel where the user can describe the reason for reporting as well as
    /// their contact details and a more detailed summary of the report.
    /// </summary>
    public void OpenSummaryMenu()
    {
        ClosePanels();
        SummaryPanel.SetActive(true);
        ReasonDropdown.value = 0;
        DescriptionTextBox.text = "";
        ContactNameInputField.text = "";
        ContactEmailInputField.text = "";
    }

    /// <summary>
    /// This acts as a notice panel. It only has one button which either closes the ReportMenu
    /// altogether or will open a different panel depending on the Action provided.
    /// This may display the result of the report being sent such as "Thank you for sending a report"
    /// or an error such as "Something went wrong, try again"
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
            InfoPanelButton.onClick.AddListener(delegate { CloseReportingMenu(); });
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
    /// This method is attached to the Unity Event on the 'Submit' button of the summary panel.
    /// This takes all of the fields on the panel and inputs them into a Report object that we then
    /// send off to the mod.io server
    /// </summary>
    public async void SubmitReport()
    {
        OpenLoadingPanel();

        Report report = new Report(currentModBeingReported.id, 
                        (ReportType)ReasonDropdown.value,
                 DescriptionTextBox.text,
                    ContactNameInputField.text,
                        ContactEmailInputField.text);
        
        Result result = await ModIOUnityAsync.Report(report);
        
        if(result.Succeeded())
        {
            OpenInfoPanel("Thank you for sending a report.", CloseReportingMenu);
        }
        else
        {
            OpenInfoPanel("Something went wrong. could not send your report. Please try again later.", CloseReportingMenu);
        }
    }
}
