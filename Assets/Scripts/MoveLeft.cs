using UnityEngine;

public class MoveLeft : MonoBehaviour
{
    private void Update()
    {
        float speed = GameManager.Instance.baseSpeed * GameManager.Instance.speedMultiplier;

        transform.position += Vector3.left * speed * Time.deltaTime;

        if (transform.position.x < -20f)
            Destroy(gameObject);
    }
}
