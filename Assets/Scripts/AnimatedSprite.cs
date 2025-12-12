// Assets/Scripts/AnimatedSprite.cs
using System.Collections;
using UnityEngine;

/// <summary>
/// Handles run-cycle, bobbing, tilt, squash/stretch.
/// Hard-freeze for drink sequence; final wings-only loop with eased flap.
/// </summary>
public class AnimatedSprite : MonoBehaviour
{
    [Header("Run Cycle Sprites")]
    public Sprite[] sprites;

    [Header("Hurt / Death Sprites")]
    public Sprite hurtSprite;
    public Sprite deathSprite;
    public float hurtFlashTime = 0.15f;

    [Header("Red Bull Special Sprites")]
    public Sprite idleSprite;
    public Sprite drink1Sprite;
    public Sprite drink2Sprite;
    public Sprite drink3Sprite;
    public Sprite wingsSprite;

    [Header("Final Wings Loop")]
    public Sprite[] wingsLoopSprites;               // assign 2+ frames (e.g., wings up/down)
    [Tooltip("Flaps per second (back-and-forth cycle rate).")]
    public float wingsFlapsPerSecond = 3f;
    [Range(0f, 0.9f)]
    [Tooltip("Blend toward sine ease-in/out (0=no ease, 0.25=gentle).")]
    public float wingsEaseAmount = 0.25f;
    [Tooltip("Use realtime when timeScale may change.")]
    public bool wingsLoopUseUnscaledTime = false;

    private SpriteRenderer sr;
    private int frame = 0;

    [Header("Animation Settings")]
    public float fps = 12f;

    [Header("Bobbing")]
    public float runBobA = 0.03f;
    public float runBobF = 8f;
    public float airBobA = 0.015f;
    public float airBobF = 5f;

    [Header("Air Pose")]
    public float tiltAngle = 8f;

    [Header("Squash & Stretch")]
    public float stretchScale = 1.15f;
    public float squashScale = 0.85f;
    public float scaleLerpSpeed = 10f;

    private bool frozenByPlayer = false; // soft-freeze (run-cycle)
    private bool frozenByHurt = false;   // soft-freeze (run-cycle)
    private bool fullyFrozen = false;    // hard-freeze: skip all anim logic
    private bool dead = false;

    private float timer = 0f;
    private float lastBob = 0f;

    private readonly Vector3 baseScale = new Vector3(0.3f, 0.3f, 1f);
    private Vector3 targetScale;

    private bool airborne = false;
    private bool wasAirborneLastFrame = false;

    // Saved soft-freeze flags for restoration
    private bool savedFrozenByPlayer;
    private bool savedFrozenByHurt;

    // Wings loop state
    private bool finalWingsLoopActive = false;
    private float wingsPhase = 0f;  // 0..1 normalized phase across one flap cycle
    private int wingsLastIndex = -1;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        transform.localScale = baseScale;
        targetScale = baseScale;
    }

    public void FreezeAllAnimation()
    {
        if (fullyFrozen) return;
        savedFrozenByPlayer = frozenByPlayer;
        savedFrozenByHurt = frozenByHurt;
        fullyFrozen = true;
        finalWingsLoopActive = false; // why: full-freeze should trump special states
        timer = 0f;
    }

    public void UnfreezeAllAnimation()
    {
        if (!fullyFrozen) return;
        fullyFrozen = false;
        frozenByPlayer = savedFrozenByPlayer;
        frozenByHurt = savedFrozenByHurt;
        timer = 0f;
    }

    public void SetFrozen(bool f)
    {
        if (fullyFrozen || finalWingsLoopActive) return; // why: loop owns pose/frames
        frozenByPlayer = f;
    }

    /// <summary>
    /// Start wings-only loop: disables bob/tilt/squash/run-cycle; only flips wings frames with ease.
    /// </summary>
    public void StartFinalWingsLoop(bool resetPose = true)
    {
        fullyFrozen = false;         // why: allow Update to tick the loop
        finalWingsLoopActive = true;
        frozenByPlayer = true;       // block run-cycle
        frozenByHurt = false;

        wingsPhase = 0f;
        wingsLastIndex = -1;

        if (resetPose)
        {
            // remove any bob offset to avoid freezing mid-bob
            var p = transform.localPosition;
            p.y -= lastBob;
            transform.localPosition = p;
            lastBob = 0f;

            transform.localRotation = Quaternion.identity;
            targetScale = baseScale;
            transform.localScale = baseScale;
            airborne = false;
            wasAirborneLastFrame = false;
        }

        // seed first sprite
        if (wingsLoopSprites != null && wingsLoopSprites.Length > 0 && wingsLoopSprites[0] != null)
            sr.sprite = wingsLoopSprites[0];
        else if (wingsSprite != null)
            sr.sprite = wingsSprite;
    }

    public void StopFinalWingsLoop()
    {
        finalWingsLoopActive = false;
    }

    private void Update()
    {
        if (dead) return;

        // Wings-only loop: advance just the wings frames; skip all other animation.
        if (finalWingsLoopActive)
        {
            float dt = wingsLoopUseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float hz = Mathf.Max(0.01f, wingsFlapsPerSecond);

            // advance phase 0..1 at 'hz' cycles/sec
            wingsPhase += dt * hz;
            if (wingsPhase >= 1f) wingsPhase -= 1f;

            // sine-ease blend: linear t vs eased t (0.5 - 0.5*cos(2πt))
            float tLinear = wingsPhase;
            float tEased = 0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * tLinear);
            float t = Mathf.Lerp(tLinear, tEased, wingsEaseAmount);

            // ping-pong across frames: 0..1..0 mapping
            float ping = (t <= 0.5f) ? (t * 2f) : (2f - t * 2f); // 0..1 up then down

            int n = (wingsLoopSprites != null) ? wingsLoopSprites.Length : 0;
            if (n >= 2)
            {
                float fIndex = ping * (n - 1);
                int index = Mathf.Clamp(Mathf.RoundToInt(fIndex), 0, n - 1);
                if (index != wingsLastIndex)
                {
                    Sprite s = wingsLoopSprites[index];
                    if (s != null) sr.sprite = s;
                    wingsLastIndex = index;
                }
            }
            else if (wingsSprite != null && wingsLastIndex != 0)
            {
                sr.sprite = wingsSprite; // fallback
                wingsLastIndex = 0;
            }

            return; // skip bob/tilt/run-cycle/squash
        }

        if (fullyFrozen) return;

        float multiplier = (GameManager.Instance != null ? GameManager.Instance.speedMultiplier : 1f);

        // Bobbing
        float bob = airborne
            ? Mathf.Sin(Time.time * airBobF) * airBobA
            : Mathf.Sin(Time.time * runBobF * multiplier) * runBobA;

        Vector3 pos = transform.localPosition;
        pos.y -= lastBob;
        pos.y += bob;
        transform.localPosition = pos;
        lastBob = bob;

        // Tilt
        transform.localRotation = airborne ? Quaternion.Euler(0, 0, tiltAngle) : Quaternion.identity;

        // Run-cycle
        if (!frozenByPlayer && !frozenByHurt && sprites.Length > 0)
        {
            timer += Time.deltaTime;
            float speed = fps * multiplier;
            if (speed > 0f && timer >= 1f / speed)
            {
                timer = 0f;
                frame = (frame + 1) % sprites.Length;
                sr.sprite = sprites[frame];
            }
        }

        // Squash & stretch
        if (airborne)
            targetScale = new Vector3(baseScale.x * 0.95f, baseScale.y * stretchScale, 1f);
        else
            targetScale = wasAirborneLastFrame
                ? new Vector3(baseScale.x * squashScale, baseScale.y * 1.05f, 1f)
                : baseScale;

        wasAirborneLastFrame = airborne;

        transform.localScale =
            Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * scaleLerpSpeed);
    }

    private void ShowFrame(Sprite s)
    {
        if (s == null) return;
        sr.sprite = s;
        frozenByPlayer = true; // why: ensure run-cycle can't override forced frame
    }

    public void ShowIdleFrame() => ShowFrame(idleSprite);
    public void ShowDrink1Frame() => ShowFrame(drink1Sprite);
    public void ShowDrink2Frame() => ShowFrame(drink2Sprite);
    public void ShowDrink3Frame() => ShowFrame(drink3Sprite);
    public void ShowWingsFrame() => ShowFrame(wingsSprite);

    public void SetAirborne(bool a) => airborne = a;

    public void PlayHurtFlash() => StartCoroutine(HurtRoutine());

    private IEnumerator HurtRoutine()
    {
        frozenByHurt = true;
        if (hurtSprite != null) sr.sprite = hurtSprite;
        yield return new WaitForSeconds(hurtFlashTime);
        frozenByHurt = false;
        timer = 0f;
    }

    public void PlayStandingDeath()
    {
        dead = true;
        frozenByHurt = true;
        frozenByPlayer = true;
        if (hurtSprite != null) sr.sprite = hurtSprite;
    }

    public void PlayDeath()
    {
        dead = true;
        frozenByHurt = true;
        frozenByPlayer = true;
        if (deathSprite != null) sr.sprite = deathSprite;
        transform.localRotation = Quaternion.identity;
    }
}
 