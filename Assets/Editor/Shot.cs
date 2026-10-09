using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Shot
{
    const string ScenePath = "Assets/Cainos/Pixel Art Top Down - Basic/Scene/SC Demo.unity";
    const string OutDir = "Shots";

    static Camera cam;

    public static void Run()
    {
        Directory.CreateDirectory(OutDir);
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        cam = Camera.main;
        if (cam == null) { Debug.LogError("SHOT_FAIL: no main camera"); EditorApplication.Exit(1); return; }

        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.09f, 0.10f, 0.13f, 1f);

        // Full map: bounds span 42 x 36 world units, 16:9 aspect
        // vertical half-extent 18 needs ortho size >= 18; use 18.5 plus a little margin
        Capture("01_overview", new Vector3(3f, 5f, -20f), 18.5f);

        // Gameplay framing (in-game ortho size is 5)
        Capture("02_player", new Vector3(5.82f, 2.31f, -20f), 5f);
        Capture("03_village", new Vector3(11f, 1f, -20f), 5f);
        Capture("04_graveyard", new Vector3(-2f, 11f, -20f), 5f);
        Capture("05_ruins", new Vector3(14f, 12f, -20f), 5f);
        Capture("06_altar", new Vector3(-9f, 4f, -20f), 5f);
        Capture("07_north", new Vector3(6f, 19f, -20f), 5f);

        Debug.Log("SHOT_DONE");
        EditorApplication.Exit(0);
    }

    static void Capture(string name, Vector3 pos, float size)
    {
        cam.transform.position = pos;
        cam.orthographicSize = size;

        int w = 1280, h = 720;
        var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 1;
        cam.targetTexture = rt;

        cam.Render();

        var prevActive = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        RenderTexture.active = prevActive;

        cam.targetTexture = null;
        File.WriteAllBytes(Path.Combine(OutDir, name + ".png"), tex.EncodeToPNG());

        Object.DestroyImmediate(tex);
        rt.Release();
        Object.DestroyImmediate(rt);

        Debug.Log("SHOT_SAVED " + name);
    }
}
