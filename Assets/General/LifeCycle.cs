using System.Collections;
using UnityEngine;

public class LifeCycle : MonoBehaviour
{
    public enum LifeStage { Baby, Adult, Dead }

    [Header("Stage Settings")]
    public Sprite babySprite;
    public float babyScale = 0.5f;
    public float babySpeedMult = 0.7f;
    public float growTime = 30f;
    public AudioClip growSound;
    public Sprite adultSprite;
    public float adultScale = 1f;
    public float adultSpeedMult = 1f;
    public float lifespan = 120f;
    public AudioClip deathSound;

    [Header("References")]
    public Transform visual;
    public SpriteRenderer spriteRenderer;
    public Rigidbody2D rb;
    public AudioSource audioSource;
    public HungerSystem hungerSystem;
    public Animator animator;

    [Header("Death Behavior")]
    public Collider2D colliderBoundary;
    public RectTransform UIBoundary;
    public Camera cameraBoundary;
    public float sinkSpeed = 0.8f;
    public float stopTimeOnBottom = 0.25f;
    public float fadeTime = 0.25f;

    [Header("Debug")]
    [SerializeField] LifeStage stage = LifeStage.Baby;

    public LifeStage Stage => stage;
    public float SpeedMultiplier { get; private set; } = 1f;

    float stageEnterTime;
    float fullTime;
    Vector3 baseVisualScale;

    Coroutine deathCoroutine;

    AvoidBoundary avoid;

    RigidbodyType2D prevBodyType;
    bool prevSimulated;
    float prevGravityScale;
    float prevLinearDamping;
    float prevAngularDamping;
    bool physicsFrozen;

    void Awake()
    {
        avoid = GetComponent<AvoidBoundary>();
        spriteRenderer = visual.GetComponentInChildren<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        rb = GetComponent<Rigidbody2D>();
        baseVisualScale = transform.localScale;
        cameraBoundary = Camera.main;
        hungerSystem = GetComponent<HungerSystem>();
        animator = visual.GetComponent<Animator>();
    }

    void OnEnable()
    {
        EnterBaby();
    }

    void Update()
    {
        if (stage == LifeStage.Dead) return;

        if (stage == LifeStage.Baby)
        {
            if (hungerSystem != null && hungerSystem.Stage == HungerSystem.HungerStage.Full)
            {
                fullTime += Time.deltaTime;
                if (fullTime >= growTime) EnterAdult();
            }
        }
        else if (stage == LifeStage.Adult)
        {
            if (Time.time - stageEnterTime >= lifespan)
                Kill();
        }
    }

    void EnterBaby()
    {
        stage = LifeStage.Baby;
        stageEnterTime = Time.time;
        fullTime = 0f;
        spriteRenderer.sprite = babySprite;
        transform.localScale = baseVisualScale * babyScale;
        SpeedMultiplier = babySpeedMult;
        RestorePhysicsIfNeeded();
    }

    void EnterAdult()
    {
        stage = LifeStage.Adult;
        stageEnterTime = Time.time;
        spriteRenderer.sprite = adultSprite;
        transform.localScale = baseVisualScale * adultScale;
        SpeedMultiplier = adultSpeedMult;
        audioSource.PlayOneShot(growSound);
        RestorePhysicsIfNeeded();
    }

    public void Kill()
    {
        if (stage == LifeStage.Dead) return;

        if (hungerSystem != null) hungerSystem.EnterFull();
        stage = LifeStage.Dead;
        SpeedMultiplier = 0f;
        audioSource.PlayOneShot(deathSound);

        FreezePhysics();

        if (deathCoroutine != null) StopCoroutine(deathCoroutine);
        deathCoroutine = StartCoroutine(SinkThenDespawn());

        animator.speed = 0f;
    }

    void FreezePhysics()
    {
        if (rb == null || physicsFrozen) return;

        prevBodyType = rb.bodyType;
        prevSimulated = rb.simulated;
        prevGravityScale = rb.gravityScale;
        prevLinearDamping = rb.linearDamping;
        prevAngularDamping = rb.angularDamping;

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = false;

        physicsFrozen = true;
    }

    void RestorePhysicsIfNeeded()
    {
        if (rb == null || !physicsFrozen) return;

        rb.simulated = prevSimulated;
        rb.bodyType = prevBodyType;
        rb.gravityScale = prevGravityScale;
        rb.linearDamping = prevLinearDamping;
        rb.angularDamping = prevAngularDamping;

        physicsFrozen = false;
    }

    IEnumerator SinkThenDespawn()
    {
        float bottom = GetBottom();

        Vector3 p = transform.position;
        if (p.y < bottom) p.y = bottom;
        transform.position = p;

        while (transform.position.y > bottom)
        {
            Vector3 q = transform.position;
            q.y = Mathf.MoveTowards(q.y, bottom, sinkSpeed * Time.deltaTime);
            transform.position = q;
            yield return null;
        }

        yield return new WaitForSeconds(stopTimeOnBottom);

        float t = 0f;
        float startA = spriteRenderer.color.a;

        while (t < fadeTime)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / fadeTime);

            Color c = spriteRenderer.color;
            c.a = Mathf.Lerp(startA, 0f, u);
            spriteRenderer.color = c;

            yield return null;
        }

        Color end = spriteRenderer.color;
        end.a = 0f;
        spriteRenderer.color = end;

        Destroy(gameObject);
    }

    float GetBottom()
    {
        if (avoid != null)
            return avoid.GetBottom();

        return transform.position.y;
    }
}