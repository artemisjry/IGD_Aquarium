//GOAL: make a "fish swimming movement" code that is adaptable across projects
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))] //ensures that there is a RigidBody2D Component
public class FishMovement : MonoBehaviour
{
    [Header("Movement Behavior")]
    public float moveSpeed = 2.5f; //+Assertive movement
    public float changeSpeed = 0.35f; //+Erratic Movement
    public float turnSharpness = 6f; //+Sharp Turns
    public float minDirectionHold = 0.6f; //minimal length a direction is held in seconds

    [Header("Avoid Boundaries")]
    public float avoidStrength = 3.0f; //how strong the avoidance influences movement
    public float reactionSpeed = 10f; //+reacts to obstacles more rapidly
    public float avoidHoldTime = 0.25f; //hold on to last non-zero avoid
    public float avoidReleaseSpeed = 6f; //how quickly avoid fades when safe


    //Housekeeping:
    Rigidbody2D rb;
    AvoidBoundary avoidBoundary;
    LifeCycle lifeStage;
    Camera cam;

    private SpeedCap speedCap; //max speed component

    Vector2 move; //direction of general movement
    Vector2 avoid; //direction of avoid

    float seedX; //randomize horizontal movement
    float seedY; //randomize vertical movement

    float nextRetargetTime = 0f;
    Vector2 heldTargetDir;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>(); //set rb to attached RigidBody2D
        lifeStage = GetComponent<LifeCycle>();
        avoidBoundary = rb.GetComponent<AvoidBoundary>(); //reference the attached AvoidBoundary

        avoidBoundary.SetIntensity(reactionSpeed, avoidHoldTime, avoidReleaseSpeed); //set parameters for avoid behavior

        //randomize seed
        //multiply by 1000 so changes in Perlin noise is actually noticable
        seedX = Random.value * 1000f;
        seedY = Random.value * 1000f;

        move = Random.insideUnitCircle.normalized; //provides a starting point for movement
        //insideUnitCircle picks a point in a unit circle to form a vector from the center
        //then normalized forces its magnitude to be 1 while maintaining its direction
        avoid = Vector2.zero; //which way the object should head to avoid boundary, starts neutral

        //initial hold direction
        heldTargetDir = move;
        nextRetargetTime = Time.time; //start timer
    }

    private void FixedUpdate() //things that need to be consistent across frame rates go here
    {

        //wander direction
        float t = Time.time;

        if (t >= nextRetargetTime) //when hold time is up
        {
            float x = Mathf.PerlinNoise(seedX, t * changeSpeed) * 2f - 1f; //new x
            float y = Mathf.PerlinNoise(seedY, t * changeSpeed) * 2f - 1f; //new y
            Vector2 targetDir = new Vector2(x, y); //new direction

            if (targetDir.sqrMagnitude < 0.0001f) targetDir = move; //safeguard against near 0 targetDir

            heldTargetDir = targetDir.normalized;

            nextRetargetTime = t + minDirectionHold;
        }

        Vector2 desiredDir = heldTargetDir.sqrMagnitude > 0.0001f ? heldTargetDir.normalized : move; 
        //if time isn't up yet, set desired direction to held target, or fall back to move if near 0

        avoidBoundary.fallbackMoveDir = desiredDir; //set fallbackMoveDir for avoidance component

        //avoid direction
        Vector2 avoidDir = avoidBoundary.ComputeAvoidDirection(); //reference avoid boundary component for avoid direction

        //actual direction
        Vector2 combinedDir = desiredDir + avoid * avoidStrength;
        if (combinedDir.sqrMagnitude < 0.0001f)
            combinedDir = desiredDir;

        combinedDir.Normalize();

        move = Vector2.Lerp(move, combinedDir, 1f - Mathf.Exp(-turnSharpness * Time.fixedDeltaTime)); //smooths out final movement direction

        rb.AddForce(move * moveSpeed * lifeStage.moveSpeedMult); //actually applies force

        float speed = rb.linearVelocity.magnitude;
        if (speed > speedCap.maxSpeed)
            rb.linearVelocity = rb.linearVelocity / speed * speedCap.maxSpeed; //keep only direction, then multiply by maxSpeed
    }
}

