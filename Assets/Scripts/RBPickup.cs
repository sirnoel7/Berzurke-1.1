// Assets/Scripts/RBPickup.cs
using UnityEngine;
using TMPro;
using System.Collections;

public class RBPickup : MonoBehaviour
{
    [Header("Red Bull Sequence Timings (Realtime)")]
    [SerializeField] private float drinkFrameHold = 0.22f;
    [SerializeField] private float wingsPreLoopHold = 0.30f;
    [SerializeField] private float textFadeIn = 0.35f;
    [SerializeField] private float textHold = 1.00f;
    [SerializeField] private float textFadeOut = 0.60f;

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;

        var anim = other.GetComponentInChildren<AnimatedSprite>();
        var player = other.GetComponent<Player>();

        player?.BeginRedBullSequence();
        anim?.FreezeAllAnimation();

        var col = GetComponent<Collider>(); if (col) col.enabled = false;
        var sr = GetComponent<SpriteRenderer>(); if (sr) sr.enabled = false;

        StartCoroutine(DrinkSequence(anim, player));
    }

    private IEnumerator DrinkSequence(AnimatedSprite anim, Player player)
    {
        if (anim == null)
        {
            GameManager.Instance?.ForceFinalFreeze();
            Destroy(gameObject);
            yield break;
        }

        anim.ShowDrink1Frame();
        yield return new WaitForSecondsRealtime(drinkFrameHold);

        anim.ShowDrink2Frame();
        yield return new WaitForSecondsRealtime(drinkFrameHold);

        anim.ShowDrink3Frame();
        yield return new WaitForSecondsRealtime(drinkFrameHold);

        anim.ShowWingsFrame();
        yield return new WaitForSecondsRealtime(wingsPreLoopHold);

        anim.StartFinalWingsLoop();
        GameManager.Instance?.PlayWingsFX(anim.transform.position);

        TMP_Text t = GameManager.Instance != null ? GameManager.Instance.wingsText : null;
        if (t != null)
        {
            t.gameObject.SetActive(true);
            t.alpha = 0f;

            float e = 0f;
            while (e < textFadeIn)
            {
                e += Time.unscaledDeltaTime;
                t.alpha = Mathf.Clamp01(e / Mathf.Max(0.0001f, textFadeIn));
                yield return null;
            }
            t.alpha = 1f;

            GameManager.Instance?.ForceFinalFreeze();

            if (textHold > 0f)
                yield return new WaitForSecondsRealtime(textHold);

            e = 0f;
            while (e < textFadeOut)
            {
                e += Time.unscaledDeltaTime;
                t.alpha = 1f - Mathf.Clamp01(e / Mathf.Max(0.0001f, textFadeOut));
                yield return null;
            }
            t.alpha = 0f;
        }
        else
        {
            GameManager.Instance?.ForceFinalFreeze();
        }

        GameManager.Instance?.AdvanceToNextPart();

        Destroy(gameObject);
    }
}
