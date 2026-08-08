using System;
using OpenCVForUnity.Editor;
using UnityEditor;
using UnityEngine;

namespace OpenCVForUnity
{
    /// <summary>
    /// Manages initialization of PlayMaker Editor classes
    /// Before Unity 5.4 a lot of this was done in EditorWindow constructors.
    /// In Unity 5.4+ this is not allowed and throws an error.
    /// Unity API calls are also not allowed in constructors of Serializable classes.
    /// So instead we do it all here in a non-Serializable class.
    /// </summary>
    [InitializeOnLoad]
    public class TrialVersionWindowStartup
    {
        static TrialVersionWindowStartup()
        {
#if UNITY_6000_0_OR_NEWER
            TrialVersionDllImportSettingsSync.Register();
#endif
            // Resources.Load fails in static constructor (unity bug?)
            // So we delay some work that needs PlayMakerEditorPrefs until next update

            EditorApplication.update -= ShowTrialVersionWindow;
            EditorApplication.update += ShowTrialVersionWindow;

            // Constructor is also called on Playmode change
            // So we need to handle that case (e.g., don't show welcome window)
            // NOTE: This only matters during the startupTime.
            // If we find a better way to do that we can remove this.
#if UNITY_2018_2_OR_NEWER
            EditorApplication.playModeStateChanged -= PlayModeChanged;
            EditorApplication.playModeStateChanged += PlayModeChanged;
#else
            EditorApplication.playmodeStateChanged -= PlayModeChanged;
            EditorApplication.playmodeStateChanged += PlayModeChanged;
#endif
        }

        private static bool IsUnity6rNewer()
        {
            string version = Application.unityVersion;

            // "6000.0.0f1" → major = 6000
            string majorString = version.Split('.')[0];

            if (int.TryParse(majorString, out int major))
            {
                return major >= 6000;
            }

            return false;
        }

        private static void ShowTrialVersionWindow()
        {
            const float startupTime = 30f; // time window to filter startup events from re-compiles. TODO: Is there a better way?
            var showAtStartup = EditorApplication.timeSinceStartup < startupTime;

            if (showAtStartup)
            {
                if (!IsUnity6rNewer())
                {
                    EditorUtility.DisplayDialog(
                        "OpenCV for Unity Trial Version",
                        "The trial version supports Unity 6 or later.",
                        "OK");
                }

                TrialVersionWindow.Open();
            }

            EditorApplication.update -= ShowTrialVersionWindow;
        }

#if UNITY_2018_2_OR_NEWER
        private static void PlayModeChanged(PlayModeStateChange playMode)
        {
            EditorApplication.update -= ShowTrialVersionWindow;
        }
#else
        private static void PlayModeChanged()
        {
            EditorApplication.update -= ShowTrialVersionWindow;
        }
#endif
    }
}
