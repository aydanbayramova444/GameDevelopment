using UnityEngine;

// Put this on the greens (göyərti) prefab, next to the Filling script.
// Works like Meat.cs: tells the greens dispenser when the player takes the greens.
public class Greens : MonoBehaviour
{
    [HideInInspector] public GreensDispenser sourceDispenser;

    // Called when the player picks up the greens
    public void OnPickedUp()
    {
        if (sourceDispenser != null)
        {
            sourceDispenser.NotifyGreensTaken();
            sourceDispenser = null;
        }
    }
}
