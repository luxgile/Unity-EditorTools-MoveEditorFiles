using System.IO;

using UnityEngine;

using UnityEditor;

public class MoveEditorFilesWindow : ScriptableWizard
{
    private DefaultAsset scriptFolder;
    private DefaultAsset editorFolder;

    [MenuItem("Tools/Move Editor Files")]
    public static void Init()
    {
        DisplayWizard<MoveEditorFilesWindow>("Move Editor Files");
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox("This operation will move all objects that inherits from 'Editor', 'EditorWindow' and 'PropertyDrawer' " +
            "or if his parent folder is 'Editor' to the specified editor folder. Will leave the path exactly the same in the editor folder." +
            "This should be used mainly to separate editor scripts from runtime scripts into different assemblies.", MessageType.Info);

        scriptFolder = EditorGUILayout.ObjectField("Scripts folder", scriptFolder, typeof(DefaultAsset), false) as DefaultAsset;
        editorFolder = EditorGUILayout.ObjectField("Editor folder", editorFolder, typeof(DefaultAsset), false) as DefaultAsset;

        EditorGUILayout.HelpBox("If this operation hasn't been done in a while, this can take several seconds to complete.", MessageType.Warning);

        if (GUILayout.Button("Move files"))
            MoveFiles();
    }

    private void MoveFiles()
    {
        if (scriptFolder == null || editorFolder == null)
        {
            ShowNotification(new GUIContent("Folders not specified. Please assing a valid folder"), 3);
            return;
        }

        string appPath = Application.dataPath.Replace("Assets", "") + AssetDatabase.GetAssetPath(scriptFolder);
        string[] assetPaths = Directory.GetFiles(appPath, "*.cs", SearchOption.AllDirectories);
        int assetsMoved = 0;

        try
        {
            for (int i = 0; i < assetPaths.Length; i++)
            {
                if (EditorUtility.DisplayCancelableProgressBar("Moving editor scripts", "Moving folders... (" + i + "/" + assetPaths.Length + ")", (float)i / (float)assetPaths.Length))
                    break;

                string path = AssetDatabase.GetAssetPath(scriptFolder) + assetPaths[i].Replace(appPath, "").Replace('\\', '/');
                string targetPath = AssetDatabase.GetAssetPath(editorFolder) + assetPaths[i].Replace(appPath, "").Replace('\\', '/');
                Object asset = AssetDatabase.LoadAssetAtPath(path, typeof(Object));

                if (asset is MonoScript)
                {
                    MonoScript script = asset as MonoScript;
                    if (script != null && script.GetClass() != null
                        && (script.GetClass().IsSubclassOf(typeof(UnityEditor.Editor))
                        || script.GetClass().IsSubclassOf(typeof(EditorWindow))
                        || script.GetClass().IsSubclassOf(typeof(PropertyDrawer))
                        || IsParentClassEditor(script)))
                    {
                        CreatePath(targetPath);
                        string error = AssetDatabase.ValidateMoveAsset(path, targetPath);
                        if (string.IsNullOrEmpty(error))
                        {
                            Undo.RegisterCompleteObjectUndo(script, "Move " + script.name + " to editor folder");
                            AssetDatabase.MoveAsset(path, targetPath);
                            assetsMoved++;
                        }
                        else
                            Debug.LogError(error);
                    }

                }
            }
        }
        catch (System.Exception e) { Debug.LogException(e); }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        ShowNotification(new GUIContent("Moved " + assetsMoved + " files to editor folder"), 3);

        EditorUtility.ClearProgressBar();
    }

    private bool IsParentClassEditor(Object obj)
    {
        string path = AssetDatabase.GetAssetPath(obj);
        string[] pathSteps = path.Split('/');
        if (pathSteps.Length == 1)
            return false;
        else if (pathSteps.Length > 1)
            return pathSteps[pathSteps.Length - 2] == "Editor" || pathSteps[1] == "editor";

        return false;
    }

    private void CreatePath(string path)
    {
        string[] pathDivided = path.Split('/');
        string currentPath = pathDivided[0];
        for (int i = 1; i < pathDivided.Length; i++)
        {
            string folderName = pathDivided[i];
            if (folderName.Contains(".cs"))
                break;

            if (!AssetDatabase.IsValidFolder(currentPath + "/" + folderName))
                AssetDatabase.CreateFolder(currentPath, folderName);

            currentPath += "/" + folderName;
        }
    }
}