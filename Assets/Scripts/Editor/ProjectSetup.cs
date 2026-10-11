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

        static void AddSceneToBuildList(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == scenePath)) return;

            scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
