using UnityEngine;

public class MeatDispenser : MonoBehaviour
{
    public GameObject meatPrefab;
    public Transform meatSpawnPoint;

    private bool hasMeat = false;

    public bool CanDispense => !hasMeat;

    public GameObject Dispense()
    {
        if (hasMeat)
        {
            Debug.Log("The meat dispenser is empty!");
            return null;
        }

        if (meatPrefab == null || meatSpawnPoint == null)
        {
            Debug.LogError("Assign the Meat Prefab and Spawn Point!");
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

        Debug.Log("New meat created!");

        return newMeat;
    }

    public void NotifyMeatTaken()
    {
        hasMeat = false;
        Debug.Log("Meat dispenser is ready for more meat!");
    }
}