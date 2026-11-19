using UnityEngine;

public class Meat : MonoBehaviour
{
    [HideInInspector] public MeatDispenser sourceDispenser;

    // Call this when the player picks up the meat
    public void OnPickedUp()
    {
        if (sourceDispenser != null)
        {
            sourceDispenser.NotifyMeatTaken();
            sourceDispenser = null;
        }
    }
}