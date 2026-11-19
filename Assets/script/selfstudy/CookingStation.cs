using UnityEngine;

// The saj. It can cook several qutabs at once, each with its own timer.
public class CookingStation : MonoBehaviour
{
    // The middle of the saj. Extra cooking spots are made around it.
    public Transform cookPoint;

    [Header("Cooking spots")]
    // How many qutabs fit on the saj at once
    public int slotCount = 3;

    // Distance between the qutabs (in a row, left to right)
    public float slotSpacing = 0.25f;

    // Put the qutabs ON the saj's curved surface and tilt them with it.
    // (Needs a Mesh Collider on the saj for the exact dome shape.)
    public bool followSurface = true;

    // Most a qutab may tilt, in degrees
    [Range(0f, 60f)]
    public float maxTilt = 35f;

    // Optional: place your own empty objects as spots.
    // If empty, the spots are made automatically from Cook Point.
    public Transform[] cookPoints;

    [Header("Cooking")]
    public float cookTime = 5f;
    public GameObject cookedQutabPrefab;

    [Header("Burning (optional)")]
    public bool canBurn = true;
    public float burnTime = 8f;
    // Optional: a burnt model. If empty, the cooked qutab just turns dark.
    public GameObject burntQutabPrefab;

    // One cooking spot on the saj
    class Slot
    {
        public Transform point;
        public GameObject item;
        public Dough data;
        public float timer;

        public bool IsFree => item == null;
        public DoughStage Stage => data != null ? data.stage : DoughStage.Raw;
        public bool IsReady => !IsFree &&
            (Stage == DoughStage.Cooked || Stage == DoughStage.Burnt);
    }

    private Slot[] slots;

    // Where the player is looking on the saj (to choose a spot)
    private Vector3 lookPoint;

    // ---------- Setup ----------

    void Awake()
    {
        BuildSlots();
    }

    void BuildSlots()
    {
        if (cookPoints == null || cookPoints.Length == 0)
        {
            Transform center = cookPoint != null ? cookPoint : transform;
            int n = Mathf.Max(1, slotCount);
            cookPoints = new Transform[n];

            for (int i = 0; i < n; i++)
            {
                GetSpotPose(center, i, n, out Vector3 pos, out Quaternion rot);

                GameObject p = new GameObject("CookSpot " + (i + 1));
                p.transform.SetParent(center, false);
                p.transform.position = pos;
                p.transform.rotation = rot;
                cookPoints[i] = p.transform;
            }
        }

        slots = new Slot[cookPoints.Length];
        for (int i = 0; i < cookPoints.Length; i++)
        {
            slots[i] = new Slot { point = cookPoints[i] };
        }

        lookPoint = cookPoints[0].position;
    }

    // Where spot number i (of n) goes: in a row, dropped onto the saj's surface
    void GetSpotPose(Transform center, int i, int n, out Vector3 pos, out Quaternion rot)
    {
        // Spread the spots in a row: e.g. left, middle, right
        float offset = (i - (n - 1) / 2f) * slotSpacing;
        pos = center.position + center.right * offset;
        rot = center.rotation;

        if (!followSurface) return;

        // Shoot a ray straight down onto the saj to find its surface here
        Vector3 up = center.up;
        RaycastHit[] hits = Physics.RaycastAll(pos + up * 2f, -up, 4f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        float best = float.MaxValue;
        bool found = false;
        RaycastHit surface = default;

        foreach (RaycastHit h in hits)
        {
            // Only the saj itself (not qutabs or other things)
            if (!h.collider.transform.IsChildOf(transform)) continue;
            if (h.collider.GetComponentInParent<Dough>() != null) continue;

            if (h.distance < best)
            {
                best = h.distance;
                surface = h;
                found = true;
            }
        }

        if (!found) return;

        pos = surface.point;

        // Tilt the spot like the surface, but not more than maxTilt
        Vector3 normal = Vector3.RotateTowards(up, surface.normal,
            maxTilt * Mathf.Deg2Rad, 0f);
        rot = Quaternion.FromToRotation(up, normal) * center.rotation;
    }

    // ---------- Info for the player script ----------

    // The player script tells us where on the saj the player is looking
    public void LookAt(Vector3 worldPoint)
    {
        lookPoint = worldPoint;
    }

    public bool HasItem => FindSlot(s => !s.IsFree) != null;
    public bool HasFreeSlot => FindSlot(s => s.IsFree) != null;

    // The qutab you are looking at (closest to where you look)
    public GameObject CurrentItem => Focused?.item;
    public DoughStage Stage => Focused != null ? Focused.Stage : DoughStage.Raw;

    // The closest qutab that is cooked or burnt (for putting on a plate)
    public GameObject ReadyItem => FindSlot(s => s.IsReady)?.item;

    Slot Focused => FindSlot(s => !s.IsFree);

    public string StatusText
    {
        get
        {
            Slot s = Focused;
            if (s == null) return "";

            string text;
            switch (s.Stage)
            {
                case DoughStage.Closed:
                    text = "Cooking... " + Mathf.RoundToInt(s.timer / cookTime * 100f) + "%";
                    break;

                case DoughStage.Cooked:
                    text = canBurn
                        ? "Cooked! Press E to take (burns in " +
                          Mathf.CeilToInt(burnTime - s.timer) + "s)"
                        : "Cooked! Press E to take";
                    break;

                case DoughStage.Burnt:
                    text = "Burnt! Press E to take";
                    break;

                default:
                    text = "Press E to take";
                    break;
            }

            // Show how full the saj is, e.g. "[2/3]"
            int used = 0;
            foreach (Slot sl in slots) if (!sl.IsFree) used++;
            return "[" + used + "/" + slots.Length + "] " + text;
        }
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

    // ---------- Cooking timers ----------

    void Update()
    {
        if (slots == null) return;

        foreach (Slot s in slots)
        {
            if (s.IsFree) continue;

            if (s.Stage == DoughStage.Closed)
            {
                s.timer += Time.deltaTime;

                if (s.timer >= cookTime)
                {
                    s.timer = 0f;
                    Cook(s);
                }
            }
            else if (s.Stage == DoughStage.Cooked && canBurn)
            {
                s.timer += Time.deltaTime;

                if (s.timer >= burnTime)
                {
                    s.timer = 0f;
                    Burn(s);
                }
            }
        }
    }

    // ---------- Placing / removing ----------

    // Only a closed raw qutab can go on the saj, and there must be a free spot
    public bool CanAccept(GameObject obj)
    {
        if (!HasFreeSlot) return false;

        Dough d = obj.GetComponentInParent<Dough>();
        return d != null && d.stage == DoughStage.Closed;
    }

    // Puts the qutab on the free spot closest to where the player looks
    public void PlaceItem(GameObject obj)
    {
        if (!CanAccept(obj)) return;

        obj = obj.GetComponentInParent<Dough>().gameObject;
        Slot s = FindSlot(sl => sl.IsFree);

        MakeStatic(obj);
        PlacementHelper.SnapTo(obj, s.point, true);

        s.item = obj;
        s.data = obj.GetComponent<Dough>();
        s.timer = 0f;

        Debug.Log("Qutab is cooking...");
    }

    // Called when the player takes a qutab off the saj
    public void RemoveItem(GameObject obj)
    {
        if (slots == null) return;

        foreach (Slot s in slots)
        {
            if (s.item != null &&
                (obj == s.item || obj.transform.IsChildOf(s.item.transform)))
            {
                s.item = null;
                s.data = null;
                s.timer = 0f;
            }
        }
    }

    // ---------- Cook / burn ----------

    void Cook(Slot s)
    {
        if (cookedQutabPrefab != null)
        {
            ReplaceItem(s, cookedQutabPrefab, DoughStage.Cooked);
        }
        else
        {
            Debug.LogWarning("No Cooked Qutab Prefab set on the saj!");
            Darken(s.item, 0.75f);
            s.data.stage = DoughStage.Cooked;
        }

        Debug.Log("Qutab is cooked!");
    }

    void Burn(Slot s)
    {
        if (burntQutabPrefab != null)
        {
            ReplaceItem(s, burntQutabPrefab, DoughStage.Burnt);
        }
        else
        {
            Darken(s.item, 0.3f);
            s.data.stage = DoughStage.Burnt;
        }

        Debug.Log("Qutab is burnt!");
    }

    // ---------- Helpers ----------

    // Swap a qutab for a new model, keeping it on the same spot
    void ReplaceItem(Slot s, GameObject prefab, DoughStage newStage)
    {
        GameObject newItem = Instantiate(prefab);

        MakeStatic(newItem);
        PlacementHelper.SnapTo(newItem, s.point, true);

        Dough d = newItem.GetComponent<Dough>();
        if (d == null)
        {
            d = newItem.AddComponent<Dough>();
        }
        d.stage = newStage;

        // Remember what is inside (e.g. "meat") on the new model
        if (s.data != null)
            d.fillingName = s.data.fillingName;

        Destroy(s.item);

        s.item = newItem;
        s.data = d;
    }

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

    // Makes all colors of the object darker (1 = same, 0 = black)
    void Darken(GameObject obj, float amount)
    {
        foreach (Renderer r in obj.GetComponentsInChildren<Renderer>())
        {
            foreach (Material m in r.materials)
            {
                Color c = m.color * amount;
                c.a = 1f;
                m.color = c;
            }
        }
    }

    // Shows the cooking spots as yellow circles in the Scene view
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        if (cookPoints != null && cookPoints.Length > 0)
        {
            foreach (Transform p in cookPoints)
                if (p != null) Gizmos.DrawWireSphere(p.position, 0.06f);
        }
        else if (cookPoint != null)
        {
            int n = Mathf.Max(1, slotCount);
            for (int i = 0; i < n; i++)
            {
                GetSpotPose(cookPoint, i, n, out Vector3 pos, out Quaternion rot);
                Gizmos.DrawWireSphere(pos, 0.06f);

                // Line shows which way the qutab will tilt
                Gizmos.DrawLine(pos, pos + rot * Vector3.up * 0.2f);
            }
        }
    }
}
