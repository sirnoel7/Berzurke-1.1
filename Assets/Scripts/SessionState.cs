using UnityEngine;

/// <summary>
/// Persist small session data (e.g., lives & score carryover) across scene loads.
/// Values are one-time consumable to avoid accidental reuse.
/// </summary>
public class SessionState : MonoBehaviour
{
    public static SessionState Instance { get; private set; }

    // Lives
    private bool hasCarryoverLives;
    private int carryoverLives;

    // Score
    private bool hasCarryoverScore;
    private float carryoverScore;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ---- Lives ----
    public void SetLives(int lives)
    {
        carryoverLives = Mathf.Max(0, lives);
        hasCarryoverLives = true;
    }

    public bool TryConsumeLives(out int lives)
    {
        if (hasCarryoverLives)
        {
            lives = carryoverLives;
            hasCarryoverLives = false; // one-time use
            return true;
        }

        lives = 0;
        return false;
    }

    // ---- Score ----
    public void SetScore(float score)
    {
        carryoverScore = Mathf.Max(0f, score);
        hasCarryoverScore = true;
    }

    public bool TryConsumeScore(out float score)
    {
        if (hasCarryoverScore)
        {
            score = carryoverScore;
            hasCarryoverScore = false; // one-time use
            return true;
        }

        score = 0f;
        return false;
    }
}
