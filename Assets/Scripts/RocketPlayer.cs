using UnityEngine;

/// <summary>
/// Flappy-like rocket: thrust while input held, gravity otherwise.
/// Shows thrust/idle/hit sprites. One life lost per valid hit.
/// Works with MeteorHitOnce (preferred) to prevent repeat hits per meteor.
/// On death: ignores clamps/input and free-falls off-screen.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class RocketPlayer : MonoBehaviour
{
    [Header("Flight")]
    public float gravity = 25f;
    public float thrust = 12f;
    public float maxUpVel = 15f;
    public float maxDownVel = -20f;

    [Header("Bounds")]
    public float minY = -2.5f;
    public float maxY = 5.0f;

    [Header("Tilt")]
    public float maxTilt = 35f;     // degrees
    public float tiltLerp = 12f;

    [Header("Sprites")]
    public Sprite idleSprite;       // fire OFF
    public Sprite thrustSprite;     // fire ON
    public Sprite hitSprite;        // briefly on damage
    public float hitSpriteTime = 0.12f;
    public float thrustLinger = 0.08f; // small after-burn to avoid flicker

    [Header("Damage / Invulnerability")]
    public float hitCooldown = 0.35f; // how long we ignore further hits after one

    [Header("Death Fall")]
    public float deathGravityMultiplier = 1.25f; // faster plunge feels better

    private Rigidbody2D rb;
    private SpriteRenderer sr;

    private float yVel = 0f;
    private float hitLockUntil = -999f;
    private float showHitUntil = -999f;
    private float thrustLingerUntil = -999f;

    private bool deadFalling = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();

        // 2D setup expectations
        rb.gravityScale = 0f; // simulate gravity manually
        rb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;

        var col = GetComponent<Collider2D>();
        col.isTrigger = true; // we use trigger hits

        if (idleSprite != null) sr.sprite = idleSprite;
    }

    public void BeginDeathFall()
    {
        deadFalling = true;
        hitLockUntil = float.PositiveInfinity;   // ignore any further damage
        showHitUntil = Time.time + Mathf.Max(0.08f, hitSpriteTime); // flash hit briefly
        if (hitSprite != null) sr.sprite = hitSprite;
    }

    private void Update()
    {
        // During finale freeze, we stop entirely (no fall)
        if (GameManager.Instance != null && GameManager.Instance.inFinalCinematic)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        if (deadFalling)
        {
            // no input, stronger gravity, no y clamp
            yVel -= gravity * deathGravityMultiplier * Time.deltaTime;
            rb.velocity = new Vector2(0f, yVel);

            // tilt nose-down quickly
            float deadTiltTarget = -maxTilt;
            transform.localRotation = Quaternion.Lerp(
                transform.localRotation,
                Quaternion.Euler(0, 0, deadTiltTarget),
                Time.deltaTime * (tiltLerp * 0.75f)
            );

            // sprite already set to hit briefly; after that show idle
            if (Time.time >= showHitUntil && idleSprite != null && sr.sprite != idleSprite)
                sr.sprite = idleSprite;

            return;
        }

        bool pressing =
            Input.GetButton("Jump") ||
            Input.GetMouseButton(0) ||
            (Input.touchCount > 0);

        // Input -> thrust, else -> gravity
        if (pressing)
        {
            yVel = thrust;
            thrustLingerUntil = Time.time + thrustLinger;
        }
        else
        {
            yVel -= gravity * Time.deltaTime;
        }

        yVel = Mathf.Clamp(yVel, maxDownVel, maxUpVel);

        // Apply velocity
        rb.velocity = new Vector2(0f, yVel);

        // Clamp vertical bounds
        Vector3 pos = transform.position;
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        transform.position = pos;

        // Tilt based on vertical velocity
        float lerpT = Mathf.InverseLerp(maxDownVel, maxUpVel, yVel);
        float runTiltTarget = Mathf.Lerp(-maxTilt, maxTilt, lerpT);
        transform.localRotation = Quaternion.Lerp(
            transform.localRotation,
            Quaternion.Euler(0, 0, runTiltTarget),
            Time.deltaTime * tiltLerp
        );

        // Sprite logic
        UpdateSprite(pressing);
    }

    private void UpdateSprite(bool pressing)
    {
        // Show hit sprite briefly on damage
        if (Time.time < showHitUntil && hitSprite != null)
        {
            if (sr.sprite != hitSprite) sr.sprite = hitSprite;
            return;
        }

        // Otherwise, thrust sprite while pressing or during short after-burn
        bool showThrust = pressing || Time.time < thrustLingerUntil;
        if (showThrust && thrustSprite != null)
        {
            if (sr.sprite != thrustSprite) sr.sprite = thrustSprite;
        }
        else if (idleSprite != null)
        {
            if (sr.sprite != idleSprite) sr.sprite = idleSprite;
        }
    }

    private bool CanTakeHit => Time.time >= hitLockUntil && !deadFalling;

    private void TakeHit()
    {
        if (!CanTakeHit) return;

        // Lock further hits briefly
        hitLockUntil = Time.time + hitCooldown;

        // Briefly show hit sprite
        showHitUntil = Time.time + hitSpriteTime;

        // Tell the game we were hit (camera shake + exactly one life)
        GameManager.Instance?.PlayerHit();

        // Small upward nudge feels nice on hit
        yVel = Mathf.Max(yVel, thrust * 0.35f);
    }

    private void TryHitFrom(Collider2D other)
    {
        // Prefer MeteorHitOnce contract: consume once per meteor
        var once = other.GetComponent<MeteorHitOnce>();
        if (once != null)
        {
            if (once.TryConsume())
                TakeHit();
            return;
        }

        // Fallback: rely on our cooldown + tag
        if (other.CompareTag("Obstacle"))
            TakeHit();
    }

    private void OnTriggerEnter2D(Collider2D other) => TryHitFrom(other);
    private void OnTriggerStay2D(Collider2D other) => TryHitFrom(other);
}
