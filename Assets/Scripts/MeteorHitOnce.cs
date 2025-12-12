using UnityEngine;

/// <summary>
/// Guarantees a meteor only damages the player once.
/// After first consumption, disables all 2D colliders and clears the "Obstacle" tag.
/// Optionally destroys the meteor after a short delay.
/// </summary>
[DisallowMultipleComponent]
public class MeteorHitOnce : MonoBehaviour
{
    [Tooltip("Seconds after consuming before destroying the meteor. Set 0 to not auto-destroy.")]
    public float destroyAfter = 0.4f;

    [Tooltip("Also hide sprites immediately on consume.")]
    public bool disableRenderersOnConsume = false;

    private bool consumed = false;
    private Collider2D[] cachedColliders;
    private SpriteRenderer[] cachedRenderers;

    private void Awake()
    {
        cachedColliders = GetComponentsInChildren<Collider2D>(true);
        cachedRenderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    /// <summary>
    /// Returns true if this call consumed the meteor (i.e., first time).
    /// Returns false if it was already consumed earlier.
    /// </summary>
    public bool TryConsume()
    {
        if (consumed) return false;
        consumed = true;

        // Disable all colliders so it can't trigger again
        if (cachedColliders != null)
        {
            for (int i = 0; i < cachedColliders.Length; i++)
                if (cachedColliders[i] != null) cachedColliders[i].enabled = false;
        }

        // Remove obstacle tag to be extra safe
        gameObject.tag = "Untagged";

        // Optionally hide visuals
        if (disableRenderersOnConsume && cachedRenderers != null)
        {
            for (int i = 0; i < cachedRenderers.Length; i++)
                if (cachedRenderers[i] != null) cachedRenderers[i].enabled = false;
        }

        // Optional timed destroy
        if (destroyAfter > 0f)
            Destroy(gameObject, destroyAfter);

        return true;
    }
}
