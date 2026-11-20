using UnityEngine;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    public Camera playerCamera;
    public Transform holdPoint;
    public TMP_Text interactionText;

    public float interactionDistance = 4f;
    public LayerMask interactableLayer;

    private GameObject heldObject;
    private Rigidbody heldRigidbody;
    private Collider[] heldColliders;

    private GameObject objectInSight;

    private PreparationStation stationInSight;
    private CookingStation cookingInSight;
    private DoughDispenser dispenserInSight;
    private MeatDispenser meatDispenserInSight;
    private GreensDispenser greensDispenserInSight;
    private Plate plateInSight;
    private PlateDispenser plateDispenserInSight;

    // The plate in our hands (null if we aren't holding a plate)
    private Plate HeldPlate =>
        heldObject != null ? heldObject.GetComponent<Plate>() : null;

    // Holding an empty plate, looking at the saj with a cooked (or burnt) qutab
    private bool CanServeFromSaj =>
        HeldPlate != null &&
        !HeldPlate.HasItem &&
        cookingInSight != null &&
        cookingInSight.ReadyItem != null;

    // Wait until E is released after an action
    private bool waitForKeyRelease;

    void Update()
    {
        CheckForObject();
        ShowKindInfo();

        if (waitForKeyRelease)
        {
            if (!Input.GetKey(KeyCode.E))
            {
                waitForKeyRelease = false;
            }

            return;
        }

        // Holding E to flatten dough / close qutab
        if (CanWorkOnStation())
        {
            if (Input.GetKey(KeyCode.E))
            {
                bool finished =
                    stationInSight.AddWorkProgress(Time.deltaTime);

                if (finished)
                {
                    waitForKeyRelease = true;
                }
            }

            return;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            Interact();
        }
    }

    // Shows what kind of qutab you hold or look at, e.g. "In hand: Closed greens qutab (raw)"
    void ShowKindInfo()
    {
        if (interactionText == null) return;

        const string tag = "In hand: ";
        if (interactionText.text.Contains(tag)) return;

        // Holding a qutab (plates already show their dish name)
        Dough held = heldObject != null && HeldPlate == null
            ? heldObject.GetComponentInParent<Dough>()
            : null;

        if (held != null && held.stage != DoughStage.Raw)
        {
            interactionText.text += "\n<size=75%>" + tag + held.KindName + "</size>";
            return;
        }

        // Looking at a loose qutab (on the table or the floor) with empty hands
        if (heldObject == null && objectInSight != null &&
            interactionText.text == "Press E to pick up")
        {
            Dough seen = objectInSight.GetComponentInParent<Dough>();
            if (seen != null)
                interactionText.text = seen.KindName + " - Press E to pick up";
        }
    }

    bool CanWorkOnStation()
    {
        // Looking straight at a finished dough on the table? Then pick it up instead
        if (stationInSight != null && stationInSight.IsDoneOnTable(objectInSight))
            return false;

        return heldObject == null &&
               stationInSight != null &&
               stationInSight.CanWork;
    }

    void CheckForObject()
    {
        objectInSight = null;
        stationInSight = null;
        cookingInSight = null;
        dispenserInSight = null;
        meatDispenserInSight = null;
        greensDispenserInSight = null;
        plateInSight = null;
        plateDispenserInSight = null;

        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        Debug.DrawRay(
            ray.origin,
            ray.direction * interactionDistance,
            Color.red
        );

        // Find all objects in front of the player
        RaycastHit[] hits =
            Physics.RaycastAll(ray, interactionDistance);

        System.Array.Sort(
            hits,
            (a, b) => a.distance.CompareTo(b.distance)
        );

        foreach (RaycastHit hit in hits)
        {
            // Skip the held object and the player
            if (heldObject != null &&
                hit.collider.transform.IsChildOf(heldObject.transform))
                continue;

            if (hit.collider.transform.IsChildOf(transform))
                continue;

            // Detect preparation station
            if (stationInSight == null)
            {
                stationInSight =
                    hit.collider.GetComponentInParent<PreparationStation>();

                // Tell the table where we look, so it picks the right dough
                if (stationInSight != null)
                    stationInSight.LookAt(hit.point);
            }

            // Detect cooking station (saj)
            if (cookingInSight == null)
            {
                cookingInSight =
                    hit.collider.GetComponentInParent<CookingStation>();

                // Tell the saj where we look, so it picks the right spot
                if (cookingInSight != null)
                    cookingInSight.LookAt(hit.point);
            }

            // Detect dough dispenser
            if (dispenserInSight == null)
            {
                dispenserInSight =
                    hit.collider.GetComponentInParent<DoughDispenser>();
            }

            // Detect meat dispenser
            if (meatDispenserInSight == null)
            {
                meatDispenserInSight =
                    hit.collider.GetComponentInParent<MeatDispenser>();
            }

            // Detect greens dispenser
            if (greensDispenserInSight == null)
            {
                greensDispenserInSight =
                    hit.collider.GetComponentInParent<GreensDispenser>();
            }

            // Detect plate
            if (plateInSight == null)
            {
                plateInSight =
                    hit.collider.GetComponentInParent<Plate>();
            }

            // Detect plate dispenser
            if (plateDispenserInSight == null)
            {
                plateDispenserInSight =
                    hit.collider.GetComponentInParent<PlateDispenser>();
            }

            // Detect objects we can pick up
            if (objectInSight == null && heldObject == null)
            {
                bool isInteractable =
                    (interactableLayer.value &
                    (1 << hit.collider.gameObject.layer)) != 0;

                if (isInteractable)
                {
                    // Always pick up the WHOLE object, not just the child we hit
                    Dough dough =
                        hit.collider.GetComponentInParent<Dough>();

                    if (dough != null)
                        objectInSight = dough.gameObject;
                    else if (hit.rigidbody != null)
                        objectInSight = hit.rigidbody.gameObject;
                    else
                        objectInSight = hit.collider.gameObject;
                }
            }
        }

        // ----- Update interaction text -----

        // Hold E actions (flatten / close)
        if (CanWorkOnStation())
        {
            objectInSight = null;

            int percent = Mathf.RoundToInt(
                stationInSight.WorkPercent * 100f
            );

            if (percent > 0)
                interactionText.text = "Working... " + percent + "%";
            else
                interactionText.text = "Hold E to " + stationInSight.WorkName;

            return;
        }

        // Holding an empty plate, looking at the saj with cooked qutab
        if (CanServeFromSaj)
        {
            interactionText.text = "Press E to put qutab on plate";
            return;
        }

        // Holding something (not a plate) while looking at the table
        if (heldObject != null && HeldPlate == null && stationInSight != null)
        {
            if (stationInSight.CanAddFilling(heldObject))
            {
                Filling filling =
                    heldObject.GetComponentInParent<Filling>();
                interactionText.text =
                    "Press E to add " + filling.DisplayName;
                return;
            }

            if (stationInSight.CanAccept(heldObject))
            {
                interactionText.text = "Press E to place dough";
                return;
            }

            if (heldObject.GetComponentInParent<Dough>() != null &&
                !stationInSight.HasFreeSlot)
            {
                interactionText.text = "Table is full!";
                return;
            }

            if (!stationInSight.HasDough)
            {
                interactionText.text = "Only dough can be placed here";
                return;
            }
        }

        // Holding something (not a plate) while looking at the saj
        if (heldObject != null && HeldPlate == null && cookingInSight != null)
        {
            if (cookingInSight.CanAccept(heldObject))
            {
                interactionText.text = "Press E to cook qutab";
                return;
            }

            Dough heldDough = heldObject.GetComponentInParent<Dough>();
            if (heldDough != null &&
                heldDough.stage == DoughStage.Closed &&
                !cookingInSight.HasFreeSlot)
            {
                interactionText.text = "Saj is full!";
                return;
            }

            if (!cookingInSight.HasItem)
            {
                interactionText.text = "Only closed qutab can be cooked";
                return;
            }
        }

        // Holding something while looking at the plate
        if (heldObject != null && plateInSight != null)
        {
            if (plateInSight.CanAccept(heldObject))
            {
                interactionText.text = "Press E to put on plate";
                return;
            }

            if (!plateInSight.HasItem)
            {
                interactionText.text = "Only cooked qutab can go on the plate";
                return;
            }
        }

        // Empty hands, looking at a plate we can carry
        if (heldObject == null &&
            plateInSight != null &&
            plateInSight.CanBePickedUp)
        {
            if (plateInSight.HasItem)
                interactionText.text =
                    plateInSight.DishName + " - Press E to pick up plate";
            else
                interactionText.text = "Press E to pick up plate";
            return;
        }

        // Empty hands, looking at a fixed plate with food on it
        if (heldObject == null &&
            plateInSight != null &&
            plateInSight.HasItem)
        {
            interactionText.text =
                plateInSight.DishName + " - Press E to take";
            return;
        }

        // Empty hands, looking at the saj with a qutab on it
        if (heldObject == null &&
            cookingInSight != null &&
            cookingInSight.HasItem)
        {
            interactionText.text = cookingInSight.StatusText;
            return;
        }

        if (heldObject == null && dispenserInSight != null)
        {
            if (dispenserInSight.CanDispense)
                interactionText.text = "Press E to get dough";
            else
                interactionText.text = "Dough dispenser is empty";
        }
        else if (heldObject == null && meatDispenserInSight != null)
        {
            if (meatDispenserInSight.CanDispense)
                interactionText.text = "Press E to get " + meatDispenserInSight.itemName;
            else
                interactionText.text = "Wait, " + meatDispenserInSight.itemName + " is already out";
        }
        else if (heldObject == null && greensDispenserInSight != null)
        {
            if (greensDispenserInSight.CanDispense)
                interactionText.text = "Press E to get greens (goyerti)";
            else
                interactionText.text = "Greens dispenser is empty";
        }
        else if (heldObject == null && plateDispenserInSight != null)
        {
            if (plateDispenserInSight.CanDispense)
                interactionText.text = "Press E to get plate (" +
                    plateDispenserInSight.PlatesLeft + " left)";
            else
                interactionText.text = plateDispenserInSight.EmptyText;
        }
        else if (HeldPlate != null && HeldPlate.HasItem)
        {
            interactionText.text = HeldPlate.DishName + " - Press E to drop";
        }
        else if (heldObject == null && objectInSight != null)
        {
            interactionText.text = "Press E to pick up";
        }
        else if (heldObject != null)
        {
            interactionText.text = "Press E to drop";
        }
        else
        {
            interactionText.text = "";
        }
    }

    void Interact()
    {
        // If holding an object
        if (heldObject != null)
        {
            // Holding an empty plate: take the cooked qutab off the saj onto it
            if (CanServeFromSaj)
            {
                GameObject qutab = cookingInSight.ReadyItem;
                cookingInSight.RemoveItem(qutab);
                HeldPlate.PlaceItem(qutab);

                // The plate is in our hands, so turn off all its colliders
                // (including the new qutab) until we drop it
                heldColliders = heldObject.GetComponentsInChildren<Collider>();
                foreach (Collider c in heldColliders)
                {
                    c.enabled = false;
                }

                waitForKeyRelease = true;
                return;
            }

            if (HeldPlate == null && stationInSight != null)
            {
                // Place dough on empty table
                if (stationInSight.CanAccept(heldObject))
                {
                    stationInSight.PlaceDough(heldObject);
                    ClearHeld();
                    waitForKeyRelease = true;
                    return;
                }

                // Add filling to flat dough
                if (stationInSight.CanAddFilling(heldObject))
                {
                    if (stationInSight.AddFilling(heldObject))
                    {
                        ClearHeld();
                        waitForKeyRelease = true;
                        return;
                    }
                }
            }

            // Put closed qutab on the saj
            if (cookingInSight != null &&
                cookingInSight.CanAccept(heldObject))
            {
                cookingInSight.PlaceItem(heldObject);
                ClearHeld();
                waitForKeyRelease = true;
                return;
            }

            // Put cooked qutab on the plate
            if (plateInSight != null &&
                plateInSight.CanAccept(heldObject))
            {
                plateInSight.PlaceItem(heldObject);
                ClearHeld();
                waitForKeyRelease = true;
                return;
            }

            DropObject();
            return;
        }

        // Pick up a carryable plate (with its food)
        if (plateInSight != null && plateInSight.CanBePickedUp)
        {
            PickUpObject(plateInSight.gameObject);
            return;
        }

        // Take the qutab off a fixed plate
        if (plateInSight != null && plateInSight.HasItem)
        {
            PickUpObject(plateInSight.CurrentItem);
            return;
        }

        // Take the qutab off the saj
        if (cookingInSight != null && cookingInSight.HasItem)
        {
            PickUpObject(cookingInSight.CurrentItem);
            return;
        }

        // Get dough from dispenser
        if (dispenserInSight != null &&
            dispenserInSight.CanDispense)
        {
            GameObject newDough =
                dispenserInSight.Dispense();

            if (newDough != null)
            {
                PickUpObject(newDough);
            }

            return;
        }

        // Get meat from meat dispenser
        if (meatDispenserInSight != null &&
            meatDispenserInSight.CanDispense)
        {
            GameObject newMeat =
                meatDispenserInSight.Dispense();

            if (newMeat != null)
            {
                PickUpObject(newMeat);
            }

            return;
        }

        // Get greens from greens dispenser
        if (greensDispenserInSight != null &&
            greensDispenserInSight.CanDispense)
        {
            GameObject newGreens =
                greensDispenserInSight.Dispense();

            if (newGreens != null)
            {
                PickUpObject(newGreens);
            }

            return;
        }

        // Get plate from plate dispenser
        if (plateDispenserInSight != null &&
            plateDispenserInSight.CanDispense)
        {
            GameObject newPlate =
                plateDispenserInSight.Dispense();

            if (newPlate != null)
            {
                PickUpObject(newPlate);
            }

            return;
        }

        // Pick up an existing object
        if (objectInSight != null)
        {
            PickUpObject(objectInSight);
        }
    }

    void PickUpObject(GameObject obj)
    {
        // Remove from preparation station if necessary
        PreparationStation station =
            obj.GetComponentInParent<PreparationStation>();

        if (station != null)
        {
            station.RemoveDough(obj);
        }

        // Remove from saj if necessary
        CookingStation cooking =
            obj.GetComponentInParent<CookingStation>();

        if (cooking != null)
        {
            cooking.RemoveItem(obj);
        }

        // Remove from plate if necessary
        Plate plate =
            obj.GetComponentInParent<Plate>();

        if (plate != null)
        {
            plate.RemoveItem(obj);
        }

        // Notify plate dispenser that its plate has been collected
        Plate pickedPlate = obj.GetComponent<Plate>();

        if (pickedPlate != null &&
            pickedPlate.sourceDispenser != null)
        {
            pickedPlate.sourceDispenser.NotifyPlateTaken();
            pickedPlate.sourceDispenser = null;
        }

        // Notify dough dispenser that its dough has been collected
        Dough dough =
            obj.GetComponentInChildren<Dough>(true);

        if (dough != null &&
            dough.sourceDispenser != null)
        {
            dough.sourceDispenser.NotifyDoughTaken();
            dough.sourceDispenser = null;
        }

        // Notify meat dispenser that its meat has been collected
        Filling filling =
            obj.GetComponentInChildren<Filling>(true);

        if (filling != null &&
            filling.sourceDispenser != null)
        {
            filling.sourceDispenser.NotifyMeatTaken();
            filling.sourceDispenser = null;
        }

        // Notify greens dispenser that its greens have been collected
        Greens greens =
            obj.GetComponentInChildren<Greens>(true);

        if (greens != null)
        {
            greens.OnPickedUp();
        }

        heldObject = obj;

        // Find Rigidbody and all Colliders
        heldRigidbody =
            heldObject.GetComponentInChildren<Rigidbody>();

        heldColliders =
            heldObject.GetComponentsInChildren<Collider>();

        if (heldRigidbody != null)
        {
            heldRigidbody.isKinematic = true;
        }

        // Disable all colliders while holding
        foreach (Collider c in heldColliders)
        {
            c.enabled = false;
        }

        // Center the visible model in the hand (fixes models with shifted pivots)
        PlacementHelper.SnapTo(heldObject, holdPoint, false);

        Debug.Log("Picked up: " + heldObject.name);
    }

    void DropObject()
    {
        heldObject.transform.SetParent(null);

        // Enable all colliders again
        if (heldColliders != null)
        {
            foreach (Collider c in heldColliders)
            {
                c.enabled = true;
            }
        }

        if (heldRigidbody != null)
        {
            heldRigidbody.isKinematic = false;
        }

        ClearHeld();
        Debug.Log("Object dropped!");
    }

    void ClearHeld()
    {
        heldObject = null;
        heldRigidbody = null;
        heldColliders = null;
    }
}