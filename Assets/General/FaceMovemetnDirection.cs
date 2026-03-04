using UnityEngine;

public class FaceMovementDirection : MonoBehaviour
{
    public Rigidbody2D rb;
    public float turnSpeed = 12f;
    public bool flipXInsteadOfRotate = false;
    public float minSpeedToTurn = 0.2f;
    public float spriteAngleOffset = 0f;
    public bool invertFlipX = false;
    float startupDelay = 0.1f;
    float startTime;

    private void Awake()
    {
        startTime = Time.time; //count time
    }

    void LateUpdate()
    {
        if (Time.time - startTime < startupDelay) return; //wait to start turning
        if (rb == null) return;

        Vector2 v = rb.linearVelocity;
        if (v.magnitude < minSpeedToTurn) return; //only turns above certain speed

        Vector2 vel = rb.linearVelocity; //movement direction
        if (vel.sqrMagnitude < 0.001f) return; //ignore jitter

        float angle = Mathf.Atan2(vel.y, vel.x) * Mathf.Rad2Deg; //convert velocity to angle

        if (flipXInsteadOfRotate)
        {
            // Fish only flips left/right
            if (Mathf.Abs(vel.x) > 0.05f) //only when there is horizontal movement
            {
                Vector3 s = transform.localScale; //current scale
                float sign = Mathf.Sign(vel.x);
                if (invertFlipX) sign *= -1f;
                s.x = sign * Mathf.Abs(s.x); //1 or -1
                transform.localScale = s; //apply flip
            }
        }
        else
        {
            // Full smooth rotation
            Quaternion target = Quaternion.Euler(0f, 0f, angle + spriteAngleOffset); //rotates around z axis
            transform.rotation = Quaternion.Slerp(transform.rotation, target, 1f - Mathf.Exp(-turnSpeed * Time.deltaTime)); //smooth turning towards velocity
        }
    }
}