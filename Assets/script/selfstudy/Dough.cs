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

    // Name for ANY stage, shown on screen, e.g. "Open greens qutab",
    // "Closed meat qutab (raw)", "Burnt greens qutab"
    public string KindName
    {
        get
        {
            string f = string.IsNullOrEmpty(fillingName) ? "" : fillingName + " ";
            string text;

            switch (stage)
            {
                case DoughStage.Raw:    text = "dough"; break;
                case DoughStage.Flat:   text = "flat dough"; break;
                case DoughStage.Filled: text = "open " + f + "qutab"; break;
                case DoughStage.Closed: text = "closed " + f + "qutab (raw)"; break;
                case DoughStage.Burnt:  text = "burnt " + f + "qutab"; break;
                default:                text = f + "qutab"; break;
            }

            return char.ToUpper(text[0]) + text.Substring(1);
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