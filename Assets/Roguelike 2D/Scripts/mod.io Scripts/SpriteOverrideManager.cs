using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// We use this script to Load the sprite sheets being used in game, and also to load new ones from
/// given directories. We also have a method for used by the SpriteOverride.cs called GetSprite.
/// This uses a sprite as the key, it then looks up for the appropriate sprite to swap to from the
/// dictionary of the current loaded sprite sheet texture. (see CurrentTexturePack and LoadTexturePack)
/// </summary>
public static class SpriteOverrideManager
{
    // using the default sprites as the key for the equivalent in this texture pack
    public static Dictionary<Sprite,Sprite> CurrentTexturePack = new Dictionary<Sprite, Sprite>(); 
    public static Sprite[] DefaultTextures;

    /// <summary>
    /// This loads the default sprite sheet into memory, DefaultTextures
    /// </summary>
    public static void LoadDefaultTextures()
    {
        DefaultTextures = Resources.LoadAll<Sprite>("Sprites/Scavengers_SpriteSheet");
    }
    
    /// <summary>
    /// This loads a sprite sheet based on a directory provided, eg a mod. It then slices the sheet
    /// according to the same dimensions as the default sheet, and loads each sprite into a dictionary
    /// using the default sprite on the same slice as the key.
    /// </summary>
    public static void LoadTexturePack(string directory)
    {
        if(DefaultTextures == null) LoadDefaultTextures();
        
        string filepath = Path.Combine(directory, "Spritesheet.png");

        Texture2D tex = new Texture2D(1, 1);
        tex.LoadImage(File.ReadAllBytes(filepath));
        tex.filterMode = FilterMode.Point;

        CurrentTexturePack.Clear();
        int xOffset = 0;
        int yOffset = (tex.height / 32) - 1;
        foreach(Sprite sprite in DefaultTextures)
        {
            try
            {
                Sprite newSprite = Sprite.Create(tex, new Rect(32 * xOffset, 32 * yOffset, 32, 32), new Vector2(0.5f, 0.5f), 32f);
                CurrentTexturePack.Add(sprite, newSprite);
                xOffset++;
                if(32 * xOffset > tex.width - 32)
                {
                    yOffset--;
                    xOffset = 0;
                }
            }
            catch
            {
                Debug.LogError("Failed to slice spritesheet");
                break;
            }
        }
    }

    /// <summary>
    /// Used by the SpriteOverride.cs script to check if it can swap it's current sprite to the
    /// loaded texture pack instead. This gets called each frame on LateUpdate, after any animation
    /// or prefab changes have occurred.
    /// </summary>
    public static Sprite GetSprite(Sprite sprite)
    {
        CurrentTexturePack.TryGetValue(sprite, out Sprite replacement);
        return replacement == null ? sprite : replacement;
    }
}
