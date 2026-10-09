using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Probe
{
    const string ScenePath = "Assets/Cainos/Pixel Art Top Down - Basic/Scene/SC Demo.unity";

    public static void Run()
    {
        var sb = new StringBuilder();
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        sb.AppendLine("== CHEST-LIKE OBJECTS IN SCENE ==");
        foreach (var root in scene.GetRootGameObjects())
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            var n = t.name.ToLowerInvariant();
            if (!n.Contains("chest")) continue;
            sb.AppendLine($"OBJ '{t.name}' pos={t.position} layer={LayerMask.LayerToName(t.gameObject.layer)}");
            foreach (var c in t.GetComponents<Component>())
                sb.AppendLine($"   comp {c.GetType().Name}");
            foreach (var sr in t.GetComponentsInChildren<SpriteRenderer>(true))
                sb.AppendLine($"   sr '{sr.gameObject.name}' sprite={(sr.sprite != null ? sr.sprite.name : "NULL")} layer='{sr.sortingLayerName}' order={sr.sortingOrder} color={sr.color}");
        }

        sb.AppendLine("== ROOTS / CANVAS ==");
        foreach (var root in scene.GetRootGameObjects())
        {
            sb.AppendLine($"ROOT '{root.name}' active={root.activeSelf} children={root.transform.childCount}");
            foreach (var cv in root.GetComponentsInChildren<Canvas>(true))
                sb.AppendLine($"   canvas '{cv.name}' mode={cv.renderMode}");
            foreach (var es in root.GetComponentsInChildren<UnityEngine.EventSystems.EventSystem>(true))
                sb.AppendLine("   eventsystem present");
        }

        sb.AppendLine("== PLAYER ==");
        foreach (var root in scene.GetRootGameObjects())
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (!t.name.ToLowerInvariant().Contains("player")) continue;
            sb.AppendLine($"PLAYERISH '{t.name}' pos={t.position} layer={LayerMask.LayerToName(t.gameObject.layer)}");
            foreach (var c in t.GetComponents<Component>()) sb.AppendLine($"   comp {c.GetType().Name}");
            foreach (var col in t.GetComponentsInChildren<Collider2D>(true))
                sb.AppendLine($"   col2d '{col.name}' type={col.GetType().Name} isTrigger={col.isTrigger}");
        }

        sb.AppendLine("== TMP AVAILABLE ==");
        sb.AppendLine("TextMeshProUGUI=" + (System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro") != null));
        sb.AppendLine("UnityEngine.UI.Text=" + (System.Type.GetType("UnityEngine.UI.Text, UnityEngine.UI") != null));

        sb.AppendLine("== SORTING LAYERS ==");
        foreach (var name in UnityEditorInternal.InternalEditorUtility.layers) sb.AppendLine($"   layer {name}");
        foreach (var sl in UnityEngine.SortingLayer.layers) sb.AppendLine($"   sorting {sl.name}");

        sb.AppendLine("== CHEST PREFABS ==");
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab Chest", new[] { "Assets" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            sb.AppendLine($"PREFAB {path}");
            foreach (var c in pf.GetComponents<Component>()) sb.AppendLine($"   comp {c.GetType().Name}");
            foreach (var sr in pf.GetComponentsInChildren<SpriteRenderer>(true))
                sb.AppendLine($"   sr '{sr.gameObject.name}' sprite={(sr.sprite != null ? sr.sprite.name : "NULL")} localScale={sr.transform.localScale} localPos={sr.transform.localPosition} order={sr.sortingOrder}");
        }

        Debug.Log(sb.ToString());
        EditorApplication.Exit(0);
    }
}
