using UnityEngine;

// A stack of plates. Every plate you take makes the stack one plate shorter,
// and a new plate comes back every few seconds.
public class PlateDispenser : MonoBehaviour
{
    public GameObject platePrefab;
    public Transform plateSpawnPoint;

    [Header("Stack")]
    // How many plates the stack holds
    public int maxPlates = 5;

    // Where the stack stands. If empty, the Plate Spawn Point is used.
    public Transform stackPoint;

    // Gap between plates in the stack. 0 = calculate from the plate model.
    public float plateSpacing = 0f;

    // Optional: your own plate models for the stack (bottom first).
    // If empty, the stack is built automatically from the Plate Prefab.
    public GameObject[] stackPlates;

    [Header("Refill")]
    // Seconds until one plate comes back (0 = never refill)
    public float refillTime = 8f;

    private int platesLeft;
    private float refillTimer;
    private bool hasPlate = false;

    // ---------- Info for the player script ----------

    public int PlatesLeft => platesLeft;
    public bool CanDispense => !hasPlate && platesLeft > 0;

    public string EmptyText
    {
        get
        {
            if (hasPlate) return "Take the plate first";
            if (refillTime > 0f)
                return "No plates left (next in " +
                       Mathf.CeilToInt(refillTime - refillTimer) + "s)";
            return "No plates left";
        }
    }

    // ---------- Setup ----------

    void Start()
    {
        if (stackPlates == null || stackPlates.Length == 0)
        {
            BuildStack();
        }
        else
        {
            // Your own stack plates are only decoration too
            foreach (GameObject p in stackPlates)
            {
                if (p != null) MakeDecoration(p);
            }

            maxPlates = stackPlates.Length;
        }

        platesLeft = maxPlates;
        UpdateStack();
    }

    // Makes "maxPlates" plate models stacked on top of each other
    void BuildStack()
    {
        if (platePrefab == null) return;

        Transform point = stackPoint != null ? stackPoint : plateSpawnPoint;
        if (point == null) point = transform;

        // Build the copies under a switched-off holder, so their scripts never start
        GameObject holder = new GameObject("PlateStack");
        holder.SetActive(false);
        holder.transform.SetParent(transform, true);
        holder.transform.position = point.position;
        holder.transform.rotation = point.rotation;

        stackPlates = new GameObject[maxPlates];
        float spacing = plateSpacing;

        for (int i = 0; i < maxPlates; i++)
        {
            GameObject copy = Instantiate(platePrefab, holder.transform);
            copy.name = "StackPlate " + (i + 1);

            MakeDecoration(copy);

            copy.transform.localPosition = Vector3.zero;
            copy.transform.localRotation = Quaternion.identity;

            // Work out the gap from the plate's height (only once)
            if (spacing <= 0f)
            {
                spacing = MeasureHeight(copy) * 0.6f;
                if (spacing <= 0f) spacing = 0.03f;
            }

            // Put the bottom-middle of the plate MODEL on the stack spot
            // (the model inside your plate prefab is shifted from its pivot)
            Vector3 target = point.position + point.up * spacing * i;
            Vector3 modelBottom = GetModelBottom(copy);
            copy.transform.position += target - modelBottom;

            stackPlates[i] = copy;
        }

        holder.SetActive(true);
    }

    // The stack plates are only for looks: no scripts, no physics
    void MakeDecoration(GameObject copy)
    {
        foreach (Plate p in copy.GetComponentsInChildren<Plate>(true))
            DestroyImmediate(p);

        foreach (Rigidbody rb in copy.GetComponentsInChildren<Rigidbody>(true))
            DestroyImmediate(rb);

        // Colliders stay, so looking at the stack still finds this dispenser
    }

    // World position of the bottom-middle of the plate's 3D model
    Vector3 GetModelBottom(GameObject obj)
    {
        MeshFilter mf = obj.GetComponentInChildren<MeshFilter>(true);
        if (mf == null || mf.sharedMesh == null) return obj.transform.position;

        Bounds mb = mf.sharedMesh.bounds;
        Vector3 bottomLocal = mb.center - new Vector3(0f, mb.extents.y, 0f);
        return mf.transform.TransformPoint(bottomLocal);
    }

    float MeasureHeight(GameObject obj)
    {
        Renderer[] rs = obj.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) return 0f;

        // Holder is switched off, so bounds can't be read yet:
        // use the mesh size and the object's scale instead
        float h = 0f;
        foreach (MeshFilter mf in obj.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh == null) continue;
            Vector3 size = Vector3.Scale(mf.sharedMesh.bounds.size, mf.transform.lossyScale);
            h = Mathf.Max(h, Mathf.Abs(size.y));
        }
        return h;
    }

    // ---------- Refill ----------

    void Update()
    {
        if (refillTime <= 0f || platesLeft >= maxPlates)
        {
            refillTimer = 0f;
            return;
        }

        refillTimer += Time.deltaTime;

        if (refillTimer >= refillTime)
        {
            refillTimer = 0f;
            platesLeft++;
            UpdateStack();
            Debug.Log("A clean plate is back! Plates: " + platesLeft);
        }
    }

    // Show only as many stack plates as we have left
    void UpdateStack()
    {
        if (stackPlates == null) return;

        for (int i = 0; i < stackPlates.Length; i++)
        {
            if (stackPlates[i] != null)
                stackPlates[i].SetActive(i < platesLeft);
        }
    }

    // ---------- Taking plates ----------

    public GameObject Dispense()
    {
        if (!CanDispense)
        {
            Debug.Log(EmptyText);
            return null;
        }

        if (platePrefab == null || plateSpawnPoint == null)
        {
            Debug.LogError("Assign the Plate Prefab and Spawn Point!");
            return null;
        }

        GameObject newPlate = Instantiate(
            platePrefab,
            plateSpawnPoint.position,
            plateSpawnPoint.rotation
        );

        // Make sure the plate works even if the prefab is just a 3D model
        Plate plate = newPlate.GetComponentInChildren<Plate>();
        if (plate == null)
        {
            plate = newPlate.AddComponent<Plate>();
        }

        // A Rigidbody lets the plate be carried, dropped and fall on tables
        if (newPlate.GetComponent<Rigidbody>() == null)
        {
            newPlate.AddComponent<Rigidbody>();
        }

        plate.sourceDispenser = this;
        hasPlate = true;

        // Top plate of the stack disappears
        platesLeft--;
        UpdateStack();

        Debug.Log("Took a plate! Plates left: " + platesLeft);

        return newPlate;
    }

    public void NotifyPlateTaken()
    {
        hasPlate = false;
    }
}
