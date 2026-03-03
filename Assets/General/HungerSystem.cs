using UnityEngine;

public class HungerSystem : MonoBehaviour
{
    public enum HungerStage { Hungry, Full, Starving}

    [SerializeField] HungerStage stage = HungerStage.Full;
    public HungerStage Stage => stage;
}
