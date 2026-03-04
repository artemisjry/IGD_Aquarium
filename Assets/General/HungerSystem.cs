using UnityEngine;

public class HungerSystem : MonoBehaviour
{
    public enum HungerStage { Full, Hungry, Starving }

    public float hungerCycleDuration = 20f;
    public Color hungryColor = Color.white;
    public Color starvingColor = Color.white;

    public LayerMask foodLayer;
    public float foodDetectionRadius = 5f;

    public Transform visual;
    public SpriteRenderer spriteRenderer;
    public LifeCycle lifeCycle;

    [SerializeField] HungerStage stage = HungerStage.Full;
    public HungerStage Stage => stage;

    public Vector2 FoodSteerDir { get; private set; }

    float hungerTime;
    Transform lockedFood;
    Color baseColor;

    void Awake()
    {
        lifeCycle = GetComponent<LifeCycle>();

        if (spriteRenderer == null)
        {
            if (visual != null) spriteRenderer = visual.GetComponentInChildren<SpriteRenderer>(true);
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        }

        if (spriteRenderer != null) baseColor = spriteRenderer.color;

        hungerTime = Mathf.Max(0.01f, hungerCycleDuration);
    }

    void OnEnable()
    {
        EnterFull();
        lockedFood = null;
    }

    void Update()
    {
        float dur = Mathf.Max(0.01f, hungerCycleDuration);

        hungerTime -= Time.deltaTime;
        if (hungerTime <= 0f)
        {
            hungerTime = 0f;
            if (lifeCycle != null) lifeCycle.Kill();
        }

        float hungryAt = dur * 0.60f;
        float starvingAt = dur * 0.20f;

        HungerStage desired =
            hungerTime <= starvingAt ? HungerStage.Starving :
            hungerTime <= hungryAt ? HungerStage.Hungry :
            HungerStage.Full;

        stage = desired;

        ApplyColor();

        FoodSteerDir = Vector2.zero;

        if (stage == HungerStage.Hungry || stage == HungerStage.Starving)
        {
            FoodSteerDir = ComputeFoodSteerDirection();
        }
        else
        {
            lockedFood = null;
        }
    }

    public void EnterFull()
    {
        hungerTime = Mathf.Max(0.01f, hungerCycleDuration);
        stage = HungerStage.Full;
        lockedFood = null;
        ApplyColor();
    }

    void ApplyColor()
    {
        if (spriteRenderer == null) return;

        if (stage == HungerStage.Full) spriteRenderer.color = baseColor;
        else if (stage == HungerStage.Hungry) spriteRenderer.color = hungryColor;
        else spriteRenderer.color = starvingColor;
    }

    Vector2 ComputeFoodSteerDirection()
    {
        if (lockedFood != null)
        {
            Vector2 d = lockedFood.position - transform.position;
            if (d.sqrMagnitude > 0.0001f) return d.normalized;
        }

        Collider2D[] foods = Physics2D.OverlapCircleAll(transform.position, foodDetectionRadius, foodLayer);

        if (foods.Length == 0) return Vector2.zero;

        Transform closest = foods[0].transform;
        float best = (closest.position - transform.position).sqrMagnitude;

        for (int i = 1; i < foods.Length; i++)
        {
            float sq = (foods[i].transform.position - transform.position).sqrMagnitude;
            if (sq < best)
            {
                best = sq;
                closest = foods[i].transform;
            }
        }

        lockedFood = closest;

        Vector2 dir = lockedFood.position - transform.position;
        if (dir.sqrMagnitude > 0.0001f) return dir.normalized;

        return Vector2.zero;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if ((foodLayer.value & (1 << other.gameObject.layer)) == 0) return;

        float dur = Mathf.Max(0.01f, hungerCycleDuration);
        hungerTime = Mathf.Min(dur, hungerTime + dur * 0.2f);

        if (lockedFood == other.transform) lockedFood = null;

        Destroy(other.gameObject);
    }
}