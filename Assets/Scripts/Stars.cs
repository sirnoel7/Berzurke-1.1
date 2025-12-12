using UnityEngine;

public class Stars : MonoBehaviour
{
    private MeshRenderer meshRenderer;
    private float starSpeed = 0.1f;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
    }

    private void Update()
    {
        meshRenderer.material.mainTextureOffset += new Vector2(starSpeed * Time.deltaTime, 0);
    }
}