using UnityEngine;

/// <summary>
/// This is attached to any prefab or game object with a sprite renderer that uses the default
/// sprite sheet as part of the Roguelike 2D game. Each LateUpdate it checks to see if it needs to
/// swap the sprite of the sprite renderer to the equivalent sprite in the loaded texture pack.
/// </summary>
public class SpriteOverride : MonoBehaviour
{
    [SerializeField] SpriteRenderer spriteRenderer;

    void Reset()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        Sprite sprite = SpriteOverrideManager.GetSprite(spriteRenderer.sprite);
        spriteRenderer.sprite = sprite;
    }
}
