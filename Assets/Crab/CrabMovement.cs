using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;

[RequireComponent(typeof(Rigidbody2D))] //ensures that there is a RigidBody2D Component
public class CrabMovement : MonoBehaviour
{
    [Header("Movement Behavior")]
    public float moveSpeed = 4f; //+Assertive movement
    public float changeSpeed = 0.1f; //+Erratic Movement
    public float turnSharpness = 10f; //+Sharp Turns
    public float minDirectionHold = 0.6f; //minimal length a direction is held in seconds
    HungerSystem hunger;

    [Header("Avoid Boundaries")]
    public float avoidStrength = 3.0f; //how strong the avoidance influences movement
    public float reactionSpeed = 10f; //+reacts to obstacles more rapidly
    public float avoidHoldTime = 0.25f; //hold on to last non-zero avoid
    public float avoidReleaseSpeed = 6f; //how quickly avoid fades when safe

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

    //Housekeeping:
    Rigidbody2D rb;
    AvoidBoundary avoidBoundary;
    LifeCycle lifeStage;

    private SpeedCap speedCap; //max speed component

    Vector2 move; //direction of general movement
    Vector2 avoid; //direction of avoid

    float nextRetargetTime = 0f;
    Vector2 heldTargetDir;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>(); //set rb to attached RigidBody2D
        lifeStage = GetComponent<LifeCycle>();
        avoidBoundary = rb.GetComponent<AvoidBoundary>(); //reference the attached AvoidBoundary
        speedCap = rb.GetComponent<SpeedCap>();
        hunger = GetComponent<HungerSystem>();
        if (avoidBoundary != null) avoidBoundary.SetIntensity(reactionSpeed, avoidHoldTime, avoidReleaseSpeed); //set parameters for avoid behavior

        move = Random.value > 0.5f ? Vector2.right : Vector2.left;
        avoid = Vector2.zero;

        //initial hold direction
        heldTargetDir = move;
        nextRetargetTime = Time.time; //start timer
        RecomputeBump();
    }

    void RecomputeBump()
    {
        bumpImpulseAtMax = Mathf.Lerp(0.6f, 2.2f, bumpStrength);
        bumpSteerAtMax = Mathf.Lerp(0.3f, 1.2f, bumpStrength);
        bumpImpactForMax = Mathf.Lerp(6.0f, 2.0f, bumpStrength);
        bumpSteerDecay = Mathf.Lerp(12f, 4f, bumpStrength);
    }

    private void FixedUpdate() //things that need to be consistent across frame rates go here
    {
        bumpSteer = Vector2.Lerp(bumpSteer, Vector2.zero, 1f - Mathf.Exp(-bumpSteerDecay * Time.fixedDeltaTime));

        //wander direction
        float t = Time.time;

        if (t >= nextRetargetTime) //when hold time is up
        {
            Vector2 targetDir = Random.value > 0.5f ? Vector2.right : Vector2.left; ; //new direction

            heldTargetDir = targetDir;

            nextRetargetTime = t + minDirectionHold;
        }

        Vector2 desiredDir = heldTargetDir.sqrMagnitude > 0.0001f ? heldTargetDir.normalized : move;

        if (hunger != null && hunger.Stage != HungerSystem.HungerStage.Full && hunger.FoodSteerDir.sqrMagnitude > 0.0001f)
        {
            Vector2 foodDir = hunger.FoodSteerDir;
            foodDir.y = 0f;
            if (foodDir.sqrMagnitude > 0.0001f)
                desiredDir = foodDir.normalized;
        }

        if (avoidBoundary != null) avoidBoundary.fallbackMoveDir = desiredDir; //set fallbackMoveDir for avoidance component

        //avoid direction
        Vector2 avoidDir = (avoidBoundary != null) ? avoidBoundary.ComputeAvoidDirection() : Vector2.zero; //reference avoid boundary component for avoid direction

        avoid = Vector2.Lerp(avoid, avoidDir, 1f - Mathf.Exp(-reactionSpeed * Time.fixedDeltaTime));

        //actual direction
        Vector2 combinedDir = desiredDir + avoid * avoidStrength + bumpSteer * bumpSteerStrength;
        if (combinedDir.sqrMagnitude < 0.0001f)
            combinedDir = desiredDir;

        combinedDir.Normalize();

        move = Vector2.Lerp(move, combinedDir, 1f - Mathf.Exp(-turnSharpness * Time.fixedDeltaTime)); //smooths out final movement direction

        rb.AddForce(move * moveSpeed * lifeStage.SpeedMultiplier); //actually applies force

        LockToBottom();

        float speed = rb.linearVelocity.magnitude;
        if (speedCap != null && speed > speedCap.maxSpeed)
            rb.linearVelocity = rb.linearVelocity / speed * speedCap.maxSpeed; //keep only direction, then multiply by maxSpeed
    }

    void LockToBottom()
    {
        float bottom = avoidBoundary.GetBottom();

        // keep crab on bottom
        Vector2 p = rb.position;
        p.y = bottom;
        rb.position = p;

        // remove vertical drift
        Vector2 v = rb.linearVelocity;
        v.y = 0f;
        rb.linearVelocity = v;
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (Time.time - lastBumpTime < bumpCooldown) return;
        if (col.gameObject.layer != gameObject.layer) return;

        Vector2 away = col.GetContact(0).normal;
        if (away.sqrMagnitude < 0.0001f) return;
        away.Normalize();

        float impact = col.relativeVelocity.magnitude;
        float s = Mathf.Clamp01(impact / Mathf.Max(0.0001f, bumpImpactForMax));

        Vector2 edgeDir = (avoidBoundary != null) ? avoidBoundary.ComputeAvoidDirection() : Vector2.zero;
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

        float maxV = (speedCap != null) ? speedCap.maxSpeed : float.PositiveInfinity;
        float v = rb.linearVelocity.magnitude;
        if (v > maxV) rb.linearVelocity = rb.linearVelocity / v * maxV;

        lastBumpTime = Time.time;
    }
}