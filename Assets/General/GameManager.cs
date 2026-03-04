using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Spawning")]
    public Camera cam;
    public GameObject fishPrefab;
    public int fishCount = 8;
    public GameObject crabPrefab;
    public int crabCount = 4;
    public GameObject jellyPrefab;
    public int jellyCount = 3;

    [Header("Spawn Area")]
    public float sidePadding = 0.8f;
    public float bottomPadding = 0.6f;
    public float topPadding = 0.6f;

    [Header("Empty Aquarium End")]
    public float emptyGraceTime = 3f;

    float emptyTimer;

    void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

    void Start()
    {
        SpawnGroup(fishPrefab, fishCount);
        SpawnGroup(crabPrefab, crabCount);
        SpawnGroup(jellyPrefab, jellyCount);
    }

    void Update()
    {
        int alive = CountAliveCreatures();
        if (alive <= 0)
        {
            emptyTimer += Time.deltaTime;
            if (emptyTimer >= emptyGraceTime)
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }
        else
        {
            emptyTimer = 0f;
        }
    }

    void SpawnGroup(GameObject prefab, int count)
    {
        if (prefab == null || count <= 0) return;

        for (int i = 0; i < count; i++)
        {
            Vector3 p = RandomSpawnPoint();
            Instantiate(prefab, p, Quaternion.identity);
        }
    }

    Vector3 RandomSpawnPoint()
    {
        float z = Mathf.Abs(cam.transform.position.z);

        Vector3 bl = cam.ViewportToWorldPoint(new Vector3(0f, 0f, z));
        Vector3 tr = cam.ViewportToWorldPoint(new Vector3(1f, 1f, z));

        float xMin = bl.x + sidePadding;
        float xMax = tr.x - sidePadding;
        float yMin = bl.y + bottomPadding;
        float yMax = tr.y - topPadding;

        float x = Random.Range(xMin, xMax);
        float y = Random.Range(yMin, yMax);

        return new Vector3(x, y, 0f);
    }

    int CountAliveCreatures()
    {
        int count = 0;

        if (fishPrefab != null)
        {
            var all = FindObjectsByType<FishMovement>(FindObjectsSortMode.None);
            count += all.Length;
        }

        if (crabPrefab != null)
        {
            var all = FindObjectsByType<CrabMovement>(FindObjectsSortMode.None);
            count += all.Length;
        }

        if (jellyPrefab != null)
        {
            var all = FindObjectsByType<JellyMovement>(FindObjectsSortMode.None);
            count += all.Length;
        }

        return count;
    }
}