using UnityEngine;
using System.Collections.Generic;

// A plate you can put a cooked qutab on.
// Shows a sticker above it with what is inside (e.g. a steak picture for meat).
// Sticker pictures live in Assets/Resources/Stickers/<filling name>.png
// (meat.png for "meat", cheese.png for "cheese", ...)
public class Plate : MonoBehaviour
{
    // Where the qutab sits on the plate (an empty child object)
    public Transform platePoint;

    // The sticker above the plate (created automatically)
    public SpriteRenderer sticker;

    // How big the sticker is in the world
    public float stickerSize = 0.25f;

    // How high above the plate the sticker floats
    public float stickerHeight = 0.3f;

    // Can a burnt qutab be put on the plate too?
    public bool acceptBurnt = true;

    [HideInInspector]
    public PlateDispenser sourceDispenser;

    private GameObject currentItem;
    private Dough currentData;
    private Camera cam;

    // ---------- Info for the player script ----------

    public bool HasItem => currentItem != null;
    public GameObject CurrentItem => currentItem;
    public string DishName => currentData != null ? currentData.DisplayName : "";

    // Plates with a Rigidbody (like ones from the plate dispenser) can be carried
    public bool CanBePickedUp => GetComponent<Rigidbody>() != null;

    // When the game starts, any object named "Plate" (or "Plate (1)", "Plate (2)"...)
    // automatically becomes a working plate, even without adding this script by hand.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void MakeAllPlatesWork()
    {
        var nameRule = new System.Text.RegularExpressions.Regex(@"^Plate( \(\d+\))?$");

        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            // Skip plates that are part of a plate dispenser's stack (they are only decoration)
            if (nameRule.IsMatch(t.name) &&
                t.GetComponent<Plate>() == null &&
                t.GetComponentInParent<PlateDispenser>() == null)
            {
                t.gameObject.AddComponent<Plate>();
            }
        }
    }

    void Awake()
    {
        AutoSetup();
    }

    void Start()
    {
        cam = Camera.main;
        UpdateSticker();
    }

    // Creates anything that is missing: collider, PlatePoint and Sticker.
    // Safe to call many times.
    public void AutoSetup()
    {
        // Collider so the player's ray can hit the plate
        Bounds b = GetModelBounds();

        if (GetComponentInChildren<Collider>() == null)
        {
            // Make the box exactly as big as the plate model
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.center = transform.InverseTransformPoint(b.center);
            Vector3 s = transform.lossyScale;
            box.size = new Vector3(
                b.size.x / Mathf.Abs(s.x),
                Mathf.Max(b.size.y, 0.05f) / Mathf.Abs(s.y),
                b.size.z / Mathf.Abs(s.z));
        }

        // Point on top of the plate where the qutab will sit
        if (platePoint == null)
        {
            Transform existing = transform.Find("PlatePoint");
            if (existing != null)
            {
                platePoint = existing;
            }
            else
            {
                GameObject p = new GameObject("PlatePoint");
                p.transform.SetParent(transform, false);
                p.transform.position = new Vector3(b.center.x, b.max.y, b.center.z);
                platePoint = p.transform;
            }
        }

        // Sticker above the plate
        if (sticker == null)
        {
            Transform existing = transform.Find("Sticker");
            if (existing != null)
            {
                sticker = existing.GetComponent<SpriteRenderer>();
            }

            if (sticker == null)
            {
                GameObject t = new GameObject("Sticker");
                t.transform.SetParent(transform, true);
                t.transform.position = new Vector3(b.center.x, b.max.y + stickerHeight, b.center.z);
                t.transform.rotation = Quaternion.identity;
                // Keep the sticker the same size even if the plate is scaled
                t.transform.localScale = new Vector3(
                    1f / transform.lossyScale.x,
                    1f / transform.lossyScale.y,
                    1f / transform.lossyScale.z);

                sticker = t.AddComponent<SpriteRenderer>();
            }
        }
    }

    Bounds GetModelBounds()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        Bounds b = new Bounds(transform.position, Vector3.zero);
        bool first = true;

        foreach (Renderer r in renderers)
        {
            // Only measure the real plate model, not the sticker
            if (r is SpriteRenderer)
                continue;

            if (first) { b = r.bounds; first = false; }
            else b.Encapsulate(r.bounds);
        }

        return b;
    }

    void LateUpdate()
    {
        // Make the sticker always face the camera
        if (sticker != null && sticker.gameObject.activeSelf && cam != null)
        {
            sticker.transform.rotation = cam.transform.rotation;
        }
    }

    // ---------- Placing / removing ----------

    // Only cooked (or burnt) qutab can go on the plate
    public bool CanAccept(GameObject obj)
    {
        if (HasItem) return false;

        Dough d = obj.GetComponentInParent<Dough>();
        if (d == null) return false;

        return d.stage == DoughStage.Cooked ||
               (acceptBurnt && d.stage == DoughStage.Burnt);
    }

    public void PlaceItem(GameObject obj)
    {
        if (!CanAccept(obj)) return;

        obj = obj.GetComponentInParent<Dough>().gameObject;

        // Stop it from falling and make sure the player's ray can hit it
        Rigidbody rb = obj.GetComponentInChildren<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        foreach (Collider c in obj.GetComponentsInChildren<Collider>(true))
            c.enabled = true;

        // On a carryable plate the qutab becomes part of the plate,
        // so it moves with it and doesn't push the plate around
        if (CanBePickedUp && rb != null)
        {
            Destroy(rb);
        }

        PlacementHelper.SnapTo(obj, platePoint, true);

        currentItem = obj;
        currentData = obj.GetComponent<Dough>();
        UpdateSticker();

        Debug.Log(DishName + " is on the plate!");
    }

    // Called when the player takes the qutab off the plate
    public void RemoveItem(GameObject obj)
    {
        if (currentItem != null &&
            (obj == currentItem || obj.transform.IsChildOf(currentItem.transform)))
        {
            currentItem = null;
            currentData = null;
            UpdateSticker();
        }
    }

    void UpdateSticker()
    {
        if (sticker == null) return;

        Sprite pic = null;
        bool darken = false;

        if (HasItem)
        {
            string key = string.IsNullOrEmpty(currentData.fillingName)
                ? "plain"
                : currentData.fillingName.ToLower();

            if (currentData.stage == DoughStage.Burnt)
            {
                // Burnt food has its own sticker, e.g. meat_burnt.png
                pic = GetSticker(key + "_burnt");

                // No burnt picture? Use the normal one, but dark
                if (pic == null)
                {
                    pic = GetSticker(key);
                    darken = true;
                }
            }
            else
            {
                pic = GetSticker(key);
            }
        }

        sticker.sprite = pic;
        sticker.gameObject.SetActive(pic != null);

        if (pic != null)
        {
            sticker.color = darken ? new Color(0.35f, 0.35f, 0.35f) : Color.white;

            // Make the sticker "stickerSize" big no matter the picture's size
            float w = pic.bounds.size.x;
            Vector3 s = transform.lossyScale;
            float k = stickerSize / Mathf.Max(w, 0.0001f);
            sticker.transform.localScale = new Vector3(
                k / Mathf.Abs(s.x), k / Mathf.Abs(s.y), k / Mathf.Abs(s.z));
        }
    }

    // ---------- Sticker pictures ----------

    static readonly Dictionary<string, Sprite> stickerCache = new Dictionary<string, Sprite>();

    // Loads Assets/Resources/Stickers/<key>.png (e.g. meat.png, meat_burnt.png)
    // Returns null if there is no such picture.
    static Sprite GetSticker(string key)
    {
        if (stickerCache.TryGetValue(key, out Sprite cached))
            return cached;

        Sprite sprite = Resources.Load<Sprite>("Stickers/" + key);

        if (sprite == null)
        {
            // Works even if the picture wasn't imported as a Sprite
            Texture2D tex = Resources.Load<Texture2D>("Stickers/" + key);
            if (tex != null)
            {
                sprite = Sprite.Create(
                    tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f),
                    tex.width);
            }
            else if (!key.EndsWith("_burnt"))
            {
                Debug.LogWarning("No sticker found: Assets/Resources/Stickers/" + key + ".png");
            }
        }

        stickerCache[key] = sprite;
        return sprite;
    }
}
