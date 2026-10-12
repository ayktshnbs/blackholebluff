using System.IO;
using System.Linq;
using BlackHoleBluff.Dev;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BlackHoleBluff.EditorTools
{
    /// <summary>
    /// One-time project setup. Menu: Black Hole Bluff / Setup.
    /// Idempotent — running a step twice prompts before overwriting.
    /// </summary>
    public static class ProjectSetup
    {
        const string SandboxScenePath = "Assets/Scenes/DevSandbox.unity";
        const string DuelScenePath = "Assets/Scenes/Duel.unity";

        [MenuItem("Black Hole Bluff/Setup/1. Create Dev Sandbox Scene", false, 1)]
        public static void CreateDevSandboxScene()
        {
            if (File.Exists(SandboxScenePath) &&
                !EditorUtility.DisplayDialog(
                    "Scene exists",
                    $"{SandboxScenePath} already exists.\n\nOverwrite it?",
                    "Overwrite", "Cancel"))
                return;

            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var harness = new GameObject("DevHarness");
            harness.AddComponent<DrawRoundDevHarness>();

            if (!EditorSceneManager.SaveScene(scene, SandboxScenePath))
            {
                Debug.LogError("[ProjectSetup] Failed to save the sandbox scene.");
                return;
            }

            AddSceneToBuildList(SandboxScenePath);

            Debug.Log(
                "[ProjectSetup] Created Assets/Scenes/DevSandbox.unity with a DrawRoundDevHarness " +
                "and registered it as build scene 0. Press Play to log a hand of five-card draw.");
        }

        [MenuItem("Black Hole Bluff/Setup/2. Create Duel Scene", false, 2)]
        public static void CreateDuelScene()
        {
            if (File.Exists(DuelScenePath) &&
                !EditorUtility.DisplayDialog(
                    "Scene exists",
                    $"{DuelScenePath} already exists.\n\nOverwrite it?",
                    "Overwrite", "Cancel"))
                return;

            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var duel = new GameObject("Duel");
            duel.AddComponent<DuelDevUI>();

            if (!EditorSceneManager.SaveScene(scene, DuelScenePath))
            {
                Debug.LogError("[ProjectSetup] Failed to save the duel scene.");
                return;
            }

            // The playable duel is what a build should launch into, so it goes first.
            AddSceneToBuildList(DuelScenePath, first: true);
            AddSceneToBuildList(SandboxScenePath, first: false);

            Debug.Log(
                "[ProjectSetup] Created Assets/Scenes/Duel.unity with a DuelDevUI and made it " +
                "build scene 0. Press Play for a complete playable duel (placeholder IMGUI).");
        }

        /// <summary>
        /// Drops build-list entries whose scene file does not exist. The URP template's
        /// EditorBuildSettings referenced a SampleScene that this project never copied,
        /// which left a dangling entry Unity tolerates but should not have to.
        /// </summary>
        [MenuItem("Black Hole Bluff/Setup/Prune Missing Scenes From Build List", false, 3)]
        public static void PruneMissingScenesFromBuildList()
        {
            var kept = EditorBuildSettings.scenes.Where(s => File.Exists(s.path)).ToArray();
            var removed = EditorBuildSettings.scenes.Length - kept.Length;

            if (removed == 0)
            {
                Debug.Log("[ProjectSetup] Build list is clean; every scene exists.");
                return;
            }

            var gone = EditorBuildSettings.scenes
                .Where(s => !File.Exists(s.path))
                .Select(s => s.path);

            EditorBuildSettings.scenes = kept;

            Debug.Log($"[ProjectSetup] Removed {removed} missing scene(s) from the build list: " +
                      string.Join(", ", gone));
        }

        [MenuItem("Black Hole Bluff/Setup/Verify Project Settings", false, 20)]
        public static void VerifyProjectSettings()
        {
            var ok = new System.Collections.Generic.List<string>();
            var problems = new System.Collections.Generic.List<string>();

            void Check(bool passed, string label, string fix)
            {
                if (passed) ok.Add(label);
                else problems.Add($"{label} — {fix}");
            }

            Check(PlayerSettings.colorSpace == ColorSpace.Linear,
                "Color Space is Linear",
                "set Player > Other Settings > Color Space to Linear");

            Check(EditorSettings.serializationMode == SerializationMode.ForceText,
                "Asset Serialization is Force Text",
                "set Editor > Asset Serialization to Force Text, or git conflicts become unresolvable");

            Check(EditorSettings.enterPlayModeOptionsEnabled,
                "Fast play-mode entry is on",
                "enable Editor > Enter Play Mode Settings");

            Check(PlayerSettings.productName == "Black Hole Bluff",
                "Product name is set",
                "set Player > Product Name");

            Check(File.Exists(SandboxScenePath),
                "DevSandbox.unity exists",
                "run Black Hole Bluff > Setup > 1. Create Dev Sandbox Scene");

            Check(EditorBuildSettings.scenes.Any(s => s.enabled),
                "At least one scene is in the build list",
                "add a scene to File > Build Profiles > Scene List, or a Windows build will fail");

            var report = "[ProjectSetup] Verification\n\nOK:\n  " + string.Join("\n  ", ok);

            if (problems.Count > 0)
            {
                report += "\n\nNeeds attention:\n  " + string.Join("\n  ", problems);
                Debug.LogWarning(report);
            }
            else
            {
                Debug.Log(report + "\n\nAll project settings are correct.");
            }
        }

        static void AddSceneToBuildList(string scenePath, bool first = true)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == scenePath)) return;

            var entry = new EditorBuildSettingsScene(scenePath, true);
            if (first) scenes.Insert(0, entry); else scenes.Add(entry);
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
