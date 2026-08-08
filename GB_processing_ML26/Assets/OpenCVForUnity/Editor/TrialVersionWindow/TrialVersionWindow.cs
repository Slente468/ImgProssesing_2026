using UnityEditor;
using UnityEngine;

namespace OpenCVForUnity
{
    /// <summary>
    /// TrialVersionWindow with getting started shortcuts
    /// </summary>
    [InitializeOnLoad]
    public class TrialVersionWindow : EditorWindow
    {
        private const string opencvforunityUrl = "https://assetstore.unity.com/packages/tools/integration/opencv-for-unity-21088?aid=1011l4ehR";

        private const float windowWidth = 513;
        private const float windowHeight = 460;

        private Rect currentPageRect;
        private Rect headerRect;

        private static GUIStyle opencvforunityHeader;

        private static bool stylesInitialized;

        [MenuItem("Tools/OpenCV for Unity/Download Full Version", false, 500)]
        public static void OpenWelcomeWindow()
        {
            var window = GetWindow<TrialVersionWindow>(true);
        }

        public static void Open()
        {
            OpenWelcomeWindow();
        }

        public void OnEnable()
        {
            titleContent = new GUIContent("Download Full Version");

            maxSize = new Vector2(windowWidth, windowHeight);
            minSize = maxSize;

            headerRect = new Rect(0, 0, windowWidth, 389);
            currentPageRect = new Rect(0, headerRect.height, windowWidth, windowHeight - headerRect.height);
        }

        private static void InitStyles()
        {
            if (!stylesInitialized)
            {
                opencvforunityHeader = new GUIStyle
                {
                    normal =
                    {
                        background = Resources.Load("opencvforuntiyLogo") as Texture2D,
                        textColor = Color.white
                    },
                };
            }

            stylesInitialized = true;
        }

        public void OnGUI()
        {
            InitStyles();

            GUILayout.BeginVertical();

            GUI.Box(headerRect, "", opencvforunityHeader);

            GUILayout.BeginArea(currentPageRect);

            GUILayout.BeginHorizontal();
            GUILayout.Space(60);

            GUILayout.BeginVertical();
            GUILayout.Space(15);

            AddonDownload("Download Full Version",
                "Download Full Version",
                opencvforunityUrl);
            GUILayout.EndVertical();

            GUILayout.Space(60);

            GUILayout.EndHorizontal();

            GUILayout.EndArea();

            GUILayout.EndVertical();
        }

        private static void AddonDownload(string name, string tooltip, string url, Texture image = null)
        {
            if (Button(name, tooltip, image))
            {
                Application.OpenURL(url);
            }
        }

        private static bool Button(string name, string tooltip, Texture image = null)
        {
            InitLabel(name, tooltip); // TODO: Fix image styling
            return GUILayout.Button(label, GUILayout.Height(40));
        }

        private static readonly GUIContent label = new GUIContent();

        private static void InitLabel(string text, string tooltip, Texture image = null)
        {
            label.text = text;
            label.tooltip = tooltip;
            label.image = image;
        }
    }
}
