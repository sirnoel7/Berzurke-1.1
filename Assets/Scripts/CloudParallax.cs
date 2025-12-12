using UnityEngine;

public class CloudParallax : MonoBehaviour
{
    public float speed = 0.3f;
    public float destroyX = -12f;

    void Update()
    {
        transform.position += Vector3.left * speed * Time.deltaTime;

        if (transform.position.x <= destroyX)
            Destroy(gameObject);
    }
}
