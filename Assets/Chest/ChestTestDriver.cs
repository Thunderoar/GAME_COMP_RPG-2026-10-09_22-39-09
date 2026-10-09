using System.Collections;
using System.IO;
using System.Reflection;
using Cainos.PixelArtTopDown_Basic;
using UnityEngine;
using UnityEngine.UI;
using Unisza;

/// <summary>
/// SCRATCH-ONLY harness. Drives the chest interaction in Play mode so the trigger,
/// sprite swap and popup can be verified without synthesising keyboard input.
/// Not copied into the real project.
/// </summary>
public class ChestTestDriver : MonoBehaviour
{
    public const string OutDir = "ShotsPlay";
    const string DoneMarker = "chesttest.done";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        bool wanted = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-chesttest") >= 0;
        if (!wanted) return;

        new GameObject("ChestTestDriver").AddComponent<ChestTestDriver>();
    }

    IEnumerator Start()
    {
        Directory.CreateDirectory(OutDir);

        var chest = FindObjectOfType<ChestInteract>();
        var player = GameObject.Find("PF Player");

        if (!chest || !player)
        {
            Debug.Log("TEST_FAIL missing chest or player");
            Finish();
            yield break;
        }

        var canvas = FindObjectOfType<Canvas>();
        var pac = player.GetComponent<PointAndClickController>();
        var body = player.GetComponent<Rigidbody2D>();

        Debug.Log($"TEST canvas={(canvas ? canvas.name : "none")} chestAt={chest.transform.position} playerAt={player.transform.position}");
        Debug.Log($"TEST pointAndClick={(pac ? "present" : "MISSING")} legacyEnabled={player.GetComponent<TopDownCharacterController>()?.enabled}");

        // ---------- 1. point-and-click movement ----------
        if (pac)
        {
            Vector3 start = player.transform.position;
            // Open grass to the left; the +X side is blocked by a wall at x~7.
            Vector3 goal = start + new Vector3(-2.5f, 0f, 0f);

            SetTarget(pac, goal);
            Debug.Log($"TEST walkStart={start} walkGoal={goal}");

            yield return Capture("pac_01_target_set.png", canvas);

            // let it walk
            for (int i = 0; i < 150; i++) yield return null;

            Vector3 end = player.transform.position;
            float travelled = Vector3.Distance(start, end);
            float error = Vector3.Distance(end, goal);
            bool stopped = (Vector2)body.linearVelocity == Vector2.zero;

            Debug.Log($"TEST walkEnd={end} travelled={travelled:F2} errorToGoal={error:F2} stopped={stopped}");

            yield return Capture("pac_02_arrived.png", canvas);
        }
        else
        {
            Debug.Log("TEST_FAIL point-and-click controller missing");
        }

        // ---------- 2. chest interaction ----------
        body.linearVelocity = Vector2.zero;
        player.transform.position = chest.transform.position + new Vector3(0f, 0.35f, 0f);

        for (int i = 0; i < 20; i++) yield return null;

        var flag = typeof(ChestInteract)
            .GetField("playerInRange", BindingFlags.NonPublic | BindingFlags.Instance);
        bool inRange = flag != null && (bool)flag.GetValue(chest);
        Debug.Log("TEST triggerDetectedPlayer=" + inRange);
        Debug.Log("TEST chestSpriteBefore=" + chest.spriteRenderer.sprite.name);

        yield return Capture("play_01_before.png", canvas);

        chest.Open();

        for (int i = 0; i < 25; i++) yield return null;

        Debug.Log("TEST chestSpriteAfter=" + chest.spriteRenderer.sprite.name);
        Debug.Log("TEST popupAlpha=" + chest.popup.group.alpha);

        yield return Capture("play_02_popup.png", canvas);

        Finish();
    }

    static void SetTarget(PointAndClickController pac, Vector3 goal)
    {
        var f = typeof(PointAndClickController).GetField("target", BindingFlags.NonPublic | BindingFlags.Instance);
        f.SetValue(pac, goal);

        var has = typeof(PointAndClickController).GetField("hasTarget", BindingFlags.NonPublic | BindingFlags.Instance);
        has.SetValue(pac, true);
    }

    IEnumerator Capture(string name, Canvas canvas)
    {
        yield return new WaitForEndOfFrame();

        // Grab the real back buffer: this includes ScreenSpaceOverlay UI, which
        // Camera.Render() into a RenderTexture would miss.
        var tex = ScreenCapture.CaptureScreenshotAsTexture();

        File.WriteAllBytes(Path.Combine(OutDir, name), tex.EncodeToPNG());
        Debug.Log($"TEST saved {name} {tex.width}x{tex.height}");

        Destroy(tex);
    }

    void Finish()
    {
        File.WriteAllText(DoneMarker, "done");
        Debug.Log("TEST finished");
        Application.Quit(0);
    }
}
