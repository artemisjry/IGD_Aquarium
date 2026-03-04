//GOAL: make a "fish swimming movement" code that is adaptable across projects
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class JellyMovement : MonoBehaviour
{
    [Header("Movement Behavior")]
    public float moveSpeed = 2.5f;
    public float changeSpeed = 0.35f;
    public float turnSharpness = 6f;
    public float minDirectionHold = 0.6f;
    HungerSystem hunger;

    [Header("Jellyfish Burst")]
    public int burstAnimFrames = 8;
    public float burstAnimFps = 12f;
    [Range(0.05f, 0.95f)] public float burstDuty = 0.35f;
    public float burstStrength = 1.35f;

    public float burstImpulseMult = 1.0f;
    public float burstDamping = 0.2f;
    public float postBurstDamping = 6.0f;
    public float dampingReturnSpeed = 6.0f;

    [Header("Animation")]
    public Animator animator;
    public string burstParam = "Burst";

    [Header("Avoid Boundaries")]
    public float avoidStrength = 3.0f;
    public float reactionSpeed = 10f;
    public float avoidHoldTime = 0.25f;
    public float avoidReleaseSpeed = 6f;

    public float boundaryBounceImpulse = 0.9f;
    public float boundaryBounceCooldown = 0.25f;
    [Range(0f, 1f)] public float bounceInwardBias = 0.75f;

    [Header("Bump Reaction")]
    [Range(0f, 1f)] public float bumpStrength = 0.6f;
    public float bumpCooldown = 0.12f;

    Vector2 bumpSteer;
    float bumpSteerStrength;
    float lastBumpTime;

    float bumpImpulseAtMax;
    float bumpSteerAtMax;
    float bumpImpactForMax;
    float bumpSteerDecay;

    Rigidbody2D rb;
    AvoidBoundary avoidBoundary;
    LifeCycle lifeStage;

    private SpeedCap speedCap;

    Vector2 move;
    Vector2 avoid;

    float seedX;
    float seedY;

    float nextRetargetTime = 0f;
    Vector2 heldTargetDir;

    float burstTimer;
    float burstPeriod;
    float burstOnTime;

    bool prevInBurst;

    float baseLinearDamping;
    float dampingTarget;

    float lastBoundaryBounceTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        lifeStage = GetComponent<LifeCycle>();
        avoidBoundary = rb.GetComponent<AvoidBoundary>();
        speedCap = rb.GetComponent<SpeedCap>();
        hunger = GetComponent<HungerSystem>();

        if (avoidBoundary != null)
            avoidBoundary.SetIntensity(reactionSpeed, avoidHoldTime, avoidReleaseSpeed);

        baseLinearDamping = rb.linearDamping;
        dampingTarget = baseLinearDamping;

        seedX = Random.value * 1000f;
        seedY = Random.value * 1000f;

        move = Random.insideUnitCircle.normalized;
        avoid = Vector2.zero;

        heldTargetDir = move;
        nextRetargetTime = Time.time;

        RecomputeBump();
        RecomputeBurstTiming();

        burstTimer = Random.value * burstPeriod;
        prevInBurst = false;
        lastBoundaryBounceTime = -999f;
    }

    void OnValidate()
    {
        RecomputeBurstTiming();
    }

    void RecomputeBurstTiming()
    {
        burstAnimFrames = Mathf.Max(1, burstAnimFrames);
        burstAnimFps = Mathf.Max(0.01f, burstAnimFps);

        burstPeriod = burstAnimFrames / burstAnimFps;
        burstOnTime = burstPeriod * Mathf.Clamp01(burstDuty);
    }

    void RecomputeBump()
    {
        bumpImpulseAtMax = Mathf.Lerp(0.6f, 2.2f, bumpStrength);
        bumpSteerAtMax = Mathf.Lerp(0.3f, 1.2f, bumpStrength);
        bumpImpactForMax = Mathf.Lerp(6.0f, 2.0f, bumpStrength);
        bumpSteerDecay = Mathf.Lerp(12f, 4f, bumpStrength);
    }

    private void FixedUpdate()
    {
        if (lifeStage != null && lifeStage.Stage == LifeCycle.LifeStage.Dead) return;

        bumpSteer = Vector2.Lerp(bumpSteer, Vector2.zero, 1f - Mathf.Exp(-bumpSteerDecay * Time.fixedDeltaTime));

        float t = Time.time;

        if (t >= nextRetargetTime)
        {
            float x = Mathf.PerlinNoise(seedX, t * changeSpeed) * 2f - 1f;
            float y = Mathf.PerlinNoise(seedY, t * changeSpeed) * 2f - 1f;

            Vector2 targetDir = new Vector2(x, y);

            if (targetDir.sqrMagnitude < 0.0001f)
                targetDir = move;

            heldTargetDir = targetDir.normalized;
            nextRetargetTime = t + minDirectionHold;
        }

        Vector2 desiredDir = heldTargetDir.sqrMagnitude > 0.0001f ? heldTargetDir.normalized : move;

        if (hunger != null &&
            hunger.Stage != HungerSystem.HungerStage.Full &&
            hunger.FoodSteerDir.sqrMagnitude > 0.0001f)
        {
            desiredDir = hunger.FoodSteerDir.normalized;
        }

        if (avoidBoundary != null)
            avoidBoundary.fallbackMoveDir = desiredDir;

        Vector2 avoidDir = Vector2.zero;

        if (avoidBoundary != null)
        {
            avoidDir = avoidBoundary.ComputeAvoidDirection();
            avoid = Vector2.Lerp(avoid, avoidDir,
                1f - Mathf.Exp(-reactionSpeed * Time.fixedDeltaTime));
        }

        float edge = Mathf.Clamp01(avoidDir.magnitude);

        if (edge > 0.001f && Time.time - lastBoundaryBounceTime >= boundaryBounceCooldown)
        {
            Vector2 inward = avoidDir.sqrMagnitude > 0.0001f ? avoidDir.normalized : -move;

            Vector2 v = rb.linearVelocity;
            float outward = Vector2.Dot(v, -inward);

            if (outward > 0.15f)
            {
                Vector2 bounced = v + inward * outward;

                rb.linearVelocity = Vector2.Lerp(v, bounced, bounceInwardBias);

                float m = (lifeStage != null) ? lifeStage.SpeedMultiplier : 1f;
                rb.AddForce(inward * boundaryBounceImpulse * m, ForceMode2D.Impulse);

                lastBoundaryBounceTime = Time.time;
            }
        }

        burstTimer += Time.fixedDeltaTime;
        if (burstTimer >= burstPeriod)
            burstTimer -= burstPeriod;

        bool inBurst = burstTimer <= burstOnTime;

        if (animator != null)
            animator.SetBool(burstParam, inBurst);

        Vector2 combinedDir =
            desiredDir +
            avoid * avoidStrength +
            bumpSteer * bumpSteerStrength;

        if (combinedDir.sqrMagnitude < 0.0001f)
            combinedDir = desiredDir;

        combinedDir.Normalize();

        move = Vector2.Lerp(
            move,
            combinedDir,
            1f - Mathf.Exp(-turnSharpness * Time.fixedDeltaTime)
        );

        if (inBurst && !prevInBurst)
        {
            float m = lifeStage != null ? lifeStage.SpeedMultiplier : 1f;

            rb.AddForce(
                move * (moveSpeed * burstStrength * burstImpulseMult * m),
                ForceMode2D.Impulse
            );

            dampingTarget = burstDamping;
        }
        else if (!inBurst && prevInBurst)
        {
            dampingTarget = postBurstDamping;
        }
        else if (inBurst)
        {
            dampingTarget = burstDamping;
        }
        else
        {
            dampingTarget = baseLinearDamping;
        }

        rb.linearDamping = Mathf.Lerp(
            rb.linearDamping,
            dampingTarget,
            1f - Mathf.Exp(-Mathf.Max(0.01f, dampingReturnSpeed) * Time.fixedDeltaTime)
        );

        float speed = rb.linearVelocity.magnitude;

        if (speedCap != null && speed > speedCap.maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity / speed * speedCap.maxSpeed;
        }

        prevInBurst = inBurst;
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (lifeStage != null && lifeStage.Stage == LifeCycle.LifeStage.Dead) return;
        if (Time.time - lastBumpTime < bumpCooldown) return;
        if (col.gameObject.layer != gameObject.layer) return;

        Vector2 away = col.GetContact(0).normal;

        if (away.sqrMagnitude < 0.0001f) return;

        away.Normalize();

        float impact = col.relativeVelocity.magnitude;

        float s = Mathf.Clamp01(
            impact / Mathf.Max(0.0001f, bumpImpactForMax)
        );

        Vector2 edgeDir = avoidBoundary != null
            ? avoidBoundary.ComputeAvoidDirection()
            : Vector2.zero;

        float edge = Mathf.Clamp01(edgeDir.magnitude);

        if (edge > 0.001f)
        {
            Vector2 inward = edgeDir.normalized;

            away = Vector2.Lerp(away, inward, 0.85f).normalized;

            s *= Mathf.Lerp(1f, 0.25f, edge);
        }

        float impulse = bumpImpulseAtMax * s;

        bumpSteerStrength = bumpSteerAtMax * s;

        rb.AddForce(away * impulse, ForceMode2D.Impulse);

        bumpSteer = away;

        float maxV = speedCap != null
            ? speedCap.maxSpeed
            : float.PositiveInfinity;

        float v = rb.linearVelocity.magnitude;

        if (v > maxV)
            rb.linearVelocity = rb.linearVelocity / v * maxV;

        lastBumpTime = Time.time;
    }
}