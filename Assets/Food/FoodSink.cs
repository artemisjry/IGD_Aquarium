using UnityEngine;

public class FoodSink : MonoBehaviour
{
    public Camera cam;
    public float sinkSpeed = 1.6f;
    public float bottomPadding = 0.35f;

    void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

    void Update()
    {
        float z = Mathf.Abs(cam.transform.position.z);
        float bottomY = cam.ViewportToWorldPoint(new Vector3(0.5f, 0f, z)).y + bottomPadding;

        Vector3 p = transform.position;
        p.y = Mathf.MoveTowards(p.y, bottomY, sinkSpeed * Time.deltaTime);
        transform.position = p;
    }
}