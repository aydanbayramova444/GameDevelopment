using UnityEngine;
using UnityEditor;

// Adds a menu item: Tools > Kitchen > Make Selected Object a Plate
public static class PlateSetupMenu
{
    [MenuItem("Tools/Kitchen/Make Selected Object a Plate")]
    static void MakePlate()
    {
        GameObject[] selected = Selection.gameObjects;

        if (selected.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "Make Plate",
                "First click your plate in the Hierarchy, then use this menu again.",
                "OK");
            return;
        }

        foreach (GameObject go in selected)
        {
            Plate plate = go.GetComponent<Plate>();
            if (plate == null)
            {
                plate = Undo.AddComponent<Plate>(go);
            }

            Undo.RegisterFullObjectHierarchyUndo(go, "Make Plate");
            plate.AutoSetup();

            // Show the meat sticker in the editor so you can see/move it,
            // it hides itself in Play mode until food is on the plate
            if (plate.sticker != null && plate.sticker.sprite == null)
            {
                Sprite preview = AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/Resources/Stickers/meat.png");
                if (preview != null)
                {
                    plate.sticker.sprite = preview;
                }
            }

            EditorUtility.SetDirty(go);
            Debug.Log(go.name + " is now a plate! Save the scene (Ctrl+S).");
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            selected[0].scene);
    }
}
