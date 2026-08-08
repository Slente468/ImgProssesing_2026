#if UNITY_EDITOR && UNITY_6000_0_OR_NEWER
using System;
using UnityEditor;
using UnityEngine;

namespace OpenCVForUnity.Editor
{
    /// <summary>
    /// Exclusively enables the correct Trial managed DLL for Sentis integration (equivalent to OPENCV_SENTIS_AVAILABLE in asmdef).
    /// </summary>
    public static class TrialVersionDllImportSettingsSync
    {
        private const string TrialDllAssetPath = "Assets/OpenCVForUnity/org/EnoxSoftware.OpenCVForUnityTrialVersion.dll";

        private const string TrialSentisDllAssetPath = "Assets/OpenCVForUnity/org/EnoxSoftware.OpenCVForUnityTrialVersionSentis.dll";

        private const string MainAsmdefSuffix = "/EnoxSoftware.OpenCVForUnity.asmdef";

        private const string EditorAsmdefSuffix = "/Editor/EnoxSoftware.OpenCVForUnity.Editor.asmdef";

        private const string ExampleAsmdefSuffix = "/Examples/EnoxSoftware.OpenCVForUnityExample.asmdef";

        private const string SessionKeyLastWantSentisDll =
            "OpenCVForUnity.TrialDllLastWantSentisDll";

        private static bool s_suppressPostprocess;

        /// <summary>
        /// Build targets that had been excluded via Any Platform (legacy). Cleared when switching to Editor-only with Any Platform off.
        /// </summary>
        private static readonly BuildTarget[] LegacyAnyPlatformExcludeTargets =
        {
            BuildTarget.Android,
            BuildTarget.iOS,
#if (UNITY_2022_3_OR_NEWER && !(UNITY_2023_1_OR_NEWER) && !(UNITY_2022_3_0 || UNITY_2022_3_1 || UNITY_2022_3_2 || UNITY_2022_3_3 || UNITY_2022_3_4 || UNITY_2022_3_5 || UNITY_2022_3_6 || UNITY_2022_3_7 || UNITY_2022_3_8 || UNITY_2022_3_9 || UNITY_2022_3_10 || UNITY_2022_3_11 || UNITY_2022_3_12 || UNITY_2022_3_13 || UNITY_2022_3_14 || UNITY_2022_3_15 || UNITY_2022_3_16 || UNITY_2022_3_17)) || UNITY_6000_0_OR_NEWER
            BuildTarget.VisionOS,
#endif
            BuildTarget.StandaloneWindows,
            BuildTarget.StandaloneWindows64,
            BuildTarget.StandaloneOSX,
            BuildTarget.StandaloneLinux64,
            BuildTarget.WSAPlayer,
            BuildTarget.WebGL,
        };

        /// <summary>
        /// Called from <see cref="TrialVersionWindowStartup"/> static constructor. Schedules initial <c>delayCall</c> sync and relies on the asset postprocessor.
        /// </summary>
        public static void Register()
        {
            EditorApplication.delayCall -= OnInitialDelayCall;
            EditorApplication.delayCall += OnInitialDelayCall;
        }

        private static void OnInitialDelayCall()
        {
            EditorApplication.delayCall -= OnInitialDelayCall;
            ApplyIfNeeded("initialDelayCall");
        }

        internal static bool IsPostprocessSuppressed()
        {
            return s_suppressPostprocess;
        }

        internal static void ApplyIfNeeded(string reason)
        {
            if (!TryGetOpenCVForUnityFolderAssetPath(out _))
            {
                return;
            }

            bool wantSentisDll = OpenCVForUnityMenuItem.ValidateSentisAsmdefIntegration();
            int wantCode = wantSentisDll ? 1 : 0;

            if (SessionState.GetInt(SessionKeyLastWantSentisDll, -1) == wantCode
                && ImportersAlreadyMatch(wantSentisDll))
            {
                return;
            }

            if (ImportersAlreadyMatch(wantSentisDll))
            {
                SessionState.SetInt(SessionKeyLastWantSentisDll, wantCode);
                return;
            }

            var trialImporter = PluginImporter.GetAtPath(TrialDllAssetPath) as PluginImporter;
            var sentisImporter = PluginImporter.GetAtPath(TrialSentisDllAssetPath) as PluginImporter;
            if (trialImporter == null || sentisImporter == null)
            {
                Debug.LogWarning(
                    "OpenCV for Unity Trial: Could not get PluginImporter for Trial DLL. Verify asset paths.");
                return;
            }

            try
            {
                s_suppressPostprocess = true;

                if (wantSentisDll)
                {
                    DisablePluginFully(trialImporter);
                    EnablePluginEditorOnly(sentisImporter);
                }
                else
                {
                    DisablePluginFully(sentisImporter);
                    EnablePluginEditorOnly(trialImporter);
                }

                trialImporter.SaveAndReimport();
                sentisImporter.SaveAndReimport();
                SessionState.SetInt(SessionKeyLastWantSentisDll, wantCode);

                Debug.Log(
                    $"OpenCV for Unity Trial: Trial DLLs synced with Sentis integration {(wantSentisDll ? "ON" : "OFF")} ({reason}).");
            }
            finally
            {
                s_suppressPostprocess = false;
            }
        }

        private static bool ImportersAlreadyMatch(bool wantSentisDll)
        {
            var trialImporter = PluginImporter.GetAtPath(TrialDllAssetPath) as PluginImporter;
            var sentisImporter = PluginImporter.GetAtPath(TrialSentisDllAssetPath) as PluginImporter;
            if (trialImporter == null || sentisImporter == null)
            {
                return false;
            }

            if (wantSentisDll)
            {
                return IsEditorOnlyWithoutAnyPlatform(sentisImporter)
                    && IsFullyDisabled(trialImporter);
            }

            return IsEditorOnlyWithoutAnyPlatform(trialImporter)
                && IsFullyDisabled(sentisImporter);
        }

        /// <summary>
        /// Returns whether Any Platform is off, Editor is on, and every other include platform is off.
        /// </summary>
        private static bool IsEditorOnlyWithoutAnyPlatform(PluginImporter importer)
        {
            if (importer.GetCompatibleWithAnyPlatform())
            {
                return false;
            }

            if (!importer.GetCompatibleWithEditor())
            {
                return false;
            }

            if (importer.GetCompatibleWithPlatform(BuildTarget.Android)
                || importer.GetCompatibleWithPlatform(BuildTarget.iOS)
#if (UNITY_2022_3_OR_NEWER && !(UNITY_2023_1_OR_NEWER) && !(UNITY_2022_3_0 || UNITY_2022_3_1 || UNITY_2022_3_2 || UNITY_2022_3_3 || UNITY_2022_3_4 || UNITY_2022_3_5 || UNITY_2022_3_6 || UNITY_2022_3_7 || UNITY_2022_3_8 || UNITY_2022_3_9 || UNITY_2022_3_10 || UNITY_2022_3_11 || UNITY_2022_3_12 || UNITY_2022_3_13 || UNITY_2022_3_14 || UNITY_2022_3_15 || UNITY_2022_3_16 || UNITY_2022_3_17)) || UNITY_6000_0_OR_NEWER
                || importer.GetCompatibleWithPlatform(BuildTarget.VisionOS)
#endif
                || importer.GetCompatibleWithPlatform(BuildTarget.StandaloneWindows)
                || importer.GetCompatibleWithPlatform(BuildTarget.StandaloneWindows64)
                || importer.GetCompatibleWithPlatform(BuildTarget.StandaloneOSX)
                || importer.GetCompatibleWithPlatform(BuildTarget.StandaloneLinux64)
                || importer.GetCompatibleWithPlatform(BuildTarget.WSAPlayer)
                || importer.GetCompatibleWithPlatform(BuildTarget.WebGL))
            {
                return false;
            }

            return true;
        }

        private static bool IsFullyDisabled(PluginImporter importer)
        {
            if (importer.GetCompatibleWithAnyPlatform())
            {
                return false;
            }

            if (importer.GetCompatibleWithEditor())
            {
                return false;
            }

            if (importer.GetCompatibleWithPlatform(BuildTarget.Android)
                || importer.GetCompatibleWithPlatform(BuildTarget.iOS)
#if (UNITY_2022_3_OR_NEWER && !(UNITY_2023_1_OR_NEWER) && !(UNITY_2022_3_0 || UNITY_2022_3_1 || UNITY_2022_3_2 || UNITY_2022_3_3 || UNITY_2022_3_4 || UNITY_2022_3_5 || UNITY_2022_3_6 || UNITY_2022_3_7 || UNITY_2022_3_8 || UNITY_2022_3_9 || UNITY_2022_3_10 || UNITY_2022_3_11 || UNITY_2022_3_12 || UNITY_2022_3_13 || UNITY_2022_3_14 || UNITY_2022_3_15 || UNITY_2022_3_16 || UNITY_2022_3_17)) || UNITY_6000_0_OR_NEWER
                || importer.GetCompatibleWithPlatform(BuildTarget.VisionOS)
#endif
                || importer.GetCompatibleWithPlatform(BuildTarget.StandaloneWindows)
                || importer.GetCompatibleWithPlatform(BuildTarget.StandaloneWindows64)
                || importer.GetCompatibleWithPlatform(BuildTarget.StandaloneOSX)
                || importer.GetCompatibleWithPlatform(BuildTarget.StandaloneLinux64)
                || importer.GetCompatibleWithPlatform(BuildTarget.WSAPlayer)
                || importer.GetCompatibleWithPlatform(BuildTarget.WebGL))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Enables the DLL for the Editor only: Any Platform off, Include Platforms Editor only on.
        /// </summary>
        private static void EnablePluginEditorOnly(PluginImporter pluginImporter)
        {
            ClearLegacyAnyPlatformExcludes(pluginImporter);

            pluginImporter.SetCompatibleWithAnyPlatform(false);
            pluginImporter.SetCompatibleWithEditor(true);
            pluginImporter.SetCompatibleWithPlatform(BuildTarget.Android, false);
            pluginImporter.SetCompatibleWithPlatform(BuildTarget.iOS, false);
#if (UNITY_2022_3_OR_NEWER && !(UNITY_2023_1_OR_NEWER) && !(UNITY_2022_3_0 || UNITY_2022_3_1 || UNITY_2022_3_2 || UNITY_2022_3_3 || UNITY_2022_3_4 || UNITY_2022_3_5 || UNITY_2022_3_6 || UNITY_2022_3_7 || UNITY_2022_3_8 || UNITY_2022_3_9 || UNITY_2022_3_10 || UNITY_2022_3_11 || UNITY_2022_3_12 || UNITY_2022_3_13 || UNITY_2022_3_14 || UNITY_2022_3_15 || UNITY_2022_3_16 || UNITY_2022_3_17)) || UNITY_6000_0_OR_NEWER
            pluginImporter.SetCompatibleWithPlatform(BuildTarget.VisionOS, false);
#endif
            pluginImporter.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows, false);
            pluginImporter.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows64, false);
            pluginImporter.SetCompatibleWithPlatform(BuildTarget.StandaloneOSX, false);
            pluginImporter.SetCompatibleWithPlatform(BuildTarget.StandaloneLinux64, false);
            pluginImporter.SetCompatibleWithPlatform(BuildTarget.WSAPlayer, false);
            pluginImporter.SetCompatibleWithPlatform(BuildTarget.WebGL, false);
            pluginImporter.isPreloaded = false;
        }

        private static void ClearLegacyAnyPlatformExcludes(PluginImporter pluginImporter)
        {
            if (!pluginImporter.GetCompatibleWithAnyPlatform())
            {
                return;
            }

            pluginImporter.SetExcludeEditorFromAnyPlatform(false);
            foreach (BuildTarget t in LegacyAnyPlatformExcludeTargets)
            {
                pluginImporter.SetExcludeFromAnyPlatform(t, false);
            }
        }

        private static void DisablePluginFully(PluginImporter pluginImporter)
        {
            ClearLegacyAnyPlatformExcludes(pluginImporter);

            pluginImporter.SetCompatibleWithAnyPlatform(false);
            pluginImporter.SetCompatibleWithEditor(false);
            pluginImporter.SetCompatibleWithPlatform(BuildTarget.Android, false);
            pluginImporter.SetCompatibleWithPlatform(BuildTarget.iOS, false);
#if (UNITY_2022_3_OR_NEWER && !(UNITY_2023_1_OR_NEWER) && !(UNITY_2022_3_0 || UNITY_2022_3_1 || UNITY_2022_3_2 || UNITY_2022_3_3 || UNITY_2022_3_4 || UNITY_2022_3_5 || UNITY_2022_3_6 || UNITY_2022_3_7 || UNITY_2022_3_8 || UNITY_2022_3_9 || UNITY_2022_3_10 || UNITY_2022_3_11 || UNITY_2022_3_12 || UNITY_2022_3_13 || UNITY_2022_3_14 || UNITY_2022_3_15 || UNITY_2022_3_16 || UNITY_2022_3_17)) || UNITY_6000_0_OR_NEWER
            pluginImporter.SetCompatibleWithPlatform(BuildTarget.VisionOS, false);
#endif
            pluginImporter.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows, false);
            pluginImporter.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows64, false);
            pluginImporter.SetCompatibleWithPlatform(BuildTarget.StandaloneOSX, false);
            pluginImporter.SetCompatibleWithPlatform(BuildTarget.StandaloneLinux64, false);
            pluginImporter.SetCompatibleWithPlatform(BuildTarget.WSAPlayer, false);
            pluginImporter.SetCompatibleWithPlatform(BuildTarget.WebGL, false);
            pluginImporter.isPreloaded = false;
        }

        internal static bool TryGetOpenCVForUnityFolderAssetPath(out string folderPath)
        {
            folderPath = null;
            string[] guids = AssetDatabase.FindAssets("OpenCVForUnityMenuItem");
            if (guids.Length == 0)
            {
                return false;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            int idx = path.LastIndexOf("/Editor/OpenCVForUnityMenuItem.cs", StringComparison.Ordinal);
            if (idx < 0)
            {
                return false;
            }

            folderPath = path.Substring(0, idx);
            return true;
        }

        internal static bool PathsTouchSentisRelatedAsmdefs(string[] paths, string openCvRoot)
        {
            if (paths == null || paths.Length == 0)
            {
                return false;
            }

            string main = openCvRoot + MainAsmdefSuffix;
            string editor = openCvRoot + EditorAsmdefSuffix;
            string example = openCvRoot + ExampleAsmdefSuffix;
            string mainNorm = main.Replace('\\', '/');
            string editorNorm = editor.Replace('\\', '/');
            string exampleNorm = example.Replace('\\', '/');

            foreach (string p in paths)
            {
                if (string.IsNullOrEmpty(p))
                {
                    continue;
                }

                string n = p.Replace('\\', '/');
                if (string.Equals(n, mainNorm, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(n, editorNorm, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(n, exampleNorm, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Asset postprocessor for <see cref="TrialVersionDllImportSettingsSync"/>: detects asmdef reimports touched by Sentis integration.
    /// </summary>
    internal sealed class TrialVersionDllImportSettingsAssetPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedToAssets,
            string[] movedFromAssetPaths)
        {
            if (TrialVersionDllImportSettingsSync.IsPostprocessSuppressed())
            {
                return;
            }

            if (!TrialVersionDllImportSettingsSync.TryGetOpenCVForUnityFolderAssetPath(out string root))
            {
                return;
            }

            if (!TrialVersionDllImportSettingsSync.PathsTouchSentisRelatedAsmdefs(importedAssets, root)
                && !TrialVersionDllImportSettingsSync.PathsTouchSentisRelatedAsmdefs(deletedAssets, root)
                && !TrialVersionDllImportSettingsSync.PathsTouchSentisRelatedAsmdefs(movedToAssets, root)
                && !TrialVersionDllImportSettingsSync.PathsTouchSentisRelatedAsmdefs(movedFromAssetPaths, root))
            {
                return;
            }

            TrialVersionDllImportSettingsSync.ApplyIfNeeded("asmdefReimport");
        }
    }
}
#endif
