using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CoreRunner
{
    // Opt-in verification of the actual Windows player. Normal launches never create this object.
    public sealed class VerificationRunner : MonoBehaviour
    {
        [Serializable] public sealed class Check { public string name; public bool passed; public string detail; }
        [Serializable] public sealed class Report { public string unityVersion; public bool passed; public List<Check> checks = new List<Check>(); }
        Report report = new Report();
        string output;
        bool finished;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-coreRunnerVerify");
            if (index < 0 || index + 1 >= args.Length) return;
            var runner = new GameObject("Verification Runner").AddComponent<VerificationRunner>();
            DontDestroyOnLoad(runner.gameObject);
            runner.output = args[index + 1]; Directory.CreateDirectory(runner.output);
            runner.report.unityVersion = Application.unityVersion;
            Application.logMessageReceived += runner.OnLog;
            runner.StartCoroutine(runner.Run());
        }
        void OnLog(string text, string trace, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error)
                CheckResult("Runtime error", false, text + "\n" + trace);
        }
        void CheckResult(string name, bool ok, string detail = "")
        { report.checks.Add(new Check { name = name, passed = ok, detail = detail }); Debug.Log("VERIFY " + name + ": " + (ok ? "PASS" : "FAIL") + " " + detail); }
        IEnumerator Capture(string name)
        {
            // Hidden Windows players skip presentation. Render the actual scene/UI explicitly
            // so verification screenshots do not depend on whether the window is foreground.
            yield return null;
            var camera = Camera.main;
            var canvas = Array.Find(FindObjectsByType<Canvas>(FindObjectsSortMode.None), c => c.renderMode == RenderMode.ScreenSpaceOverlay);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var previousMode = canvas.renderMode;
            var previousCamera = canvas.worldCamera;
            float previousDistance = canvas.planeDistance;
            var rt = new RenderTexture(1600, 900, 24);
            var texture = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = rt;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera; canvas.planeDistance = 1;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = rt;
                texture.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); texture.Apply();
                File.WriteAllBytes(Path.Combine(output, name + ".png"), texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                canvas.renderMode = previousMode; canvas.worldCamera = previousCamera; canvas.planeDistance = previousDistance;
                RenderTexture.active = previousActive;
                rt.Release(); Destroy(rt); Destroy(texture);
            }
        }
        static void Place(PlayerController player, Vector3 position)
        {
            player.Body.position = position;
            player.Body.linearVelocity = Vector3.zero;
            player.Body.angularVelocity = Vector3.zero;
            Physics.SyncTransforms();
        }
        IEnumerator Run()
        {
            yield return new WaitForSecondsRealtime(.7f);
            CheckResult("Menu is initial scene", SceneManager.GetActiveScene().name == "MainMenu");
            yield return Capture("01-menu");
            var menu = FindFirstObjectByType<MenuController>();
            CheckResult("Menu start button exists", menu && menu.startButton);
            if (!menu) { Finish(); yield break; }
            menu.startButton.onClick.Invoke();
            float deadline = Time.realtimeSinceStartup + 20;
            while (SceneManager.GetActiveScene().name != "Game" && Time.realtimeSinceStartup < deadline) yield return null;
            CheckResult("Start button transitions to Game", SceneManager.GetActiveScene().name == "Game");
            yield return new WaitForSeconds(.7f);
            var g = GameManager.Instance;
            if (!g) { CheckResult("Game manager exists", false); Finish(); yield break; }
            var p = g.player;
            CheckResult("Eight cores instantiated at runtime", FindObjectsByType<CorePickup>(FindObjectsSortMode.None).Length == 8);
            CheckResult("Sphere rests on physical deck", p.Grounded && p.transform.position.y > .45f && p.transform.position.y < .8f, p.transform.position.ToString());
            CheckResult("Audio clips are assigned", g.collectSound && g.boostSound && g.fallSound && g.checkpointSound && g.winSound);
            yield return Capture("02-arrival");
            CheckResult("Exit is locked before collecting", !g.TryFinish() && !g.Won);

            // Ground contact + real impulse + flight across both gaps, without moving during the flight.
            Place(p, new Vector3(0, .57f, 18));
            yield return new WaitForSeconds(.1f);
            bool boosted = p.TryBoost(Vector3.forward);
            CheckResult("Grounded boost applies", boosted);
            CheckResult("Boost cannot be repeated during cooldown", !p.TryBoost(Vector3.forward));
            yield return new WaitForSeconds(.75f);
            CheckResult("Impulse traverses first gap", p.transform.position.z > 23.5f && p.transform.position.y > .4f, p.transform.position.ToString());
            yield return new WaitForSeconds(.5f);
            CheckResult("Checkpoint activates through trigger", p.Checkpoint.z > 24);
            yield return new WaitForSeconds(.7f);
            CheckResult("Boost recharges", p.BoostReady >= 1);

            Place(p, new Vector3(0, .57f, 41));
            yield return new WaitForSeconds(.12f);
            CheckResult("Second gap boost applies", p.TryBoost(Vector3.forward));
            yield return new WaitForSeconds(.9f);
            CheckResult("Impulse traverses second gap", p.transform.position.z > 47 && p.transform.position.y > .4f, p.transform.position.ToString());
            yield return new WaitForSeconds(.45f);
            CheckResult("Second checkpoint activates", p.Checkpoint.z > 48);
            int falls = g.Falls;
            Place(p, new Vector3(0, -9, 51));
            yield return new WaitForSeconds(.75f);
            CheckResult("Falling respawns once", g.Falls == falls + 1 && !p.IsRespawning && Vector3.Distance(p.transform.position, p.Checkpoint) < .3f);

            var hazards = FindObjectsByType<MovingHazard>(FindObjectsSortMode.None);
            var hz = hazards[0];
            Vector3 old = hz.transform.position;
            yield return new WaitForSeconds(.3f);
            CheckResult("Scanner physically moves", Vector3.Distance(old, hz.transform.position) > .05f);
            falls = g.Falls;
            Place(p, hz.transform.position + Vector3.back * .65f);
            yield return new WaitForSeconds(.85f);
            CheckResult("Scanner collision triggers respawn", g.Falls == falls + 1 && !p.IsRespawning);

            foreach (var core in FindObjectsByType<CorePickup>(FindObjectsSortMode.None))
            {
                if (!core) continue;
                Place(p, core.transform.position);
                yield return new WaitForSeconds(.18f);
            }
            yield return null;
            CheckResult("Trigger collection recovers all eight cores", g.Collected == 8);
            CheckResult("HUD updates collection count", g.hud.cores.text == "CORES  8 / 8");
            falls = g.Falls;
            Place(p, new Vector3(0, -9, 60));
            yield return new WaitForSeconds(.75f);
            CheckResult("Respawning retains collected cores", g.Collected == 8 && g.Falls == falls + 1);
            float elapsed = g.Elapsed;
            g.TogglePause();
            yield return new WaitForSecondsRealtime(.3f);
            CheckResult("Pause freezes timer and reveals panel", g.Paused && g.hud.pausePanel.activeSelf && Mathf.Abs(g.Elapsed - elapsed) < .02f);
            yield return Capture("03-pause");
            g.TogglePause();
            CheckResult("Resume restores time", g.IsPlaying && Time.timeScale == 1);
            Place(p, new Vector3(0, .6f, 77));
            yield return new WaitForSeconds(.25f);
            CheckResult("Exit trigger wins after collection", g.Won && g.hud.victoryPanel.activeSelf);
            CheckResult("Victory stops player", p.Body.isKinematic);
            yield return Capture("04-victory");
            Button quit = null;
            foreach (var b in FindObjectsByType<Button>(FindObjectsSortMode.None)) if (b.name == "QUIT") quit = b;
            CheckResult("Quit button is accessible over victory", quit && quit.gameObject.activeInHierarchy && quit.transform.GetSiblingIndex() == quit.transform.parent.childCount - 1);
            g.Restart();
            yield return new WaitForSeconds(.6f);
            g = GameManager.Instance;
            CheckResult("Restart resets game state", g && !g.Won && g.Collected == 0 && g.Falls == 0);
            g.Menu();
            yield return new WaitForSeconds(.6f);
            CheckResult("Return to menu restores initial scene", SceneManager.GetActiveScene().name == "MainMenu" && Time.timeScale == 1);
            Finish();
            FindFirstObjectByType<MenuController>().Quit();
        }
        void Finish()
        {
            if (finished) return; finished = true;
            report.passed = report.checks.TrueForAll(c => c.passed);
            File.WriteAllText(Path.Combine(output, "results.json"), JsonUtility.ToJson(report, true));
            Debug.Log("CORE_RUNNER_VERIFICATION_COMPLETE " + report.passed);
            if (!report.passed) Application.Quit(1);
        }
    }
}
