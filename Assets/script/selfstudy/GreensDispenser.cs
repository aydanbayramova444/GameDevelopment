using UnityEngine;

// Gives out greens (göyərti). Works like MeatDispenser:
// one portion at a time, then waits until the player takes it.
public class GreensDispenser : MonoBehaviour
{
    public GameObject greensPrefab;
    public Transform greensSpawnPoint;

    private bool hasGreens = false;

    public bool CanDispense => !hasGreens;

    public GameObject Dispense()
    {
        if (hasGreens)
        {
            Debug.Log("The greens dispenser is empty!");
            return null;
        }

        if (greensPrefab == null || greensSpawnPoint == null)
        {
            Debug.LogError("Assign the Greens Prefab and Spawn Point on " + name + "!");
            return null;
        }

        GameObject newGreens = Instantiate(
            greensPrefab,
            greensSpawnPoint.position,
            greensSpawnPoint.rotation
        );

        // Make sure it is greens (for the sticker and the text on screen)
        Filling filling = newGreens.GetComponentInChildren<Filling>();
        if (filling != null)
        {
            filling.type = FillingType.Greens;
        }
        else
        {
            Debug.LogWarning("Greens Prefab has no Filling script! Add it (Type = Greens).");
        }

        // Tell the greens who made them, so we know when they are taken
        Greens greens = newGreens.GetComponentInChildren<Greens>();
        if (greens == null)
        {
            greens = newGreens.AddComponent<Greens>();
        }
        greens.sourceDispenser = this;

        hasGreens = true;

        Debug.Log("New greens created!");

        return newGreens;
    }

    public void NotifyGreensTaken()
    {
        hasGreens = false;
        Debug.Log("Greens dispenser is ready for more greens!");
    }
}
