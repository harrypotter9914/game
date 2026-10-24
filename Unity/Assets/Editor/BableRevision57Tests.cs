using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Characters.Enemies;
using Babel.Runtime.Combat;
using Babel.Runtime.Core;
using Babel.Runtime.World;
using Bable;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class BableRevision57Tests {
    const string Flag = "Bable57.Tests";
    static readonly List<string> checks = new(), failures = new();
    static double started;
    static Camera camera;
    static CameraFollow2D follow;
    static PassageVisibility zoom;
    static PlayerController2D player;

    static BableRevision57Tests() { EditorApplication.playModeStateChanged += Changed; }
    public static void Batch() {
        Directory.CreateDirectory("../reference/revision57");
        SessionState.SetBool(Flag, true);
        SessionState.SetBool("Bable.PracticePreview", false);
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        EditorApplication.EnterPlaymode();
    }
    static void Changed(PlayModeStateChange state) {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Flag, false)) return;
        started = EditorApplication.timeSinceStartup;
        EditorApplication.update += Watch;
        var host = new GameObject("Camera region and dead-zone regression").AddComponent<BableTestHost>();
        Object.DontDestroyOnLoad(host);
        host.StartCoroutine(Test());
    }
    static void Watch() { if (EditorApplication.timeSinceStartup - started > 180) { Check("Timeout", false); Finish(); } }
    static void Check(string name, bool ok) {
        checks.Add(name); if (!ok) failures.Add(name);
        Debug.Log("REV57 " + (ok ? "PASS " : "FAIL ") + name);
        File.WriteAllText("../reference/revision57/tests.json", JsonUtility.ToJson(new BableVerification.Report { checks = checks.ToArray(), failures = failures.ToArray() }, true));
    }
    static IEnumerator Ready() {
        yield return null;
        while (TowerLoading.Busy || BableGameUI.Instance == null || !BableGameUI.Instance.Ready) yield return null;
        yield return new WaitForSecondsRealtime(.3f);
    }
    static void Place(Vector2 position) { player.transform.position = position; follow.SetTarget(player.transform); }
    static float CameraDistance(Vector3 point) => Vector3.Distance(point, camera.transform.position);

    static IEnumerator Test() {
        yield return Ready(); BableGameUI.Instance.Begin(); yield return null;
        Object.FindFirstObjectByType<TowerPrologue>().Skip(); yield return Ready();
        player = Object.FindFirstObjectByType<PlayerController2D>();
        player.GetComponent<HealthComponent>().Invincible = true;
        player.InterruptForHit(); player.enabled = false;
        player.GetComponent<Rigidbody2D>().simulated = false;
        foreach (var e in Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None)) {
            e.enabled = false; e.CancelInvoke(); e.GetComponent<Rigidbody2D>().simulated = false;
            if (e.TryGetComponent<BossEncounter>(out var encounter)) encounter.enabled = false;
        }
        foreach (var speech in Object.FindObjectsByType<BossSpeech>(FindObjectsSortMode.None)) speech.enabled = false;
        TowerDialogue.AbortStory();
        camera = Camera.main; follow = camera.GetComponent<CameraFollow2D>(); zoom = camera.GetComponent<PassageVisibility>();
        camera.aspect = 16f / 9;
        Check("Automated campaign save is isolated", LocalStorage.Root.Contains("revision57"));
        Check("Normal view shows fourteen vertical tiles", Mathf.Abs(zoom.maximumSize - 7) < .001f);
        Check("Labyrinth close view retains seven vertical tiles", Mathf.Abs(zoom.minimumSize - 3.5f) < .001f);
        Check("No global fog is created", GameObject.Find("Unexplored passage curtain") == null);

        var places = new[] { new Vector2(6, -10.17f), new Vector2(79, -49), new Vector2(220, -37), new Vector2(392, 28.5f), new Vector2(200, 85) };
        foreach (var point in places) {
            Place(point); yield return new WaitForSeconds(.35f);
            Check("Ordinary route stays wide near walls " + point, !zoom.InLabyrinth && Mathf.Abs(camera.orthographicSize - 7) < .001f);
        }
        foreach (var boss in Object.FindObjectsByType<BossBrain>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
            Place(boss.arenaCenter); yield return null;
            Check(boss.profile.kind + " arena keeps normal view", !zoom.InLabyrinth && Mathf.Abs(camera.orthographicSize - 7) < .001f);
        }

        Place(places[0]); yield return null; BableVerification.Capture("revision57/forest-wide.png");
        var ground = GameObject.Find("GroundTilemap").GetComponent<Tilemap>();
        NarrativeGuidance.Instance.RestoreSeen(new[] { "seek_blade", "initial", "movement", "fallen" });
        var mazePoint = Vector3.zero;
        for (int y = -84; y <= -68 && mazePoint == Vector3.zero; y++)
            for (int x = 280; x <= 329 && mazePoint == Vector3.zero; x++) {
                var c = new Vector3Int(x, y, 0);
                if (!ground.HasTile(c) && !ground.HasTile(c + Vector3Int.up) && !ground.HasTile(c + Vector3Int.up * 2)
                    && !ground.HasTile(c + Vector3Int.left) && !ground.HasTile(c + Vector3Int.right) && ground.HasTile(c + Vector3Int.down))
                    mazePoint = ground.GetCellCenterWorld(c) - Vector3.up * .3f;
            }
        Check("Found authored lower-right maze corridor", mazePoint != Vector3.zero);
        Place(mazePoint); yield return null;
        Check("Only the maze selects close view", zoom.InLabyrinth && Mathf.Abs(camera.orthographicSize - 3.5f) < .001f);
        BableVerification.Capture("revision57/labyrinth-close.png");
        float size = camera.orthographicSize; player.transform.position = places[0];
        yield return null;
        Check("Leaving maze starts a gradual zoom", camera.orthographicSize > size && camera.orthographicSize < 4);
        float end = Time.time + 4.6f, rate = 0;
        while (Time.time < end) {
            float previous = camera.orthographicSize; yield return null;
            rate = Mathf.Max(rate, Mathf.Abs(camera.orthographicSize - previous) / Mathf.Max(.0001f, Time.deltaTime));
        }
        Check("Zoom transition remains rate limited", rate <= .81f);
        Check("Exit restores fixed ordinary framing", Mathf.Abs(camera.orthographicSize - 7) < .001f);
        Place(new Vector2(257.1f, -74)); yield return null;
        player.transform.position = new Vector2(256.8f, -74); yield return null;
        Check("Small movement on maze seam does not toggle zoom", zoom.InLabyrinth);
        player.transform.position = new Vector2(255.9f, -74); yield return null;
        Check("Leaving transition margin exits maze", !zoom.InLabyrinth);

        foreach (float aspect in new[] { 16f / 9, 4f / 3, 21f / 9, .75f }) {
            camera.aspect = aspect; Place(places[0]); yield return null;
            var origin = camera.transform.position;
            float halfX = camera.orthographicSize * aspect * follow.DeadZoneSize.x;
            float halfY = camera.orthographicSize * follow.DeadZoneSize.y;
            player.transform.position += new Vector3(halfX * .7f, halfY * .7f);
            yield return new WaitForSeconds(.4f);
            Check("Motion inside rectangle leaves camera still " + aspect, CameraDistance(origin) < .001f);
            player.transform.position += Vector3.right * halfX;
            yield return null;
            Check("Crossing dead-zone edge follows without snapping " + aspect, CameraDistance(origin) > 0 && CameraDistance(origin) < halfX * .7f);
            yield return new WaitForSeconds(1.7f);
            float x = camera.WorldToViewportPoint(player.transform.position).x;
            Check("Player settles at rectangle edge, not center " + aspect, x >= .615f && x < .635f);
            var settled = camera.transform.position;
            player.transform.position -= Vector3.right * halfX;
            yield return new WaitForSeconds(.3f);
            Check("Turning within rectangle does not recenter " + aspect, CameraDistance(settled) < .003f);
            player.transform.position += Vector3.up * (halfY * 2);
            yield return new WaitForSeconds(.8f);
            Check("Vertical edge starts upward follow " + aspect, camera.transform.position.y > settled.y + .1f);
        }
        camera.aspect = 16f / 9; Place(places[0]); yield return null;
        var pausedPosition = camera.transform.position; float pausedSize = camera.orthographicSize;
        GameSession.Instance.SetPaused(true); player.transform.position = mazePoint;
        yield return new WaitForSecondsRealtime(.3f);
        Check("Pause freezes tracking and zoom", CameraDistance(pausedPosition) < .001f && camera.orthographicSize == pausedSize);
        GameSession.Instance.SetPaused(false);
        follow.SetTarget(player.transform); yield return null;
        Check("Checkpoint alignment snaps directly to correct maze scale", zoom.InLabyrinth && camera.orthographicSize == zoom.minimumSize && Mathf.Abs(camera.WorldToViewportPoint(player.transform.position).x - .5f) < .001f);
        Place(new Vector2(285, -74)); yield return null;
        float fastEnd = Time.time + .7f; bool visible = true;
        while (Time.time < fastEnd) {
            player.transform.position += Vector3.right * (26 * Time.deltaTime);
            yield return null;
            float x = camera.WorldToViewportPoint(player.transform.position).x;
            visible &= x > .08f && x < .92f;
        }
        Check("Maze close view keeps full-speed Dash travel visible", visible);
        Place(places[0]); yield return null;
        var rb = player.GetComponent<Rigidbody2D>(); rb.simulated = true; rb.gravityScale = 3;
        player.enabled = true; player.SetTestInput(0, 0); yield return new WaitForSeconds(.4f);
        follow.SettleAtTarget(); var walkingOrigin = camera.transform.position; var walkingStart = player.transform.position;
        player.SetTestInput(1, 0); yield return new WaitForSeconds(.18f);
        Check("Real walking moves player inside stationary dead zone", player.transform.position.x > walkingStart.x + .5f && CameraDistance(walkingOrigin) < .03f);
        yield return new WaitForSeconds(1);
        Check("Sustained real walking starts smooth tracking", camera.transform.position.x > walkingOrigin.x + 1);
        player.SetTestInput(0, 0); player.enabled = false; rb.simulated = false;
        Place(places[0]); yield return null;
        var before = camera.transform.position;
        player.GetComponent<HealthComponent>().Invincible = false;
        player.GetComponent<HealthComponent>().ApplyDamage(1);
        yield return new WaitForSeconds(.05f);
        Check("Accepted damage still produces camera shake", CameraDistance(before) > .001f);
        yield return new WaitForSeconds(.5f);
        Check("Damage shake leaves no permanent framing drift", CameraDistance(before) < .001f);
        Place(new Vector2(59, -59.5f)); yield return null; BableVerification.Capture("revision57/abaddon-room-wide.png");
        Finish();
    }
    static void Finish() { SessionState.SetBool(Flag, false); EditorApplication.update -= Watch; EditorApplication.Exit(failures.Count == 0 ? 0 : 1); }
}
