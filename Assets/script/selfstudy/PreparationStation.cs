using UnityEngine;

// The preparation table. It can hold several doughs at once.
// You work on the dough you are looking at.
public class PreparationStation : MonoBehaviour
{
    // The middle of the table. Extra dough spots are made around it.
    public Transform doughPoint;

    [Header("Dough spots")]
    // How many doughs fit on the table at once
    public int slotCount = 2;

    // Distance between the doughs (in a row, left to right)
    public float slotSpacing = 0.35f;

    // Optional: place your own empty objects as spots.
    // If empty, the spots are made automatically from Dough Point.
    public Transform[] doughPoints;

    [Header("Times (seconds of holding E)")]
    public float flattenTime = 3f;
    public float closeTime = 3f;

    [Header("Flattening")]
    // Used if no flat prefab is set: X/Z get wider, Y gets thinner
    public Vector3 flatScaleMultiplier = new Vector3(1.6f, 0.25f, 1.6f);
    // Optional: a flat dough model to swap in instead of squashing
    public GameObject flatDoughPrefab;

    // One dough spot on the table
    class Slot
    {
        public Transform point;
        public GameObject dough;
        public Dough data;
        public float workProgress;
        public float lastWorkInputTime;

        public bool IsFree => dough == null;
        public DoughStage Stage => data != null ? data.stage : DoughStage.Raw;
        public bool CanWork => !IsFree &&
            (Stage == DoughStage.Raw || Stage == DoughStage.Filled);
    }

    private Slot[] slots;

    // Where the player is looking on the table (to choose a spot)
    private Vector3 lookPoint;

    // ---------- Setup ----------

    void Awake()
    {
        BuildSlots();
    }

    void BuildSlots()
    {
        if (doughPoints == null || doughPoints.Length == 0)
        {
            Transform center = doughPoint != null ? doughPoint : transform;
            int n = Mathf.Max(1, slotCount);
            doughPoints = new Transform[n];

            for (int i = 0; i < n; i++)
            {
                // Spread the spots in a row: e.g. left and right
                float offset = (i - (n - 1) / 2f) * slotSpacing;

                GameObject p = new GameObject("DoughSpot " + (i + 1));
                p.transform.SetParent(center, false);
                p.transform.position = center.position + center.right * offset;
                p.transform.rotation = center.rotation;
                doughPoints[i] = p.transform;
            }
        }

        slots = new Slot[doughPoints.Length];
        for (int i = 0; i < doughPoints.Length; i++)
        {
            slots[i] = new Slot { point = doughPoints[i] };
        }

        lookPoint = doughPoints[0].position;
    }

    // ---------- Info for the player script ----------

    // The player script tells us where on the table the player is looking
    public void LookAt(Vector3 worldPoint)
    {
        lookPoint = worldPoint;
    }

    public bool HasDough => FindSlot(s => !s.IsFree) != null;
    public bool HasFreeSlot => FindSlot(s => s.IsFree) != null;

    // The dough we would work on by holding E (closest to where you look)
    Slot WorkSlot => FindSlot(s => s.CanWork);

    // Something to do by holding E? (flatten raw dough, or close a filled qutab)
    public bool CanWork => WorkSlot != null;
    public DoughStage Stage => WorkSlot != null ? WorkSlot.Stage : DoughStage.Raw;
    public float WorkTime => Stage == DoughStage.Raw ? flattenTime : closeTime;
    public float WorkPercent => WorkSlot != null ? WorkSlot.workProgress / WorkTime : 0f;
    public string WorkName
    {
        get
        {
            if (Stage == DoughStage.Raw) return "flatten dough";

            // e.g. "close greens qutab"
            string f = WorkSlot != null && WorkSlot.data != null
                ? WorkSlot.data.fillingName : "";
            return string.IsNullOrEmpty(f) ? "close qutab" : "close " + f + " qutab";
        }
    }

    // Is this a dough on the table that has nothing to do right now
    // (e.g. a closed qutab ready to be picked up)?
    public bool IsDoneOnTable(GameObject obj)
    {
        if (slots == null || obj == null) return false;

        foreach (Slot s in slots)
        {
            if (s.dough != null &&
                (obj == s.dough || obj.transform.IsChildOf(s.dough.transform)))
            {
                return !s.CanWork;
            }
        }
        return false;
    }

    // Finds the matching spot closest to where the player is looking
    Slot FindSlot(System.Func<Slot, bool> match)
    {
        if (slots == null) return null;

        Slot best = null;
        float bestDist = float.MaxValue;

        foreach (Slot s in slots)
        {
            if (!match(s)) continue;

            float d = (s.point.position - lookPoint).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = s;
            }
        }

        return best;
    }

    void Update()
    {
        if (slots == null) return;

        // If the player stops holding E (or looks away), reset the progress
        foreach (Slot s in slots)
        {
            if (s.workProgress > 0f && Time.time - s.lastWorkInputTime > 0.1f)
            {
                s.workProgress = 0f;
            }
        }
    }

    // ---------- Placing / removing dough ----------

    // Only dough can be placed here, and there must be a free spot
    public bool CanAccept(GameObject obj)
    {
        return HasFreeSlot && obj.GetComponentInParent<Dough>() != null;
    }

    // Puts the dough on the free spot closest to where the player looks
    public void PlaceDough(GameObject dough)
    {
        if (!CanAccept(dough))
        {
            Debug.Log(dough.name + " can't be placed here.");
            return;
        }

        dough = dough.GetComponentInParent<Dough>().gameObject;
        Slot s = FindSlot(sl => sl.IsFree);

        dough.transform.SetParent(s.point);
        dough.transform.localPosition = Vector3.zero;
        dough.transform.localRotation = Quaternion.identity;

        MakeStatic(dough);

        s.dough = dough;
        s.data = dough.GetComponent<Dough>();
        s.workProgress = 0f;

        Debug.Log("Placed dough on table. Stage: " + s.data.stage);
    }

    // Called when the player picks a dough back up
    public void RemoveDough(GameObject dough)
    {
        if (slots == null) return;

        foreach (Slot s in slots)
        {
            if (s.dough != null &&
                (dough == s.dough || dough.transform.IsChildOf(s.dough.transform)))
            {
                s.dough = null;
                s.data = null;
                s.workProgress = 0f;
            }
        }
    }

    // ---------- Holding E (flatten / close) ----------

    // Called every frame while the player holds E. Returns true when finished.
    public bool AddWorkProgress(float amount)
    {
        Slot s = WorkSlot;
        if (s == null)
        {
            return false;
        }

        s.lastWorkInputTime = Time.time;
        s.workProgress += amount;

        float needed = s.Stage == DoughStage.Raw ? flattenTime : closeTime;

        if (s.workProgress >= needed)
        {
            s.workProgress = 0f;

            if (s.Stage == DoughStage.Raw)
                FlattenDough(s);
            else if (s.Stage == DoughStage.Filled)
                CloseQutab(s);

            return true;
        }

        return false;
    }

    void FlattenDough(Slot s)
    {
        if (flatDoughPrefab != null)
        {
            ReplaceDough(s, flatDoughPrefab, DoughStage.Flat);
        }
        else
        {
            s.dough.transform.localScale = Vector3.Scale(
                s.dough.transform.localScale,
                flatScaleMultiplier
            );
            s.data.stage = DoughStage.Flat;
        }

        Debug.Log("Dough is flat!");
    }

    void CloseQutab(Slot s)
    {
        GameObject closedPrefab = s.data.closedPrefab;

        if (closedPrefab == null)
        {
            Debug.LogWarning("No Closed Qutab Prefab set on the filling!");
            s.data.stage = DoughStage.Closed;
            return;
        }

        ReplaceDough(s, closedPrefab, DoughStage.Closed);
        Debug.Log("Qutab is closed!");
    }

    // ---------- Adding filling ----------

    // The flat dough closest to where you look (filling goes here)
    Slot FlatSlot => FindSlot(s => !s.IsFree && s.Stage == DoughStage.Flat);

    // Can the held object be added as filling right now?
    public bool CanAddFilling(GameObject held)
    {
        return FlatSlot != null &&
               held.GetComponentInParent<Filling>() != null;
    }

    // Returns true if the filling was used
    public bool AddFilling(GameObject held)
    {
        if (!CanAddFilling(held))
        {
            return false;
        }

        Slot s = FlatSlot;
        Filling filling = held.GetComponentInParent<Filling>();

        if (filling.openQutabPrefab == null)
        {
            Debug.LogWarning("No Open Qutab Prefab set on " + filling.name);
            return false;
        }

        GameObject closedPrefab = filling.closedQutabPrefab;

        ReplaceDough(s, filling.openQutabPrefab, DoughStage.Filled);
        s.data.closedPrefab = closedPrefab;
        s.data.fillingName = filling.fillingName;

        // The meat is used up
        Destroy(filling.gameObject);

        Debug.Log("Added " + filling.fillingName + " to the dough!");
        return true;
    }

    // ---------- Helpers ----------

    // Swap a dough for a new model, keeping it on the same spot
    void ReplaceDough(Slot s, GameObject prefab, DoughStage newStage)
    {
        GameObject newDough = Instantiate(prefab);
        newDough.transform.SetParent(s.point, true);
        newDough.transform.localPosition = Vector3.zero;
        newDough.transform.localRotation = Quaternion.identity;

        MakeStatic(newDough);

        Dough d = newDough.GetComponent<Dough>();
        if (d == null)
        {
            d = newDough.AddComponent<Dough>();
        }
        d.stage = newStage;

        // Remember what is inside (e.g. "meat") on the new model
        if (s.data != null)
            d.fillingName = s.data.fillingName;

        Destroy(s.dough);

        s.dough = newDough;
        s.data = d;
    }

    // Stop it from falling and make sure it can be hit by the ray
    void MakeStatic(GameObject obj)
    {
        Rigidbody rb = obj.GetComponentInChildren<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        foreach (Collider c in obj.GetComponentsInChildren<Collider>(true))
        {
            c.enabled = true;
        }
    }

    // Shows the dough spots as cyan circles in the Scene view
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        if (doughPoints != null && doughPoints.Length > 0)
        {
            foreach (Transform p in doughPoints)
                if (p != null) Gizmos.DrawWireSphere(p.position, 0.08f);
        }
        else if (doughPoint != null)
        {
            int n = Mathf.Max(1, slotCount);
            for (int i = 0; i < n; i++)
            {
                float offset = (i - (n - 1) / 2f) * slotSpacing;
                Gizmos.DrawWireSphere(doughPoint.position + doughPoint.right * offset, 0.08f);
            }
        }
    }
}
