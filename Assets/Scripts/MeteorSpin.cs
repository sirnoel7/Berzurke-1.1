// Assets/Scripts/MeteorSpin.cs
using UnityEngine;

/// <summary>
/// Optional visual spin for meteors (about Z axis).
/// </summary>
public class MeteorSpin : MonoBehaviour
{
    public float minSpin = -180f;
    public float maxSpin = 180f;
    private float spin;

    private void Awake()
    {
        spin = Random.Range(minSpin, maxSpin);
    }

    private void Update()
    {
        transform.Rotate(0f, 0f, spin * Time.deltaTime, Space.Self);
    }
}
