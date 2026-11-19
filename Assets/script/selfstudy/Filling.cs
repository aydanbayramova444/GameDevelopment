using UnityEngine;

public class Filling : MonoBehaviour
{
    public string fillingName = "meat";

    // Model shown after adding this filling to flat dough
    public GameObject openQutabPrefab;

    // Model shown after closing (folding) the qutab
    public GameObject closedQutabPrefab;

    [HideInInspector]
    public MeatDispenser sourceDispenser;
}