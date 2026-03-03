using System;
using System.Collections;
using UnityEngine;

public class LifeCycle : MonoBehaviour
{
    public enum LifeStage { Baby, Adult, Dead };


    [Header("Stage Settings")]
    public Sprite babySprite;
    public float babyScale;
    public float babySpeedMult;
    public float growTime;
    public AudioClip growSound;
    public Sprite adultSprite;
    public float adultScale;
    public float adultSpeedMult;
    public float lifespan;
    public AudioClip deathSound;

    [Header("References")]
    public Transform visual;
    public SpriteRenderer spriteRenderer;
    public Rigidbody2D rb;
    public AudioSource audioSource;

    [Header("Death Behavior")]
    public Collider2D colliderBoundary;
    public RectTransform UIBoundary;
    public Camera cameraBoundary;
    public float sinkSpeed = 0.8f;
    public float bottomPadding = 0.05f;
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

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        baseVisualScale = visual.localScale;
    }

    private void OnEnable()
    {
        EnterBaby();
    }
    void Update()
    {
        if (stage == LifeStage.Dead) return;

        if (stage == LifeStage.Baby)
        {
            var hunger = GetComponent<HungerSystem>();
            if (hunger.Stage == HungerSystem.HungerStage.Full)
            {
                fullTime += Time.deltaTime;
                if (fullTime >= growTime) EnterAdult(); 
            }
        }

    }

    void EnterBaby()
    {
        stage = LifeStage.Baby;
        stageEnterTime = Time.time;
        fullTime = 0f;
        spriteRenderer.sprite = babySprite;
        visual.localScale = baseVisualScale * babyScale;
        SpeedMultiplier = babySpeedMult;
    }

    void EnterAdult()
    {
        stage = LifeStage.Adult;
        stageEnterTime = Time.time;
        spriteRenderer.sprite = babySprite;
        visual.localScale = baseVisualScale * babyScale;
        SpeedMultiplier = babySpeedMult;
        audioSource.PlayOneShot(growSound);
    }

    public void Kill()
    {
        if (stage == LifeStage.Dead) return;

        stage = LifeStage.Dead;
        SpeedMultiplier = 0f;
        audioSource.PlayOneShot(deathSound);

        deathCoroutine = StartCoroutine(SinkThenDespawn());
    }

    IEnumerator SinkThenDespawn()
    {

    }

    float GetBottom()
    {
        if (colliderBoundary)
            return colliderBoundary.bounds.min.y;

        if (cameraBoundary)
        {
            float z = -cameraBoundary.transform.position.z;
            return cameraBoundary.ViewportToWorldPoint(new Vector3(0f, 0f, z)).y;
        }

        return;
    }

}
