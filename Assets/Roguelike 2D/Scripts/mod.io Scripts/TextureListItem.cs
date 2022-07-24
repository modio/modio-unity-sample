using ModIO;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This represents one of the mods we display in the list when opening the Texture menu from the
/// button in the top right corner of the main scene.
/// </summary>
public class TextureListItem : MonoBehaviour
{
    [SerializeField] Image TextureLogo;
    [SerializeField] Text TextureName;
    [SerializeField] Text TextureRating;
    [SerializeField] Button LoadTexture;
    [SerializeField] Button DownloadOrDeleteTextureButton;
    [SerializeField] Text DownloadOrDeleteTextureButtonText;

    string directory = "";
    public ModProfile mod;

    /// <summary>
    /// We use this to setup the list item and populate the contents of it. Such as the name of the
    /// mod, the rating and the image.
    /// </summary>
    /// <param name="mod"></param>
    public async void Setup(ModProfile mod)
    {
        // Set the name and rating
        this.mod = mod;
        TextureName.text = mod.name;
        TextureRating.text = $"{mod.stats.ratingsDisplayText}";
        
        // set to black because the image hasn't loaded yet. Once it's loaded we set it to full color
        TextureLogo.color = Color.black;

        // Attempt to get the logo image of this mod asynchronously
        ResultAnd<Texture2D> download = await ModIOUnityAsync.DownloadTexture(mod.logoImage_320x180);
        
        // if the profile has changed since we started downloading this image, we dont continue
        // It may be that we have re-used this list item elsewhere, therefore make sure we are still
        // attempting to update the image according to the mod profile
        if(this.mod.id == mod.id)
        {
            // Check if the download succeeded
            if (download.result.Succeeded())
            {
                // Create a sprite from the texture we downloaded and apply it to the image component
                Sprite sprite = Sprite.Create(download.value, new Rect(0, 0, download.value.width, download.value.height), Vector2.zero);
                TextureLogo.color = Color.white;
                TextureLogo.sprite = sprite;
            }
            else
            {
                // Otherwise set the image to grey so we know it failed
                TextureLogo.color = Color.gray;
                TextureLogo.sprite = null;
            }
        }

        // Set the LoadTexture button to be interactable or not and change the text of the download
        // button. If we already have the mod installed, pressing it again will uninstall it.
        DownloadOrDeleteTextureButton.interactable = true;
        if(IsInstalled())
        {
            LoadTexture.interactable = true;
            DownloadOrDeleteTextureButtonText.text = "Delete";
        }
        else
        {
            LoadTexture.interactable = false;
            DownloadOrDeleteTextureButtonText.text = "Download";
        }
    }

    /// <summary>
    /// Checks if the current mod profile as part of this list item is installed or not.
    /// </summary>
    public bool IsInstalled()
    {
        // Get all of the subscribed mods from the mod.io plugin
        SubscribedMod[] mods = ModIOUnity.GetSubscribedMods(out Result result);
        
        if (result.Succeeded() && mods != null)
        {
            // Iterate over the mods and find one that matches the id of our mod and has "Installed" status
            foreach(var mod in mods)
            {
                if(mod.modProfile.id == this.mod.id && mod.status == SubscribedModStatus.Installed)
                {
                    directory = mod.directory;
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Unity Event attached to the 'Load' button on the list item object
    /// </summary>
    public void LoadThisTexture()
    {
        if(IsInstalled())
        {
            SpriteOverrideManager.LoadTexturePack(directory);
        }
    }

    /// <summary>
    /// Unity Event attached to the Rate '+/-' buttons on the list item object
    /// </summary>
    public async void RateMod(bool positive)
    {
        if(!TextureListMenu.isAuthenticated)
        {
            AuthenticationManager.OpenAuthenticationManager();
            return;
        }
        
        if(positive)
        {
            await ModIOUnityAsync.RateMod(mod.id, ModRating.Positive);
        }
        else
        {
            await ModIOUnityAsync.RateMod(mod.id, ModRating.Negative);
        }

        Setup(mod);
    }
    
    /// <summary>
    /// Unity Event attached to the 'Report' button on the list item object
    /// </summary>
    public void ReportMod()
    {
        ReportManager.OpenReportManager(mod);
    }
    
    /// <summary>
    /// Unity Event attached to the 'Download/Delete' button on the list item object
    /// </summary>
    public async void DownloadOrDeleteThisTexture()
    {
        if(!TextureListMenu.isAuthenticated)
        {
            AuthenticationManager.OpenAuthenticationManager();
            return;
        }
        
        DownloadOrDeleteTextureButton.interactable = false;
        
        // Check if our mod is installed. If so, we unsubscribe, otherwise we subscribe to it
        if(IsInstalled())
        {
            // Attempt to unsubscribe to the mod
            Result result = await ModIOUnityAsync.UnsubscribeFromMod(mod.id);
            if(result.Succeeded())
            {
                Debug.Log("Successfully unsubscribed from mod");
            }
        }
        else
        {
            // Attempt to subscribe to the mod
            Result result = await ModIOUnityAsync.SubscribeToMod(mod.id);
            if(result.Succeeded())
            {
                Debug.Log("Successfully subscribed to mod");
            }
        }
    }
}
