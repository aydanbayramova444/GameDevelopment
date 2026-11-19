using UnityEngine;

public enum DoughStage { Raw, Flat, Filled, Closed, Cooked, Burnt }

public class Dough : MonoBehaviour
{
    public DoughStage stage = DoughStage.Raw;

    [HideInInspector]
    public DoughDispenser sourceDispenser;

    // Which closed model to use when this qutab gets folded (set automatically)
    [HideInInspector]
    public GameObject closedPrefab;

    // What is inside this qutab, e.g. "meat" (set automatically when filling is added)
    public string fillingName = "";

    // Nice name for UI, e.g. "Meat qutab" or "Burnt meat qutab"
    public string DisplayName
    {
        get
        {
            string name = string.IsNullOrEmpty(fillingName)
                ? "Qutab"
                : char.ToUpper(fillingName[0]) + fillingName.Substring(1) + " qutab";

            if (stage == DoughStage.Burnt)
                name = "Burnt " + name.ToLower();

            return name;
        }
    }

    // Kept so older code that uses "isFlat" still works
    public bool isFlat
    {
        get { return stage != DoughStage.Raw; }
        set
        {
            if (value && stage == DoughStage.Raw) stage = DoughStage.Flat;
            else if (!value) stage = DoughStage.Raw;
        }
    }
}