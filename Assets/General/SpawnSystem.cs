using System.Collections.Generic;
using UnityEngine;

public class SpawnSystem : MonoBehaviour
{
    public GameObject prefab;

    public float maxChance = 0.95f;
    public float minChance = 0.05f;
    public int dropoffAt = 10;

    public float spawnCooldown = 3f;
    public float speciesCooldown = 1.5f;

    HungerSystem hunger;
    LifeCycle life;

    float nextSpawnTime;

    static Dictionary<int, int> population = new Dictionary<int, int>();
    static Dictionary<int, float> nextSpeciesSpawnTime = new Dictionary<int, float>();

    void Awake()
    {
        hunger = GetComponent<HungerSystem>();
        life = GetComponent<LifeCycle>();
        if (prefab == null) prefab = gameObject;
    }

    void OnEnable()
    {
        int layer = gameObject.layer;
        if (!population.ContainsKey(layer)) population[layer] = 0;
        population[layer]++;

        if (!nextSpeciesSpawnTime.ContainsKey(layer)) nextSpeciesSpawnTime[layer] = 0f;
    }

    void OnDisable()
    {
        int layer = gameObject.layer;
        if (population.ContainsKey(layer)) population[layer] = Mathf.Max(0, population[layer] - 1);
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.layer != gameObject.layer) return;

        var other = col.gameObject.GetComponent<SpawnSystem>();
        if (other == null) return;

        if (GetInstanceID() > other.GetInstanceID()) return;

        if (Time.time < nextSpawnTime) return;
        if (Time.time < other.nextSpawnTime) return;

        int layer = gameObject.layer;
        if (Time.time < nextSpeciesSpawnTime[layer]) return;

        if (hunger == null || other.hunger == null) return;
        if (hunger.Stage != HungerSystem.HungerStage.Full) return;
        if (other.hunger.Stage != HungerSystem.HungerStage.Full) return;

        if (life == null || other.life == null) return;
        if (life.Stage != LifeCycle.LifeStage.Adult) return;
        if (other.life.Stage != LifeCycle.LifeStage.Adult) return;

        int count = population[layer];

        float maxC = Mathf.Clamp01(maxChance);
        float minC = Mathf.Clamp01(minChance);
        int d = Mathf.Max(1, dropoffAt);

        float k = Mathf.Log(maxC / Mathf.Max(0.0001f, minC)) / d;
        float chance = Mathf.Clamp(minC, minC, maxC);
        chance = Mathf.Clamp(minC + (maxC - minC) * Mathf.Exp(-k * count), minC, maxC);

        if (Random.value > chance) return;

        Vector3 pos = col.GetContact(0).point;
        Instantiate(prefab, pos, Quaternion.identity);

        float t = Time.time + spawnCooldown;
        nextSpawnTime = t;
        other.nextSpawnTime = t;
        nextSpeciesSpawnTime[layer] = Time.time + speciesCooldown;
    }
}