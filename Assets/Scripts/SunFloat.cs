using UnityEngine;

public class SunFloat : MonoBehaviour
{
    [Header("Horizontal Drift")]
    public float speed = 0.05f;

    [Header("Loop Settings")]
    public float resetX = -30f;
    public float respawnX = 30f;
    public float fixedY = 4.2f;

    [Header("Vertical Bobbing")]
    public float floatAmplitude = 0.08f;
    public float floatFrequency = 0.25f;

    [Header("Glow Pulse")]
    public float pulseAmplitude = 0.05f;
    public float pulseFrequency = 1.2f;
    public float glowIntensity = 0.15f;

    private float baseY;
    private Vector3 baseScale;
    private SpriteRenderer sr;

    void Start()
    {
        baseY = fixedY;
        baseScale = transform.localScale;
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // horizontal drift
        transform.position += Vector3.left * speed * Time.deltaTime;

        // looping
        if (transform.position.x <= resetX)
        {
            transform.position = new Vector3(respawnX, baseY, transform.position.z);
        }

        // bobbing
        float newY = baseY + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        // glow pulse
        float pulse = Mathf.Sin(Time.time * pulseFrequency) * pulseAmplitude;

        // scale pulse
        transform.localScale = baseScale * (1f + pulse);

        // brightness pulse
        if (sr != null)
        {
            float brightness = 1f + pulse * glowIntensity;
            sr.color = new Color(brightness, brightness, brightness, 1f);
        }
    }
}
