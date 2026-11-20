using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// Tools > Kitchen > Create Greens Qutab
//
// Builds the green (göyərti) qutab from YOUR models in Assets/Prefabs/GreensModels:
//   Greens.obj           -> GreensFilling.prefab   (the greens you carry)
//   OpenGreensQutab.obj  -> OpenGreensQutab.prefab (open qutab with greens)
//   GreensBowl.obj       -> GreensDispenser in the scene (copy of the meat dispenser)
//
// Each one is a copy of the matching MEAT prefab with only the 3D model swapped,
// so size, collider, layer and physics are exactly like the meat ones.
// Safe to run again: prefabs are rebuilt, the dispenser is not duplicated.
public static class GreensQutabMenu
{
    const string ModelFolder = "Assets/Prefabs/GreensModels/";
    const string GreensModelPath = ModelFolder + "Greens.obj";
    const string OpenModelPath   = ModelFolder + "OpenGreensQutab.obj";
    const string BowlModelPath   = ModelFolder + "GreensBowl.obj";

    const string FallbackMeatFilling = "Assets/Prefabs/GroundMeat.prefab";

    const string GreensFillingPath = "Assets/Prefabs/GreensFilling.prefab";
    const string GreensOpenPath    = "Assets/Prefabs/OpenGreensQutab.prefab";

    [MenuItem("Tools/Kitchen/Create Greens Qutab")]
    static void Create()
    {
        GameObject greensModel = AssetDatabase.LoadAssetAtPath<GameObject>(GreensModelPath);
        GameObject openModel   = AssetDatabase.LoadAssetAtPath<GameObject>(OpenModelPath);
        GameObject bowlModel   = AssetDatabase.LoadAssetAtPath<GameObject>(BowlModelPath);

        if (greensModel == null || openModel == null)
        {
            Show("Could not find your greens models:\n" + GreensModelPath + "\n" + OpenModelPath);
            return;
        }

        // The meat dispenser in the open scene tells us which meat prefabs are used
        MeatDispenser meatDisp = FindMeatDispenser();

        GameObject meatFilling = meatDisp != null && meatDisp.meatPrefab != null
            ? meatDisp.meatPrefab
            : AssetDatabase.LoadAssetAtPath<GameObject>(FallbackMeatFilling);

        Filling meatF = meatFilling != null ? meatFilling.GetComponentInChildren<Filling>(true) : null;

        if (meatF == null || meatF.openQutabPrefab == null)
        {
            Show("Could not find the meat filling prefab (with its Open Qutab Prefab set).");
            return;
        }

        GameObject meatOpen = meatF.openQutabPrefab;

        // 1. Open greens qutab = open meat qutab with your model
        GameObject greensOpen = MakeCopy(meatOpen, GreensOpenPath, inst =>
        {
            inst.name = "OpenGreensQutab";
            SwapModel(inst, null, openModel);
        });

        // 2. Greens filling = meat filling with your model
        GameObject greensFilling = MakeCopy(meatFilling, GreensFillingPath, inst =>
        {
            inst.name = "GreensFilling";
            SwapModel(inst, null, greensModel);

            Filling f = inst.GetComponentInChildren<Filling>(true);
            f.type = FillingType.Greens;

            // Meat script -> Greens script (like the meat has Meat.cs)
            foreach (Meat m in inst.GetComponentsInChildren<Meat>(true))
                Object.DestroyImmediate(m);
            if (f.GetComponent<Greens>() == null)
                f.gameObject.AddComponent<Greens>();
            f.openQutabPrefab = greensOpen;
            // closedQutabPrefab stays: a closed qutab looks the same outside
        });

        // 3. Greens dispenser in the scene
        string dispenserMsg = MakeDispenser(meatDisp, greensFilling, bowlModel);

        AssetDatabase.SaveAssets();

        Show("Created:\n- " + GreensOpenPath + "\n- " + GreensFillingPath + "\n\n" + dispenserMsg);
    }

    // ---------- Dispenser ----------

    static string MakeDispenser(MeatDispenser meatDisp, GameObject greensFilling, GameObject bowlModel)
    {
        if (meatDisp == null)
            return "No meat dispenser found in the open scene, so no greens dispenser was made. " +
                   "Open SelfPractice and run the menu again.";

        // Already made before? Just update it.
        GreensDispenser existing = Object.FindFirstObjectByType<GreensDispenser>();
        if (existing != null)
        {
            existing.greensPrefab = greensFilling;
            EditorUtility.SetDirty(existing);
            EditorSceneManager.MarkSceneDirty(existing.gameObject.scene);
            return "Updated the existing greens dispenser '" + existing.name + "'. Save the scene (Ctrl+S).";
        }

        GameObject src = meatDisp.gameObject;
        GameObject copy = Object.Instantiate(src, src.transform.parent);
        copy.name = "GreensDispenser";
        copy.transform.position = src.transform.position + src.transform.right * 0.6f;
        copy.transform.rotation = src.transform.rotation;
        Undo.RegisterCreatedObjectUndo(copy, "Create Greens Dispenser");

        // MeatDispenser script -> GreensDispenser script
        MeatDispenser copiedMeat = copy.GetComponent<MeatDispenser>();
        Transform spawn = copiedMeat.meatSpawnPoint;
        Object.DestroyImmediate(copiedMeat);

        GreensDispenser disp = copy.AddComponent<GreensDispenser>();
        disp.greensPrefab = greensFilling;

        // Spawn point outside the dispenser? Give the copy its own one.
        if (spawn != null && !spawn.IsChildOf(copy.transform))
        {
            GameObject sp = new GameObject("GreensSpawnPoint");
            sp.transform.SetParent(copy.transform, false);
            sp.transform.position = copy.transform.TransformPoint(
                src.transform.InverseTransformPoint(meatDisp.meatSpawnPoint.position));
            sp.transform.rotation = meatDisp.meatSpawnPoint.rotation;
            spawn = sp.transform;
        }
        disp.greensSpawnPoint = spawn;

        // Swap the meat bowl for your greens bowl
        string bowlMsg = "";
        if (bowlModel != null)
        {
            int swapped = SwapModel(copy, "MeatBowl", bowlModel);
            if (swapped == 0)
                bowlMsg = "\n\nNote: couldn't find the meat bowl model inside the dispenser, " +
                          "so drag GreensBowl in by hand.";
        }

        EditorSceneManager.MarkSceneDirty(copy.scene);
        Selection.activeGameObject = copy;

        return "Made 'GreensDispenser' next to your meat dispenser (with your greens bowl). " +
               "Move it where you like, then save the scene (Ctrl+S)." + bowlMsg;
    }

    static MeatDispenser FindMeatDispenser()
    {
        // Prefer the one selected in the Hierarchy
        if (Selection.activeGameObject != null)
        {
            MeatDispenser sel = Selection.activeGameObject.GetComponentInParent<MeatDispenser>();
            if (sel != null && !EditorUtility.IsPersistent(sel))
                return sel;
        }

        return Object.FindFirstObjectByType<MeatDispenser>();
    }

    // ---------- Helpers ----------

    // Replaces 3D meshes inside "target" with the meshes of "newModel" (in order).
    // onlyFromModel: only replace meshes whose file name contains this (null = all).
    // Colliders that used the old mesh get the new one too. Returns how many were swapped.
    static int SwapModel(GameObject target, string onlyFromModel, GameObject newModel)
    {
        MeshFilter[] newFilters = newModel.GetComponentsInChildren<MeshFilter>(true);
        int n = 0;

        foreach (MeshFilter mf in target.GetComponentsInChildren<MeshFilter>(true))
        {
            if (n >= newFilters.Length) break;
            if (mf.sharedMesh == null) continue;

            if (onlyFromModel != null &&
                !AssetDatabase.GetAssetPath(mf.sharedMesh).Contains(onlyFromModel))
                continue;

            Mesh oldMesh = mf.sharedMesh;
            MeshFilter from = newFilters[n];

            mf.sharedMesh = from.sharedMesh;

            MeshRenderer r = mf.GetComponent<MeshRenderer>();
            MeshRenderer fromR = from.GetComponent<MeshRenderer>();
            if (r != null && fromR != null)
                r.sharedMaterials = fromR.sharedMaterials;

            foreach (MeshCollider mc in target.GetComponentsInChildren<MeshCollider>(true))
                if (mc.sharedMesh == oldMesh) mc.sharedMesh = from.sharedMesh;

            n++;
        }

        return n;
    }

    static GameObject MakeCopy(GameObject source, string path, System.Action<GameObject> edit)
    {
        GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(source);

        // Make it its own prefab (not linked to the meat one), so we can remove scripts
        PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.OutermostRoot,
            InteractionMode.AutomatedAction);

        edit(inst);
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(inst, path);
        Object.DestroyImmediate(inst);
        return saved;
    }

    static void Show(string msg)
    {
        EditorUtility.DisplayDialog("Greens Qutab", msg, "OK");
    }
}
