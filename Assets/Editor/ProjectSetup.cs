using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class ProjectSetup
{
    private const string ScenePath = "Assets/Scenes/Main.unity";

    static ProjectSetup()
    {
        EditorApplication.delayCall += EnsureProjectScene;
    }

    [MenuItem("STRIKE ZONE/Create or repair main scene")]
    public static void EnsureProjectScene()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;

        if (!Directory.Exists("Assets/Scenes"))
            Directory.CreateDirectory("Assets/Scenes");

        if (!File.Exists(ScenePath))
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("STRIKE ZONE Game");
            root.AddComponent<GameBootstrap>();
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };
        AssetDatabase.Refresh();
    }
}


public class BuildSceneGuard : IPreprocessBuildWithReport
{
    public int callbackOrder { get { return 0; } }

    public void OnPreprocessBuild(BuildReport report)
    {
        ProjectSetup.EnsureProjectScene();
        if (!File.Exists("Assets/Scenes/Main.unity"))
            throw new BuildFailedException("STRIKE ZONE main scene could not be created.");
    }
}
