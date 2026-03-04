//GOAL: make a "fish swimming movement" code that is adaptable across projects
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))] //ensures that there is a RigidBody2D Component
public class AvoidBoundary : MonoBehaviour
{
    [Header("Boundaries")] //accounts for different types of boundaries
    public Collider2D colliderBoundary;
    public RectTransform UIBoundary;
    public Camera cameraBoundary;
    public bool avoidHorizontal = true;
    public bool avoidVertical = true;

    [Header("Boundary Behavior")]
    public float edgePaddingWorld = 0.2f; //to keep full collider within bounds, not just the pivot
    public float approachDistance = 1.0f; //how close before turn
    public float lookAheadTime = 0.35f; //how far predictions go based on current velocity
    [HideInInspector] public float reactionSpeed; //+reacts to obstacles more rapidly
    [HideInInspector] public float avoidHoldTime; //hold on to last non-zero avoid
    [HideInInspector] public float avoidReleaseSpeed; //how quickly avoid fades when safe

    //Housekeeping:
    [HideInInspector]
    public Vector2 fallbackMoveDir; //a fall back move direction that can be set by the movement script

    Rigidbody2D rb;

    private SpeedCap speedCap;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>(); //set rb to attached RigidBody2D
        speedCap = GetComponent<SpeedCap>(); //reference MaxSpeed component
        cameraBoundary = Camera.main;
    }

    public void SetIntensity(float reaction, float hold, float release) //lets movement component set these numbers
    {
        reactionSpeed = reaction;
        avoidHoldTime = hold;
        avoidReleaseSpeed = release;
    }

    public Vector2 ComputeAvoidDirection()
    {
        Camera c = cameraBoundary != null ? cameraBoundary : Camera.main;

        Bounds b = GetWorldBounds();
        Vector2 half = b.extents;

        Vector2 pos = rb.position;
        Vector2 vel = rb.linearVelocity;
        float speed = vel.magnitude;

        float maxSpeed = (speedCap != null) ? Mathf.Max(0.0001f, speedCap.maxSpeed) : Mathf.Max(0.0001f, speed);
        float lookAhead = lookAheadTime * Mathf.Clamp01(speed / maxSpeed);
        Vector2 predicted = pos + vel * lookAhead;

        Vector2 avoid;

        if (colliderBoundary != null)
        {
            Vector2 closestNow = colliderBoundary.ClosestPoint(pos);
            if (closestNow != pos)
            {
                rb.position = closestNow;
                rb.linearVelocity = Vector2.zero;
                pos = rb.position;
                predicted = pos;
            }

            avoid = AvoidUsingCollider(colliderBoundary, predicted);
        }
        else if (UIBoundary != null)
        {
            EnforceUsingUIRect(UIBoundary, half);
            avoid = AvoidUsingUIRect(UIBoundary, predicted, half);
        }
        else
        {
            EnforceUsingCameraViewport(c, half);
            avoid = AvoidUsingCameraViewport(c, predicted, half);
        }

        if (!avoidHorizontal) avoid.x = 0f;
        if (!avoidVertical) avoid.y = 0f;

        return avoid;
    }

    Vector2 AvoidUsingCollider(Collider2D area, Vector2 predicted) //(the collider used, estimated future position)
    {
        //am I even in the boundary?
        Vector2 closest = area.ClosestPoint(predicted);
        //returns the closest point to predicted on the collider
        //if predicted is within the collider, it will return itself
        bool inside = (closest == predicted);
        //therefore, if closest == predicted, the object must be within the collider

        //if I am outside, head back now!
        if (!inside)
        {
            Vector2 inDir = closest - predicted; //gives direction that points straight back
            return inDir;
        }

        //if I am inside, am I about to leave?
        Vector2 fwd = rb.linearVelocity.sqrMagnitude > 0.0001f ? //where are we heading?
            rb.linearVelocity.normalized : fallbackMoveDir.normalized; //if velocity is near 0, fall back to moveDir

        Vector2 probe = predicted + fwd * approachDistance; //where I will be + direction I'm heading * how far to check
        Vector2 probeClosest = area.ClosestPoint(probe); //same as with closest before
        bool probeInside = (probeClosest == probe);

        if (!probeInside)
        {
            Vector2 away = predicted - probeClosest; //points away from boundary
            return away;
        }

        return Vector2.zero; //if not near boundary, do not interfere
    }

    Vector2 AvoidUsingCameraViewport(Camera c, Vector2 predicted, Vector2 half) //half = half of object's size
    {
        float z = -c.transform.position.z; //camera distance

        Vector3 min = c.ViewportToWorldPoint(new Vector3(0f, 0f, z)); //bottom left
        Vector3 max = c.ViewportToWorldPoint(new Vector3(1f, 1f, z)); //top right
        //convert viewport corners to world space

        float left = min.x + edgePaddingWorld + half.x;
        float right = max.x - edgePaddingWorld - half.x;
        float bottom = min.y + edgePaddingWorld + half.y;
        float top = max.y - edgePaddingWorld - half.y;
        //shrink bounds so object is always within

        return AvoidUsingRect(predicted, left, right, bottom, top); //pass through one generic function
    }

    Vector2 AvoidUsingUIRect(RectTransform rect, Vector2 predicted, Vector2 half)
    {
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        //UI corners in world space

        float left = corners[0].x + edgePaddingWorld + half.x;
        float right = corners[2].x - edgePaddingWorld - half.x;
        float bottom = corners[0].y + edgePaddingWorld + half.y;
        float top = corners[2].y - edgePaddingWorld - half.y;

        return AvoidUsingRect(predicted, left, right, bottom, top);
    }

    Vector2 AvoidUsingRect(Vector2 predicted, float left, float right, float bottom, float top)
    {
        Vector2 avoid = Vector2.zero; //start with no avoidance

        float dl = predicted.x - left;
        if (dl < approachDistance) avoid.x += (approachDistance - dl) / Mathf.Max(0.0001f, approachDistance);

        float dr = right - predicted.x;
        if (dr < approachDistance) avoid.x -= (approachDistance - dr) / Mathf.Max(0.0001f, approachDistance);

        float db = predicted.y - bottom;
        if (db < approachDistance) avoid.y += (approachDistance - db) / Mathf.Max(0.0001f, approachDistance);

        float dt = top - predicted.y;
        if (dt < approachDistance) avoid.y -= (approachDistance - dt) / Mathf.Max(0.0001f, approachDistance);

        if (avoid.sqrMagnitude < 0.0001f)
            return Vector2.zero; //if no edge is detected
        return avoid;
    }

    void EnforceUsingCameraViewport(Camera c, Vector2 half)
    {
        float z = -c.transform.position.z;

        Vector3 min = c.ViewportToWorldPoint(new Vector3(0f, 0f, z));
        Vector3 max = c.ViewportToWorldPoint(new Vector3(1f, 1f, z));

        float left = min.x + edgePaddingWorld + half.x;
        float right = max.x - edgePaddingWorld - half.x;
        float bottom = min.y + edgePaddingWorld + half.y;
        float top = max.y - edgePaddingWorld - half.y;

        Vector2 p = rb.position;
        float x = Mathf.Clamp(p.x, left, right);
        float y = Mathf.Clamp(p.y, bottom, top);

        if (x != p.x || y != p.y)
        {
            rb.position = new Vector2(x, y);
            rb.linearVelocity = Vector2.zero;
        }
    }

    void EnforceUsingUIRect(RectTransform rect, Vector2 half)
    {
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);

        float left = corners[0].x + edgePaddingWorld + half.x;
        float right = corners[2].x - edgePaddingWorld - half.x;
        float bottom = corners[0].y + edgePaddingWorld + half.y;
        float top = corners[2].y - edgePaddingWorld - half.y;

        Vector2 p = rb.position;
        float x = Mathf.Clamp(p.x, left, right);
        float y = Mathf.Clamp(p.y, bottom, top);

        if (x != p.x || y != p.y)
        {
            rb.position = new Vector2(x, y);
            rb.linearVelocity = Vector2.zero;
        }
    }

    public float GetBottom()
    {
        Camera c = cameraBoundary;

        float halfHeight = 0f;
        var col = GetComponent<Collider2D>();
        if (col != null)
            halfHeight = col.bounds.extents.y;

        // 1) Collider boundary
        if (colliderBoundary != null)
            return colliderBoundary.bounds.min.y + edgePaddingWorld + halfHeight;

        // 2) UI boundary
        if (UIBoundary != null)
        {
            Vector3[] corners = new Vector3[4];
            UIBoundary.GetWorldCorners(corners);
            return corners[0].y + edgePaddingWorld + halfHeight;
        }

        // 3) Camera boundary
        if (c != null)
        {
            float z = transform.position.z - c.transform.position.z;
            float camBottom = c.ViewportToWorldPoint(new Vector3(0f, 0f, z)).y;
            return camBottom + edgePaddingWorld + halfHeight;
        }

        return transform.position.y;
    }

    Bounds GetWorldBounds()
    {
        var col = GetComponent<Collider2D>();
        return col.bounds; //size of object collider
    }
}