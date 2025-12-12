using UnityEngine;

public class Ground : MonoBehaviour
{
    private MeshRenderer meshRenderer;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
    }

    private void Update()
    {
        if (GameManager.Instance == null)
            return;

        float speed = GameManager.Instance.baseSpeed * GameManager.Instance.speedMultiplier;
        Vector2 offset = Vector2.right * speed * Time.deltaTime;

        meshRenderer.material.mainTextureOffset += offset;
    }
}
