#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

// Explicit development-player screenshot command; inactive in ordinary play/builds.
public sealed class ForestioVisualCapture : MonoBehaviour
{
    private string output;
    private RenderTexture target;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartCaptureIfRequested()
    {
        string[] args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, "-forestio-capture");
        if (index < 0 || index + 1 >= args.Length) return;
        var capture = new GameObject("DevelopmentVisualCapture").AddComponent<ForestioVisualCapture>();
        capture.output = args[index + 1];
    }
    private IEnumerator Start()
    {
        Directory.CreateDirectory(output);
        Application.runInBackground = true;
        Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
        target = new RenderTexture(1280, 720, 24);
        target.Create();
        Camera.main.targetTexture = target;
        Camera.main.aspect = 16f / 9f;
        ForestioPlayerController player = FindFirstObjectByType<ForestioPlayerController>();
        player.enabled = false;
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        body.simulated = true;
        yield return new WaitForSecondsRealtime(1f);
        yield return Save("start");
        player.SendMessage("TryPickUpStone", SendMessageOptions.RequireReceiver);
        ForestioFrameAnimation animation = player.GetComponent<ForestioFrameAnimation>();
        animation.enabled = false;
        var setFrame = typeof(ForestioFrameAnimation).GetMethod("SetFrame", BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (int frame in new[] { 0, 4, 5, 9, 10, 14 })
        {
            setFrame.Invoke(animation, new object[] { frame });
            player.SendMessage("PositionHeldStone", SendMessageOptions.RequireReceiver);
            yield return Save("mouth-right-" + frame);
            player.GetComponent<SpriteRenderer>().flipX = true;
            player.SendMessage("PositionHeldStone", SendMessageOptions.RequireReceiver);
            yield return Save("mouth-left-" + frame);
            player.GetComponent<SpriteRenderer>().flipX = false;
        }
        animation.enabled = true;
        player.TryConsumeStone();
        body.position = new Vector2(-10f, -2.04f); body.linearVelocity = Vector2.zero;
        yield return new WaitForSecondsRealtime(0.8f);
        yield return Save("spikes");
        player.transform.position = new Vector3(0.2f, -2.04f, 0f);
        yield return new WaitForSecondsRealtime(0.8f);
        yield return Save("junction");
        body.position = new Vector2(8f, -2.04f); body.linearVelocity = Vector2.zero;
        yield return new WaitForSecondsRealtime(0.8f);
        yield return Save("shutter-closed");
        player.transform.position = new Vector3(6.2f, 2.44f, 0f);
        yield return new WaitForSecondsRealtime(0.8f);
        yield return Save("button");
        var stonePrefab = (ForestioStoneProjectile)typeof(ForestioPlayerController).GetField("projectilePrefab", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(player);
        var buttonStone = Instantiate(stonePrefab, new Vector3(6.9f, 2.52f, 0f), Quaternion.identity);
        buttonStone.Launch(1f, 12f, 9f, player.transform, false);
        yield return new WaitForSecondsRealtime(0.8f);
        body.position = new Vector2(8f, -2.04f); body.linearVelocity = Vector2.zero;
        yield return new WaitForSecondsRealtime(0.8f);
        yield return Save("shutter-open");
        FindFirstObjectByType<ForestioRobotController>().DisableRobot();
        body.position = new Vector2(16f, -2.04f); body.linearVelocity = Vector2.zero;
        yield return new WaitForSecondsRealtime(0.6f);
        yield return Save("robot-items");
        player.UnlockChargedThrow();
        ForestioAbilityNotice notice = FindFirstObjectByType<ForestioAbilityNotice>();
        notice.Show(player);
        yield return new WaitForSecondsRealtime(0.7f);
        yield return Save("ability");
        notice.TryClose();
        ForestioBranchBarrier barrier = FindFirstObjectByType<ForestioBranchBarrier>();
        var prefab = (ForestioStoneProjectile)typeof(ForestioPlayerController).GetField("projectilePrefab", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(player);
        ForestioStoneProjectile stone = Instantiate(prefab, barrier.transform.position, Quaternion.identity);
        stone.Launch(1f, 16f, 16f, null, true);
        barrier.TryBreak(stone);
        body.position = new Vector2(16f, -2.04f); body.linearVelocity = Vector2.zero;
        yield return new WaitForSecondsRealtime(1.1f);
        if (!FindFirstObjectByType<ForestioLevelCompletion>().TryComplete(player))
            throw new InvalidOperationException("Capture could not begin home reunion.");
        yield return new WaitForSecondsRealtime(2f);
        yield return Save("home-walk");
        yield return new WaitForSecondsRealtime(3.1f);
        yield return Save("home-rest");
        File.WriteAllText(Path.Combine(output, "capture-complete.txt"), "VISUAL_CAPTURE_OK");
        Application.Quit();
    }
    private IEnumerator Save(string name)
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        var request = new RenderPipeline.StandardRequest { destination = target };
        RenderPipeline.SubmitRenderRequest(Camera.main, request);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = target;
        Texture2D image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
        image.Apply();
        RenderTexture.active = previous;
        File.WriteAllBytes(Path.Combine(output, name + ".png"), image.EncodeToPNG());
        Destroy(image);
    }
}
#endif
