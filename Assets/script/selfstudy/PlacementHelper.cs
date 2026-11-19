using UnityEngine;

// Puts an object on a point so that its VISIBLE MODEL lands there,
// even if the 3D model inside the prefab is shifted away from the prefab's pivot.
// Works for tilted points too (e.g. the curved saj): the model lies flat on the point.
public static class PlacementHelper
{
    // sitOnTop = true: model's bottom touches the point (for tables, saj).
    // sitOnTop = false: model's center is at the point (for holding in hand).
    public static void SnapTo(GameObject obj, Transform point, bool sitOnTop)
    {
        // "true" keeps the object's real size in the world
        obj.transform.SetParent(point, true);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;

        // Measure the model's box as seen from the point (its own "up" direction)
        Vector3 min = Vector3.one * float.MaxValue;
        Vector3 max = Vector3.one * float.MinValue;
        bool found = false;

        foreach (Renderer r in obj.GetComponentsInChildren<Renderer>())
        {
            // Skip floating stickers/labels, only measure the real 3D model
            if (r is SpriteRenderer ||
                r.GetComponent<TMPro.TMP_Text>() != null ||
                r.GetComponentInParent<TMPro.TMP_Text>() != null)
                continue;

            Bounds lb = r.localBounds;

            // The 8 corners of this part's box
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 corner = lb.center + Vector3.Scale(lb.extents, new Vector3(x, y, z));
                Vector3 world = r.transform.TransformPoint(corner);
                Vector3 local = point.InverseTransformPoint(world);

                min = Vector3.Min(min, local);
                max = Vector3.Max(max, local);
            }

            found = true;
        }

        if (!found) return;

        Vector3 center = (min + max) * 0.5f;

        // Where the model should be, in the point's own space
        Vector3 wanted = sitOnTop
            ? new Vector3(center.x, min.y, center.z)   // bottom-middle on the point
            : center;                                  // middle on the point

        // Move the whole object by the gap between the model and the point
        obj.transform.position -= point.TransformVector(wanted);
    }
}
