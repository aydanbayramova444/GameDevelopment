using UnityEngine;

// Gives out a filling (meat, greens, ...). Works for any filling:
// just set the prefab and the name shown to the player.
public class MeatDispenser : MonoBehaviour
{
    public GameObject meatPrefab;
    public Transform meatSpawnPoint;

    // Shown to the player: "Press E to get <itemName>"
    public string itemName = "meat";

    private bool hasMeat = false;

    public bool CanDispense => !hasMeat;

    public GameObject Dispense()
    {
        if (hasMeat)
        {
            Debug.Log("The " + itemName + " dispenser is empty!");
            return null;
        }

        if (meatPrefab == null || meatSpawnPoint == null)
        {
            Debug.LogError("Assign the Prefab and Spawn Point on " + name + "!");
            return null;
        }

        GameObject newMeat = Instantiate(
            meatPrefab,
            meatSpawnPoint.position,
            meatSpawnPoint.rotation
        );

        Filling filling = newMeat.GetComponentInChildren<Filling>();

        if (filling != null)
        {
            filling.sourceDispenser = this;
        }

        hasMeat = true;

        Debug.Log("New " + itemName + " created!");

        return newMeat;
    }

    public void NotifyMeatTaken()
    {
        hasMeat = false;
        Debug.Log(itemName + " dispenser is ready again!");
    }
}
