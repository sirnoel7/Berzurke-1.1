// Assets/Scripts/Player.cs
using System.Collections;
using UnityEngine;

public class Player : MonoBehaviour
{
    private CharacterController cc;
    private float yVel = 0f;

    [Header("Movement Settings")]
    public float gravity = 30f;
    public float jumpForce = 10f;

    [Header("Coyote Time")]
    public float coyoteTime = 0.12f;
    private float coyoteCounter = 0f;

    [Header("Jump Buffer")]
    public float jumpBufferTime = 0.12f;
    private float jumpBufferCounter = 0f;

    private float lastJumpPressedTime = -10f;
    private float lateJumpThreshold = 0.15f;

    private Vector3 baseScale = new Vector3(0.3f, 0.3f, 1f);
    private Vector3 targetScale;

    private AnimatedSprite anim;
    private bool wasInAir = false;

    private bool isDead = false;

    private Collider lastHitObstacle = null;
    private float hitCooldown = 0.30f;

    // True during the Red Bull drink sequence
    private bool rbSequence = false;

    private void Awake()
    {
        cc = GetComponent<CharacterController>();
        anim = GetComponentInChildren<AnimatedSprite>();
        targetScale = baseScale;
    }

    // Enter Red Bull sequence: stop player movement; freeze sprite animation.
    public void BeginRedBullSequence()
    {
        rbSequence = true;
        yVel = 0f;
        anim?.FreezeAllAnimation(); // hard-freeze Update; ShowFrame still works
    }

    // Exit Red Bull sequence: resume movement/input.
    public void EndRedBullSequence()
    {
        rbSequence = false; // why: allow Update() to run normal controls again
    }

    private void Update()
    {
        if (isDead) return;

        // Block input/movement while the sequence runs
        if (rbSequence)
        {
            cc.Move(Vector3.zero);
            return;
        }

        bool grounded = cc.isGrounded;

        if (grounded) coyoteCounter = coyoteTime;
        else coyoteCounter -= Time.deltaTime;

        // Jump buffer
        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferCounter = jumpBufferTime;
            lastJumpPressedTime = Time.time;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        bool canJump = (coyoteCounter > 0f && jumpBufferCounter > 0f);

        if (canJump)
        {
            yVel = jumpForce;
            coyoteCounter = 0f;
            jumpBufferCounter = 0f;

            anim?.SetAirborne(true);
            anim?.SetFrozen(true);
        }

        if (grounded && !canJump)
        {
            if (yVel < -2f) yVel = -2f;

            anim?.SetAirborne(false);

            if (!rbSequence) // guard remains in case of async calls
                anim?.SetFrozen(false);

            if (wasInAir)
                targetScale = new Vector3(baseScale.x * 0.85f, baseScale.y * 1.05f, 1f);
            else
                targetScale = baseScale;
        }
        else if (!grounded)
        {
            yVel -= gravity * Time.deltaTime;
            anim?.SetAirborne(true);

            if (!rbSequence)
                anim?.SetFrozen(true);
        }

        wasInAir = !grounded;

        cc.Move(new Vector3(0, yVel, 0) * Time.deltaTime);

        transform.localScale =
            Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * 12f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Obstacle")) return;
        if (isDead || rbSequence) return; // ignore hits during drink sequence
        if (other == lastHitObstacle) return;

        lastHitObstacle = other;
        StartCoroutine(ClearLastHitObstacle());

        HandleHit();
    }

    private IEnumerator ClearLastHitObstacle()
    {
        yield return new WaitForSeconds(hitCooldown);
        lastHitObstacle = null;
    }

    private void HandleHit()
    {
        GameManager.Instance?.PlayerHit();

        if (GameManager.Instance != null && GameManager.Instance.currentLives <= 0)
        {
            Die();
            return;
        }

        yVel = 0.4f;
        anim?.PlayHurtFlash();
    }

    private void Die()
    {
        isDead = true;
        yVel = 0f;

        float t = Time.time - lastJumpPressedTime;

        if (t <= lateJumpThreshold)
            anim?.PlayStandingDeath();
        else
            anim?.PlayDeath();
    }
}
