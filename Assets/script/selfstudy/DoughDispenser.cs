
using UnityEngine;

public class DoughDispenser : MonoBehaviour
{
    public GameObject doughPrefab;
    public Transform doughSpawnPoint;

    private bool hasDough = false;

    public bool CanDispense => !hasDough;

    public GameObject Dispense()
    {
        if (hasDough)
        {
            Debug.Log("The dispenser is empty!");
            return null;
        }

        if (doughPrefab == null || doughSpawnPoint == null)
        {
            Debug.LogError("Assign the Dough Prefab and Spawn Point!");
            return null;
        }

        GameObject newDough = Instantiate(
            doughPrefab,
            doughSpawnPoint.position,
            doughSpawnPoint.rotation
        );

        Dough dough = newDough.GetComponentInChildren<Dough>();

        if (dough != null)
        {
            dough.sourceDispenser = this;
        }

        hasDough = true;

        Debug.Log("New dough created!");

        return newDough;
    }

    public void NotifyDoughTaken()
    {
        hasDough = false;
        Debug.Log("Dispenser is ready for more dough!");
    }
}
