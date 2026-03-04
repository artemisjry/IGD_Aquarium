using UnityEngine;

public class FoodSpawn : MonoBehaviour
{
    public Camera cam;
    public GameObject foodPrefab;
    public float spawnInterval = 1.25f;
    public float spawnYAboveScreen = 1.0f;

    public float sidePadding = 1.0f;
    public float padding = 0.4f;

    float nextSpawn;

    void Awake()
    {
        if (cam == null) cam = Camera.main;
        nextSpawn = Time.time + spawnInterval;
    }

    void Update()
    {
        if (Time.time < nextSpawn) return;
        nextSpawn = Time.time + spawnInterval;

        float z = Mathf.Abs(cam.transform.position.z);

        Vector3 left = cam.ViewportToWorldPoint(new Vector3(0f, 0.5f, z));
        Vector3 right = cam.ViewportToWorldPoint(new Vector3(1f, 0.5f, z));
        Vector3 top = cam.ViewportToWorldPoint(new Vector3(0.5f, 1f, z));

        float xMin = left.x + sidePadding;
        float xMax = right.x - sidePadding;

        float x = Random.Range(xMin, xMax);
        float y = top.y + spawnYAboveScreen + padding;

        Instantiate(foodPrefab, new Vector3(x, y, 0f), Quaternion.identity);
    }
}