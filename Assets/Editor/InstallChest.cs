using System.Linq;
using Cainos.PixelArtTopDown_Basic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unisza;

public static class InstallChest
{
    const string ScenePath = "Assets/Cainos/Pixel Art Top Down - Basic/Scene/SC Demo.unity";
    const string ChestName = "PF Props - Chest 01";
    const string OpenPrefabPath = "Assets/Cainos/Pixel Art Top Down - Basic/Prefab/Props/PF Props - Chest 01 Open.prefab";
    const string CanvasName = "ChestPopupCanvas";
    const string PlayerName = "PF Player";

    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var chest = FindDeep(scene, ChestName);
        if (!chest) { Fail("chest not found: " + ChestName); return; }

        var chestSr = chest.GetComponent<SpriteRenderer>();
        SpriteRenderer shadowSr = chest.GetComponentsInChildren<SpriteRenderer>(true)
            .FirstOrDefault(sr => sr != chestSr);

        // Pull the "opened" artwork straight from the prefab the package already ships
        var openPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(OpenPrefabPath);
        if (!openPrefab) { Fail("open chest prefab missing"); return; }

        var openSr = openPrefab.GetComponent<SpriteRenderer>();
        Sprite openSprite = openSr ? openSr.sprite : null;
        Sprite openShadow = openPrefab.GetComponentsInChildren<SpriteRenderer>(true)
            .FirstOrDefault(sr => sr != openSr)?.sprite;

        Debug.Log($"INSTALL sprites closed={(chestSr ? chestSr.sprite.name : "null")} open={(openSprite ? openSprite.name : "null")}");

        // ---- trigger zone so the chest can sense the player ----
        // The trigger must live on the SAME GameObject as ChestInteract: Unity sends
        // trigger callbacks to the collider's own GameObject, never to a parent.
        var stale = chest.transform.Find("InteractZone");
        if (stale) Object.DestroyImmediate(stale.gameObject);

        // Leave the chest's original solid collider alone so it still blocks movement,
        // and add a separate, larger trigger for interaction detection.
        var box = chest.GetComponents<BoxCollider2D>()
            .FirstOrDefault(c => c.isTrigger);

        if (!box) box = chest.gameObject.AddComponent<BoxCollider2D>();

        box.isTrigger = true;
        box.size = new Vector2(2.6f, 2.2f);
        box.offset = new Vector2(0f, 0.35f);

        // ---- popup UI ----
        var popup = BuildPopup(scene);

        // ---- chest behaviour ----
        var interact = chest.GetComponent<ChestInteract>();
        if (!interact) interact = chest.AddComponent<ChestInteract>();
        interact.spriteRenderer = chestSr;
        interact.shadowRenderer = shadowSr;
        interact.closedSprite = chestSr ? chestSr.sprite : null;
        interact.openSprite = openSprite;
        interact.closedShadowSprite = shadowSr ? shadowSr.sprite : null;
        interact.openShadowSprite = openShadow;
        interact.popup = popup;
        interact.interactKey = KeyCode.E;
        interact.requireKeyPress = true;

        EditorUtility.SetDirty(interact);

        // ---- point-and-click movement ----
        var player = FindDeep(scene, PlayerName);
        if (player)
        {
            var pac = player.GetComponent<PointAndClickController>();
            if (!pac) pac = player.AddComponent<PointAndClickController>();

            pac.speed = 3f;
            pac.arriveThreshold = 0.15f;
            pac.manualInputCancelsClick = true;
            pac.showClickMarker = true;

            // The package controller writes the same velocity every frame; disable it
            // here as well so the switch survives even outside play mode.
            var legacy = player.GetComponent<TopDownCharacterController>();
            if (legacy) legacy.enabled = false;

            EditorUtility.SetDirty(pac);
            EditorUtility.SetDirty(legacy);
            Debug.Log($"INSTALL player={player.name} pointAndClick={pac.GetType().Name} legacyEnabled={(legacy ? legacy.enabled : false)}");
        }
        else
        {
            Debug.LogWarning("INSTALL_WARN player not found, point-and-click not wired");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("INSTALL_OK chest wired + popup created");
        EditorApplication.Exit(0);
    }

    static WelcomePopup BuildPopup(Scene scene)
    {
        var existing = FindDeep(scene, CanvasName);
        if (existing) Object.DestroyImmediate(existing);

        // Canvas
        var canvasGo = new GameObject(CanvasName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // Panel
        var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        panelGo.transform.SetParent(canvasGo.transform, false);

        var prt = panelGo.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 0.5f);
        prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.anchoredPosition = Vector2.zero;
        prt.sizeDelta = new Vector2(640f, 200f);

        var img = panelGo.GetComponent<Image>();
        img.color = new Color(0.08f, 0.07f, 0.10f, 0.94f);
        img.raycastTarget = false;

        var group = panelGo.GetComponent<CanvasGroup>();

        // Label. Using legacy UI.Text: this project never imported the TMP essentials,
        // so there is no TMP font asset available and TMP glyphs would render blank.
        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(panelGo.transform, false);

        var trt = textGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(30f, 30f);
        trt.offsetMax = new Vector2(-30f, -30f);

        var label = textGo.AddComponent<Text>();
        label.text = "Welcome to Unisza";
        label.fontSize = 52;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(1f, 0.93f, 0.72f, 1f);
        label.raycastTarget = false;
        label.fontStyle = FontStyle.Bold;

        var font = LoadBuiltinFont();
        if (font) label.font = font;
        Debug.Log("INSTALL font=" + (font ? font.name : "builtin-default"));

        // Outline for readability over the dark panel
        var outline = textGo.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        outline.effectDistance = new Vector2(3f, -3f);

        var popup = panelGo.AddComponent<WelcomePopup>();
        popup.group = group;
        popup.panel = prt;
        popup.message = "Welcome to Unisza";

        return popup;
    }

    static Font LoadBuiltinFont()
    {
        // LegacyRuntime.ttf replaced Arial.ttf as the built-in font in recent Unity.
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
               ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    static GameObject FindDeep(Scene scene, string name)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == name) return root;

            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name) return t.gameObject;
            }
        }
        return null;
    }

    static void Fail(string msg)
    {
        Debug.LogError("INSTALL_FAIL " + msg);
        EditorApplication.Exit(1);
    }
}
