using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PlayTest
{
    const string ScenePath = "Assets/Cainos/Pixel Art Top Down - Basic/Scene/SC Demo.unity";
    const string Marker = "chesttest.done";

    static bool entered;

    public static void Run()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        if (System.IO.File.Exists(Marker)) System.IO.File.Delete(Marker);

        EditorApplication.update += Tick;
        EditorApplication.EnterPlaymode();

        // Safety net so batch mode never hangs forever
        EditorApplication.update += () =>
        {
            if (EditorApplication.timeSinceStartup > 0) { }
        };
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying && !entered)
        {
            entered = true;
            return;
        }

        if (System.IO.File.Exists(Marker))
        {
            Debug.Log("PLAYTEST marker seen, exiting");
            EditorApplication.update -= Tick;
            EditorApplication.Exit(0);
        }

        if (EditorApplication.timeSinceStartup > 300)
        {
            Debug.Log("PLAYTEST timeout, exiting");
            EditorApplication.update -= Tick;
            EditorApplication.Exit(1);
        }
    }
}
