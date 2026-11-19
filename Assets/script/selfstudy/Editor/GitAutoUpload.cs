using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;

// Uploads your work to GitHub automatically.
//
// - Auto mode (Tools > Git > Auto Sync With GitHub, ON by default):
//   every time you press Ctrl+S, it waits 30 seconds (so many saves in a row
//   become one upload), then: commit -> get teammates' changes -> push.
// - Auto sync (same menu switch): when you open the project, and then every
//   3 minutes, it gets your teammates' changes by itself (and uploads yours).
//   It skips the check while you have UNSAVED changes, so nothing gets lost;
//   it syncs again after you save.
// - Manual: Tools > Git > Save and Upload Now  (shortcut Ctrl+Alt+S)
//
// Messages appear in the Console window, starting with [Git].
[InitializeOnLoad]
static class GitAutoUpload
{
    const string MenuAuto = "Tools/Git/Auto Sync With GitHub";
    const double WaitAfterSave = 30;  // seconds
    const double CheckEvery = 180;    // seconds (3 minutes)

    static double uploadAt = -1;
    static double nextCheck;
    static volatile bool busy;
    static volatile bool needRefresh;

    static string PrefKey => "GitAutoUpload.Enabled." + UnityEngine.Application.dataPath;

    static bool AutoEnabled
    {
        get => EditorPrefs.GetBool(PrefKey, true);
        set => EditorPrefs.SetBool(PrefKey, value);
    }

    static GitAutoUpload()
    {
        EditorSceneManager.sceneSaved += _ => OnSaved();
        EditorApplication.update += Tick;

        // Just opened the project? Check GitHub soon (10 s, so Unity can finish loading).
        // (This code also runs after every script change, so remember we already did it.)
        if (!SessionState.GetBool("GitAutoUpload.Opened", false))
        {
            SessionState.SetBool("GitAutoUpload.Opened", true);
            nextCheck = EditorApplication.timeSinceStartup + 10;
        }
        else
        {
            nextCheck = EditorApplication.timeSinceStartup + CheckEvery;
        }
    }

    // Any open scene with changes that are not saved yet?
    static bool HasUnsavedScene()
    {
        for (int i = 0; i < EditorSceneManager.sceneCount; i++)
        {
            if (EditorSceneManager.GetSceneAt(i).isDirty) return true;
        }
        return false;
    }

    // ---------- Menu ----------

    [MenuItem(MenuAuto)]
    static void ToggleAuto()
    {
        AutoEnabled = !AutoEnabled;
        UnityEngine.Debug.Log("[Git] Auto upload is now " + (AutoEnabled ? "ON" : "OFF"));
    }

    [MenuItem(MenuAuto, true)]
    static bool ToggleAutoCheck()
    {
        Menu.SetChecked(MenuAuto, AutoEnabled);
        return true;
    }

    [MenuItem("Tools/Git/Save and Upload Now %&s")]
    static void UploadNow()
    {
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        uploadAt = -1;
        StartUpload("Update");
    }

    // ---------- Auto mode ----------

    static void OnSaved()
    {
        if (!AutoEnabled) return;
        // Wait a bit: if you save again, the timer starts over
        uploadAt = EditorApplication.timeSinceStartup + WaitAfterSave;
    }

    static void Tick()
    {
        // Teammates' changes arrived: let Unity load them
        if (needRefresh)
        {
            needRefresh = false;
            AssetDatabase.Refresh();
        }

        if (uploadAt > 0 &&
            EditorApplication.timeSinceStartup >= uploadAt &&
            !busy &&
            !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            uploadAt = -1;
            AssetDatabase.SaveAssets();
            StartUpload("Auto save");
        }

        // Every few minutes: get teammates' changes
        if (AutoEnabled &&
            EditorApplication.timeSinceStartup >= nextCheck &&
            !busy &&
            uploadAt < 0 &&
            !EditorApplication.isPlayingOrWillChangePlaymode &&
            !EditorApplication.isCompiling)
        {
            nextCheck = EditorApplication.timeSinceStartup + CheckEvery;

            // Unsaved work? Don't touch anything now - it syncs after you save.
            if (!HasUnsavedScene())
            {
                StartUpload("Auto sync", quiet: true);
            }
        }
    }

    // ---------- Upload ----------

    // quiet = don't write "nothing new" messages (for the every-3-minutes check)
    static void StartUpload(string reason, bool quiet = false)
    {
        if (busy)
        {
            if (!quiet) UnityEngine.Debug.Log("[Git] Already uploading, please wait...");
            return;
        }

        busy = true;
        string root = Path.GetDirectoryName(UnityEngine.Application.dataPath);
        string message = reason + " " + DateTime.Now.ToString("yyyy-MM-dd HH:mm");

        if (!quiet) UnityEngine.Debug.Log("[Git] Uploading to GitHub...");

        // Run in the background so Unity doesn't freeze
        Task.Run(() =>
        {
            try { Upload(root, message, quiet); }
            catch (Exception e) { UnityEngine.Debug.LogError("[Git] " + e.Message); }
            finally { busy = false; }
        });
    }

    static void Upload(string root, string message, bool quiet)
    {
        // 1. Commit everything that changed
        Git(root, "add -A", out _);
        bool hasChanges = Git(root, "diff --cached --quiet", out _) != 0;

        if (hasChanges && Git(root, "commit -m \"" + message + "\"", out string commitOut) != 0)
        {
            UnityEngine.Debug.LogError("[Git] Commit failed:\n" + commitOut);
            return;
        }

        // 2. Get teammates' changes and put ours on top
        Git(root, "rev-parse HEAD", out string before);

        if (Git(root, "pull --rebase --autostash", out string pullOut) != 0)
        {
            Git(root, "rebase --abort", out _);
            UnityEngine.Debug.LogError(
                "[Git] You and a teammate changed the same thing, so it couldn't be combined " +
                "automatically. Your work is SAFE (saved on this computer). " +
                "Open GitHub Desktop and click 'Pull origin' to fix it.\n" + pullOut);
            return;
        }

        Git(root, "rev-parse HEAD", out string after);
        bool gotTeammateChanges = before.Trim() != after.Trim();
        if (gotTeammateChanges) needRefresh = true;

        // 3. Upload
        Git(root, "status -sb", out string status);
        bool somethingToPush = status.Contains("ahead");

        if (!somethingToPush)
        {
            if (gotTeammateChanges)
                UnityEngine.Debug.Log("[Git] Got your teammates' changes ✓");
            else if (!quiet)
                UnityEngine.Debug.Log("[Git] Nothing new to upload.");
            return;
        }

        if (Git(root, "push", out string pushOut) != 0)
        {
            UnityEngine.Debug.LogError(
                "[Git] Upload failed. Your work is saved on this computer; " +
                "try again or use GitHub Desktop.\n" + pushOut);
            return;
        }

        UnityEngine.Debug.Log("[Git] Uploaded to GitHub ✓" +
            (gotTeammateChanges ? " (also got your teammates' changes)" : ""));
    }

    // Runs one git command in the project folder
    static int Git(string root, string args, out string output)
    {
        var psi = new ProcessStartInfo("git", args)
        {
            WorkingDirectory = root,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        psi.EnvironmentVariables["GIT_TERMINAL_PROMPT"] = "0";

        using (Process p = Process.Start(psi))
        {
            Task<string> o = p.StandardOutput.ReadToEndAsync();
            Task<string> e = p.StandardError.ReadToEndAsync();
            p.WaitForExit();
            output = o.Result + e.Result;
            return p.ExitCode;
        }
    }
}
