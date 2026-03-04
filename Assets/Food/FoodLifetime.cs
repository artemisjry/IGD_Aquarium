using UnityEngine;

public class FoodLifetime : MonoBehaviour
{
    public float lifetime = 12f;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }
}