using UnityEngine;

public class NewGround : MonoBehaviour
{
    private Material mat;
    private float offset;

    public float visualSpeedMultiplier = 0.1f;

    void Start()
    {
        mat = GetComponent<SpriteRenderer>().material;
    }

    void Update()
    {
        if (GameManager.Instance == null)
            return;

        float speed = GameManager.Instance.baseSpeed * GameManager.Instance.speedMultiplier;

        if (speed <= 0f)
            return;

        offset += (speed * visualSpeedMultiplier) * Time.deltaTime;
        mat.mainTextureOffset = new Vector2(offset, 0f);
    }
}
