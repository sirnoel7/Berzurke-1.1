using UnityEngine;

public class BirdAnimatedSprite : MonoBehaviour
{
    [Header("Frames")]
    public Sprite[] sprites;        // Should contain exactly 2 frames: wings up, wings down

    private SpriteRenderer spriteRenderer;
    private int frame = 0;

    [Header("Animation Settings")]
    public float baseFps = 8f;      // Bird flap speed before difficulty scaling

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        frame = 0;
        Animate();
    }

    private void OnDisable()
    {
        CancelInvoke();
    }

    private void Animate()
    {
        if (sprites == null || sprites.Length == 0)
            return;

        // Set sprite
        spriteRenderer.sprite = sprites[frame];

        // Next frame
        frame = (frame + 1) % sprites.Length;

        // Difficulty affects flap speed
        float speedMult = 1f;
        if (GameManager.Instance != null)
            speedMult = GameManager.Instance.speedMultiplier;

        float fps = baseFps * speedMult;
        float delay = 1f / fps;

        // minimum delay so animation doesn't break at very high speeds
        if (delay < 0.03f)
            delay = 0.03f;

        Invoke(nameof(Animate), delay);
    }
}
