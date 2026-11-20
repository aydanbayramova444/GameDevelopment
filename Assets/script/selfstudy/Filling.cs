using UnityEngine;
using UnityEngine.Serialization;

// All fillings in the game. To add a new one (e.g. pumpkin), add it here,
// then add its name in Filling.NameOf below and a sticker in Resources/Stickers.
public enum FillingType
{
    [InspectorName("Meat (Ət)")]         Meat,
    [InspectorName("Greens (Göyərti)")]  Greens,
}

public class Filling : MonoBehaviour
{
    // Choose the filling from the list in the Inspector
    public FillingType type = FillingType.Meat;

    // Model shown after adding this filling to flat dough
    public GameObject openQutabPrefab;

    // Model shown after closing (folding) the qutab
    public GameObject closedQutabPrefab;

    [HideInInspector]
    public MeatDispenser sourceDispenser;

    // Short name used by the game: "meat" or "greens".
    // It picks the plate sticker (meat.png, greens.png) and the text "Greens qutab".
    public string fillingName => NameOf(type);

    // Name shown to the player, e.g. "Press E to add greens (goyerti)"
    public string DisplayName => type == FillingType.Greens ? "greens (goyerti)" : fillingName;

    public static string NameOf(FillingType t)
    {
        switch (t)
        {
            case FillingType.Greens: return "greens";
            default:                 return "meat";
        }
    }

    // ---------- Keeps old prefabs working ----------
    // Older versions stored the name as text ("meat", "greens").
    // This reads that old text once and picks the matching type.
    [SerializeField, HideInInspector, FormerlySerializedAs("fillingName")]
    private string oldFillingName;

    void Awake() => ReadOldName();
    void OnValidate() => ReadOldName();

    void ReadOldName()
    {
        if (string.IsNullOrEmpty(oldFillingName)) return;

        string old = oldFillingName.ToLower();
        if (old.Contains("green") || old.Contains("goy") || old.Contains("göy"))
            type = FillingType.Greens;
        else if (old.Contains("meat"))
            type = FillingType.Meat;

        oldFillingName = "";
    }
}
