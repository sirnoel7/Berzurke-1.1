using UnityEngine;
using System.Collections;

public class RedBullFinalController : MonoBehaviour
{
    public float slideSpeed = 2f;
    public float targetX = 0.5f;
    public float stopDistance = 0.05f;

    private SpriteRenderer sr;
    private bool sliding = false;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        Color c = sr.color;
        c.a = 0;
        sr.color = c;
    }

    private void Start()
    {
        StartCoroutine(FadeInThenSlide());
    }

    private IEnumerator FadeInThenSlide()
    {
        for (float a = 0; a <= 1f; a += Time.deltaTime * 1.6f)
        {
            Color c = sr.color;
            c.a = a;
            sr.color = c;
            yield return null;
        }

        sliding = true;
    }

    private void Update()
    {
        if (!sliding) return;

        Vector3 pos = transform.position;
        pos.x = Mathf.MoveTowards(pos.x, targetX, slideSpeed * Time.deltaTime);
        transform.position = pos;

        if (Mathf.Abs(transform.position.x - targetX) <= stopDistance)
        {
            sliding = false;
            Destroy(this);
        }
    }
}
