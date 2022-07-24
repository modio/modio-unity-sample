using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using ModIO;

/// <summary>
/// This represents the main handler for controlling the menu that displays the list of textures/mods
/// </summary>
public class TextureListMenu : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] GameObject TexturesMenu;
    [SerializeField] Text LoadingText;
    [SerializeField] GameObject DownloadedTexturePrefab;
    List<TextureListItem> DownloadedTextureListItems = new List<TextureListItem>();
    [SerializeField] Transform DownloadedListItemsParent;
    [SerializeField] InputField SearchField;

    ModProfile[] mods;
    public static bool isAuthenticated;
    bool isSearching;

    // Singleton
    public static TextureListMenu Instance;

    void Awake()
    {
        // Assigning itself to make a singleton
        Instance = this;
    }

    void Start()
    {
        // The first thing we do is initialize the plugin when this object is active
        // The following example uses the config settings entered in Resources/mod.io/config
        ModIOUnity.InitializeForUser("Example", Initialized);
    }

    /// <summary>
    /// We use this callback to inform us when the initialize function completes. If it succeeds
    /// the first thing we do is EnableModManagement so mods will automatically download and install
    /// </summary>
    void Initialized(Result result)
    {
        if(result.Succeeded())
        {
            Debug.Log("Initialized the ModIO plugin");
            
            // This activates automatic mod management. So when we subscribe or unsubscribe from
            // mods, the plugin will automatically download and install the mods we need. The event
            // we give (ModManagementEvent) informs us when these mod management functions are
            // being performed.
            ModIOUnity.EnableModManagement(ModManagementEvent);
        }
        else
        {
            Debug.Log("Failed to initialize the ModIO plugin");
        }
    }

    /// <summary>
    /// This method will be invoked whenever the plugin performs a mod management operation.
    /// See the ModManagementEventType enum for a list of the types of operations
    /// </summary>
    public void ModManagementEvent(ModManagementEventType eventType, ModId modId, Result eventResult)
    {
        if(eventType == ModManagementEventType.Installed || eventType == ModManagementEventType.Uninstalled)
        {
            // If we receive an event for either a finished install or uninstall of a mod, we
            // refresh the list and all the items in it.
            RefreshListOfTextures();
        }
    }

    public async void OpenTextureListMenu()
    {
        TexturesMenu.SetActive(true);
        RefreshListOfTextures();
        
        // Check to see if we are authenticated
        Result result = await ModIOUnityAsync.IsAuthenticated();
        
        // If we are not authenticated we open up the panel for the user to attempt to login
        if(!result.Succeeded())
        {
            isAuthenticated = false;
            AuthenticationManager.OpenAuthenticationManager();
        }
        else
        {
            // otherwise if we are already authenticated, we can cache the result here for future
            isAuthenticated = true;
        }
    }

    /// <summary>
    /// This method is attached to the 'OnEndEdit' Unity Event on the Search bar input field.
    /// It generates a search filter based on the phrase inputted in the search field and attempts
    /// to get mods from the mod.io server
    /// </summary>
    public async void SearchForMods()
    {
        // Make sure we aren't already searching
        if(isSearching || string.IsNullOrWhiteSpace(SearchField.text))
        {
            return;
        }
        
        // Create a new filter object to be used for the query
        SearchFilter filter = new SearchFilter();
        filter.SetPageIndex(0);
        filter.SetPageSize(10);
        filter.AddSearchPhrase(SearchField.text);

        // Show some loading text and hide the current displayed list of mods
        LoadingText.gameObject.SetActive(true);
        HideAllListItemsForTextures();
        isSearching = true;
        
        // Attempt to get mods based on the filter, wait asynchronously
        ResultAnd<ModPage> request = await ModIOUnityAsync.GetMods(filter);
        
        isSearching = false;
        
        // If we succeeded, we can set the mods we received and refresh the list
        if(request.result.Succeeded() 
           && request.value.modProfiles != null         // check our value is instantiated
           && request.value.modProfiles.Length > 0)     // check our array isn't empty
        {
            mods = request.value.modProfiles;
            RefreshListOfTextures();
        }
        else
        {
            LoadingText.text = "No mods found";
            LoadingText.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// This method is attached to the Unity Event on the 'Clear' button beside the search bar
    /// </summary>
    public void ClearSearchField()
    {
        SearchField.text = "";
        mods = null;
        RefreshListOfTextures();
    }

    /// <summary>
    /// This method is attached to the Unity Event on the 'Back to game' button on the top left
    /// of the texture list display
    /// </summary>
    public void CloseTextureListMenu()
    {
        TexturesMenu.SetActive(false);
    }

    /// <summary>
    /// This method is attached to the Unity Event on the 'Logout' button on the top left
    /// of the texture list display
    /// </summary>
    public void Logout()
    {
        // this erases all of the cached user data, essentially logging them out
        ModIOUnity.RemoveUserData();
        
        //re-open the texture menu list to force an authentication check
        OpenTextureListMenu();
    }

    /// <summary>
    /// We use this method to obtain the first ten mods for our game, in order of most popular.
    /// </summary>
    public async Task<ResultAnd<ModPage>> GetMods()
    {
        // We create a new filter for getting the mods
        SearchFilter filter = new SearchFilter();
        
        filter.SetPageIndex(0);
        filter.SetPageSize(10);
        filter.SortBy(SortModsBy.Popular);

        // wait for the request to get the mods
        ResultAnd<ModPage> resultPage = await ModIOUnityAsync.GetMods(filter);

        return resultPage;
    }

    /// <summary>
    /// This refreshes the list of textures/mods we are displaying based on the 'mods' array we have
    /// cached. For each mod in that array we create a new list item.
    /// </summary>
    public async void RefreshListOfTextures()
    {
        HideAllListItemsForTextures();

        // Check if the mod array is null. If so, we get the first 10 popular mods to display
        if(mods == null)
        {
            // set the text to display "Loading..." while we wait for the request to get the mods
            LoadingText.text = "Loading...";
            LoadingText.gameObject.SetActive(true);
            
            // asynchronously get the first 10 popular mods
            ResultAnd<ModPage> resultPage = await GetMods();

            if(resultPage.result.Succeeded())
            {
                // If it succeeds, set the mods to our cached array to be populated
                mods = resultPage.value.modProfiles;
                LoadingText.gameObject.SetActive(false);
            }
            else
            {
                // if we failed, inform the user that we couldn't connect to receive any mods
                LoadingText.text = "Failed to get mods...";
            }
        }
        else
        {
            LoadingText.gameObject.SetActive(false);
        }

        // For each mod that we have in our array, we create a list item to represent it.
        // Each list item has it's own logo, text and buttons to manage that particular mod.
        foreach(var mod in mods)
        {
            AddTextureListItem(mod);
        }
    }

    /// <summary>
    /// We use this method to hide all of the list items we currently have displayed
    /// </summary>
    public void HideAllListItemsForTextures()
    {
        foreach(var li in DownloadedTextureListItems)
        {
            li.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Creates a new object to place into the list of textures / mods based on the ModProfile provided
    /// </summary>
    public void AddTextureListItem(ModProfile mod)
    {
        TextureListItem item = null;
        foreach(var li in DownloadedTextureListItems)
        {
            if(!li.gameObject.activeSelf)
            {
                item = li;
                break;
            }
        }

        if(item == null)
        {
            GameObject GO = Instantiate(DownloadedTexturePrefab, DownloadedListItemsParent);
            item = GO.GetComponent<TextureListItem>();
            DownloadedTextureListItems.Add(item);
        }
        
        item.Setup(mod);
        item.gameObject.SetActive(true);
    }
}
