using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Visualizes DBSCAN clusters as a transparent overlay on top of the Prime Image.
/// Each cluster gets its own unique color from a 37-color palette.
/// </summary>
public class ClusterVisualizer : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The DBSCANClusterer that contains the clustering results")]
    public DBSCANClusterer clusterer;

    [Tooltip("The RawImage where the transparent overlay will be shown")]
    public RawImage overlayDisplay;

    [Tooltip("The background texture (Prime Image or Segmentation Map)")]
    public Texture2D backgroundImage;

    [Header("Visual Settings")]
    [Tooltip("Draw all cluster points")]
    public bool drawClusters = true;

    [Tooltip("Draw centroids (white dots with colored centers)")]
    public bool drawCentroids = true;

    [Tooltip("Size of each cluster point")]
    public float pointRadius = 1.5f;

    [Tooltip("Size of the centroid marker")]
    public float centroidRadius = 5.0f;

    [Tooltip("Minimum points to consider a cluster worth showing (filters tiny clusters)")]
    public int minPointsToShow = 10;

    [Header("Color Settings")]
    [Tooltip("If checked, each cluster gets its own color from the palette")]
    public bool useUniqueClusterColors = true;

    [Tooltip("Color palette for clusters (37 distinct colors)")]
    public List<Color> clusterPalette = new List<Color>();

    [Tooltip("Color of noise points (semi-transparent)")]
    public Color noiseColor = new Color(0.3f, 0.3f, 0.3f, 0.2f);

    [Tooltip("Color of centroid outline (white for contrast)")]
    public Color centroidOutlineColor = Color.white;

    [Header("Filters")]
    [Tooltip("If true, only show clusters with >= minPointsToShow points")]
    public bool filterSmallClusters = true;

    [Header("Status")]
    [Tooltip("Shows if clustering is complete")]
    public bool isClusteringComplete = false;

    private Texture2D overlayTexture;

    // ============================================================
    // START - Initialize and check clustering status
    // ============================================================
    void Start()
    {
        // Initialize default palette if empty
        if (clusterPalette == null || clusterPalette.Count == 0)
        {
            InitializeDefaultPalette();
        }

        // Check if clustering is already complete
        CheckClusteringStatus();

        // If not complete, wait and check again
        if (!isClusteringComplete)
        {
            Debug.Log("⏳ Waiting for DBSCAN to complete... Will check again in 2 seconds.");
            Invoke("CheckClusteringStatus", 2.0f);
        }
        else
        {
            // Clustering is complete, generate overlay
            Debug.Log("✅ Clustering is complete! Generating overlay...");
            GenerateTransparentOverlay();
        }
    }

    // ============================================================
    // INITIALIZE DEFAULT PALETTE - 37 DISTINCT COLORS
    // ============================================================
    private void InitializeDefaultPalette()
    {
        clusterPalette = new List<Color>()
        {
            // Red family (4)
            new Color(1.00f, 0.00f, 0.00f, 1.0f),     // Red
            new Color(0.80f, 0.00f, 0.20f, 1.0f),     // Crimson
            new Color(0.90f, 0.20f, 0.00f, 1.0f),     // Vermilion
            new Color(1.00f, 0.30f, 0.30f, 1.0f),     // Light Red

            // Orange family (3)
            new Color(1.00f, 0.50f, 0.00f, 1.0f),     // Orange
            new Color(1.00f, 0.60f, 0.20f, 1.0f),     // Light Orange
            new Color(0.80f, 0.40f, 0.00f, 1.0f),     // Dark Orange

            // Yellow family (3)
            new Color(1.00f, 1.00f, 0.00f, 1.0f),     // Yellow
            new Color(1.00f, 0.90f, 0.20f, 1.0f),     // Light Yellow
            new Color(0.80f, 0.80f, 0.00f, 1.0f),     // Dark Yellow

            // Green family (5)
            new Color(0.00f, 1.00f, 0.00f, 1.0f),     // Green
            new Color(0.20f, 0.80f, 0.20f, 1.0f),     // Forest Green
            new Color(0.00f, 0.80f, 0.40f, 1.0f),     // Mint
            new Color(0.40f, 1.00f, 0.40f, 1.0f),     // Light Green
            new Color(0.00f, 0.60f, 0.00f, 1.0f),     // Dark Green

            // Cyan/Teal family (4)
            new Color(0.00f, 1.00f, 1.00f, 1.0f),     // Cyan
            new Color(0.00f, 0.80f, 0.80f, 1.0f),     // Teal
            new Color(0.20f, 0.90f, 0.90f, 1.0f),     // Light Cyan
            new Color(0.00f, 0.50f, 0.50f, 1.0f),     // Dark Teal

            // Blue family (5)
            new Color(0.00f, 0.00f, 1.00f, 1.0f),     // Blue
            new Color(0.20f, 0.30f, 0.90f, 1.0f),     // Royal Blue
            new Color(0.30f, 0.60f, 1.00f, 1.0f),     // Light Blue
            new Color(0.00f, 0.00f, 0.70f, 1.0f),     // Dark Blue
            new Color(0.40f, 0.40f, 0.90f, 1.0f),     // Periwinkle

            // Purple/Violet family (4)
            new Color(0.50f, 0.00f, 1.00f, 1.0f),     // Purple
            new Color(0.80f, 0.20f, 1.00f, 1.0f),     // Violet
            new Color(0.60f, 0.00f, 0.80f, 1.0f),     // Dark Purple
            new Color(0.70f, 0.40f, 0.90f, 1.0f),     // Light Purple

            // Pink/Magenta family (4)
            new Color(1.00f, 0.00f, 1.00f, 1.0f),     // Magenta
            new Color(1.00f, 0.20f, 0.60f, 1.0f),     // Pink
            new Color(0.90f, 0.00f, 0.70f, 1.0f),     // Hot Pink
            new Color(0.80f, 0.30f, 0.80f, 1.0f),     // Light Magenta

            // Brown/Tan family (3)
            new Color(0.50f, 0.30f, 0.00f, 1.0f),     // Brown
            new Color(0.70f, 0.50f, 0.20f, 1.0f),     // Tan
            new Color(0.60f, 0.40f, 0.10f, 1.0f),     // Dark Tan

            // Unique colors (2)
            new Color(0.00f, 1.00f, 0.50f, 1.0f),     // Spring Green
            new Color(1.00f, 0.50f, 0.50f, 1.0f),     // Salmon

            // TOTAL: 37 colors
        };
    }

    // ============================================================
    // CHECK CLUSTERING STATUS - Returns true if DBSCAN has data
    // ============================================================
    public bool CheckClusteringStatus()
    {
        if (clusterer == null)
        {
            Debug.LogWarning("⚠️ clusterer is NULL! Assign the DBSCANClusterer GameObject.");
            isClusteringComplete = false;
            return false;
        }

        List<DBSCANClusterer.Cluster> clusters = clusterer.GetClusters();
        List<DBSCANClusterer.Point> points = clusterer.GetPoints();

        if (clusters != null && clusters.Count > 0)
        {
            isClusteringComplete = true;
            Debug.Log($"✅ Clustering complete! Found {clusters.Count} clusters with {points.Count} total points.");
            return true;
        }
        else
        {
            isClusteringComplete = false;
            Debug.Log("⏳ Clustering not yet complete or no clusters found.");
            return false;
        }
    }

    // ============================================================
    // GET COLOR FOR CLUSTER - Returns unique color per cluster
    // ============================================================
    private Color GetClusterColor(int clusterId)
    {
        if (useUniqueClusterColors && clusterPalette != null && clusterPalette.Count > 0)
        {
            return clusterPalette[clusterId % clusterPalette.Count];
        }
        // Fallback: all yellow
        return Color.yellow;
    }

    // ============================================================
    // PUBLIC METHODS - Called from Inspector buttons or code
    // ============================================================

    [ContextMenu("Generate Transparent Overlay")]
    public void GenerateTransparentOverlay()
    {
        Debug.Log("=== Starting GenerateTransparentOverlay ===");

        // Check 1: Is backgroundImage assigned?
        if (backgroundImage == null)
        {
            Debug.LogError("❌ backgroundImage is NULL! Assign a texture in the Inspector.");
            return;
        }
        Debug.Log($"✅ backgroundImage: {backgroundImage.name} ({backgroundImage.width}x{backgroundImage.height})");

        // Check 2: Is clusterer assigned?
        if (clusterer == null)
        {
            Debug.LogError("❌ clusterer is NULL! Assign the DBSCANClusterer GameObject.");
            return;
        }
        Debug.Log($"✅ clusterer: {clusterer.name}");

        // Check 3: Does clusterer have data?
        List<DBSCANClusterer.Cluster> allClusters = clusterer.GetClusters();
        List<DBSCANClusterer.Point> points = clusterer.GetPoints();

        if (allClusters == null || allClusters.Count == 0)
        {
            Debug.LogError($"❌ No clusters found! Clusters count: {allClusters?.Count ?? 0}");
            Debug.LogError("   Did you run DBSCAN first? Click 'Refresh Clustering Status' button.");
            return;
        }
        Debug.Log($"✅ Found {allClusters.Count} clusters with {points.Count} total points");

        // Filter small clusters if enabled
        List<DBSCANClusterer.Cluster> clusters = new List<DBSCANClusterer.Cluster>();
        int filteredCount = 0;
        if (filterSmallClusters)
        {
            foreach (var c in allClusters)
            {
                if (c.pointIndices.Count >= minPointsToShow)
                {
                    clusters.Add(c);
                }
                else
                {
                    filteredCount++;
                }
            }
            Debug.Log($"🔍 Filtered out {filteredCount} clusters with < {minPointsToShow} points. Showing {clusters.Count} clusters.");
        }
        else
        {
            clusters = allClusters;
        }

        if (clusters.Count == 0)
        {
            Debug.LogError($"❌ No clusters remain after filtering! Try lowering minPointsToShow or disabling filterSmallClusters.");
            Debug.LogError($"   Total clusters: {allClusters.Count}, Filtered: {filteredCount}");
            return;
        }

        // Check 4: Is overlayDisplay assigned?
        if (overlayDisplay == null)
        {
            Debug.LogError("❌ overlayDisplay is NULL! Assign a RawImage in the Inspector.");
            return;
        }
        Debug.Log($"✅ overlayDisplay: {overlayDisplay.name}");

        // Create the overlay texture
        int width = backgroundImage.width;
        int height = backgroundImage.height;
        Debug.Log($"📐 Creating overlay texture: {width}x{height}");

        overlayTexture = new Texture2D(width, height, TextureFormat.ARGB32, false);
        Color transparentBlack = new Color(0, 0, 0, 0);
        Color[] clearPixels = new Color[width * height];
        for (int i = 0; i < clearPixels.Length; i++)
            clearPixels[i] = transparentBlack;
        overlayTexture.SetPixels(clearPixels);

        // Build cluster map for noise detection
        Dictionary<int, int> pointToCluster = new Dictionary<int, int>();
        foreach (var c in clusters)
            foreach (int idx in c.pointIndices)
                pointToCluster[idx] = c.id;

        Debug.Log($"🎨 Drawing with {clusterPalette.Count} unique colors...");

        // Draw CLUSTER points - EACH CLUSTER GETS ITS OWN COLOR
        int drawnPoints = 0;
        if (drawClusters)
        {
            foreach (var cluster in clusters)
            {
                Color clusterColor = GetClusterColor(cluster.id);
                Debug.Log($"   Cluster {cluster.id}: {cluster.pointIndices.Count} pts - Color: {ColorUtility.ToHtmlStringRGB(clusterColor)}");

                foreach (int idx in cluster.pointIndices)
                {
                    if (idx < points.Count)
                    {
                        Vector2 pos = new Vector2(points[idx].x, points[idx].y);
                        DrawCircle(overlayTexture, pos, pointRadius, clusterColor);
                        drawnPoints++;
                    }
                }
            }

            // Draw Noise points (semi-transparent gray)
            int noiseCount = 0;
            for (int i = 0; i < points.Count; i++)
            {
                if (!pointToCluster.ContainsKey(i))
                {
                    Vector2 pos = new Vector2(points[i].x, points[i].y);
                    DrawCircle(overlayTexture, pos, pointRadius * 0.5f, noiseColor);
                    noiseCount++;
                }
            }
            Debug.Log($"✅ Drew {drawnPoints} cluster points and {noiseCount} noise points");
        }

        // Draw CENTROIDS - White outline with colored center
        if (drawCentroids)
        {
            Debug.Log($"🎯 Drawing {clusters.Count} centroids...");
            foreach (var cluster in clusters)
            {
                Color clusterColor = GetClusterColor(cluster.id);
                Vector2 center = new Vector2(cluster.centroidX, cluster.centroidY);

                // White outline (larger)
                DrawCircle(overlayTexture, center, centroidRadius, centroidOutlineColor);
                // Colored center (smaller)
                DrawCircle(overlayTexture, center, centroidRadius - 1.5f, clusterColor);

                Debug.Log($"   Centroid {cluster.id}: ({center.x:F0}, {center.y:F0}) - {cluster.pointIndices.Count} pts");
            }
        }

        overlayTexture.Apply();
        Debug.Log($"✅ Texture applied! Pixel count: {overlayTexture.width * overlayTexture.height}");

        // Assign to RawImage
        overlayDisplay.texture = overlayTexture;
        Debug.Log($"✅ Assigned texture to {overlayDisplay.name}");

        Debug.Log("=== GenerateTransparentOverlay COMPLETE ===");
    }

    // ============================================================
    // FORCE GENERATE - Called from Inspector button
    // ============================================================
    public void ForceGenerateOverlay()
    {
        Debug.Log("🔄 Force generating overlay...");
        // Check status first
        CheckClusteringStatus();
        if (!isClusteringComplete)
        {
            Debug.LogWarning("⚠️ Clustering is not complete! DBSCAN may still be running.");
            Debug.LogWarning("   Try again in a few seconds, or check the Console for DBSCAN completion.");
            return;
        }
        GenerateTransparentOverlay();
    }

    // ============================================================
    // REFRESH STATUS - Check if DBSCAN is done
    // ============================================================
    public void RefreshStatus()
    {
        Debug.Log("🔄 Refreshing clustering status...");
        CheckClusteringStatus();
        if (isClusteringComplete)
        {
            List<DBSCANClusterer.Cluster> clusters = clusterer.GetClusters();
            Debug.Log($"✅ Clustering is complete! Found {clusters.Count} clusters.");
            Debug.Log("   You can now generate the overlay.");
        }
        else
        {
            Debug.Log("⏳ Clustering is still running or not started. Check the Console for DBSCAN progress.");
        }
    }

    // ============================================================
    // SAVE METHODS - Save the overlay as PNG
    // ============================================================

    public void SaveOverlayAsPNG(string filename = "ClusterOverlay")
    {
        if (overlayTexture == null)
        {
            Debug.LogError("❌ No overlay texture to save! Generate it first.");
            return;
        }

        string folderPath = Path.Combine(Application.dataPath, "Data/Overlays/");
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        string filePath = Path.Combine(folderPath, filename + ".png");
        byte[] bytes = overlayTexture.EncodeToPNG();
        File.WriteAllBytes(filePath, bytes);
        Debug.Log($"✅ Overlay saved to: {filePath}");

        #if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
        #endif
    }

    [ContextMenu("Save Combined Image (Background + Overlay)")]
    public void SaveCombinedImage()
    {
        if (backgroundImage == null || overlayTexture == null)
        {
            Debug.LogError("❌ Missing background or overlay texture!");
            return;
        }

        Texture2D combined = new Texture2D(backgroundImage.width, backgroundImage.height, TextureFormat.ARGB32, false);
        Color[] bgPixels = backgroundImage.GetPixels();
        Color[] ovPixels = overlayTexture.GetPixels();
        Color[] finalPixels = new Color[bgPixels.Length];

        // Alpha blending: Overlay pixel over background
        for (int i = 0; i < finalPixels.Length; i++)
        {
            Color bg = bgPixels[i];
            Color ov = ovPixels[i];
            // If overlay is transparent, show background
            if (ov.a == 0) finalPixels[i] = bg;
            else finalPixels[i] = Color.Lerp(bg, ov, ov.a); // Standard blend
        }

        combined.SetPixels(finalPixels);
        combined.Apply();

        string folderPath = Path.Combine(Application.dataPath, "Data/Combined/");
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        string filePath = Path.Combine(folderPath, "Prime_With_Clusters.png");
        File.WriteAllBytes(filePath, combined.EncodeToPNG());
        Debug.Log($"✅ Combined image saved to: {filePath}");

        #if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
        #endif
    }

    // ============================================================
    // PRIVATE HELPER - Draws a solid circle with alpha support
    // ============================================================
    private void DrawCircle(Texture2D tex, Vector2 center, float radius, Color color)
    {
        int cx = Mathf.RoundToInt(center.x);
        int cy = Mathf.RoundToInt(center.y);
        int r = Mathf.RoundToInt(radius);

        int xMin = Mathf.Max(0, cx - r);
        int xMax = Mathf.Min(tex.width - 1, cx + r);
        int yMin = Mathf.Max(0, cy - r);
        int yMax = Mathf.Min(tex.height - 1, cy + r);

        for (int y = yMin; y <= yMax; y++)
        {
            for (int x = xMin; x <= xMax; x++)
            {
                if (Vector2.Distance(new Vector2(x, y), center) <= radius)
                {
                    tex.SetPixel(x, y, color);
                }
            }
        }
    }
}

// ============================================================
// CUSTOM INSPECTOR - Adds buttons to the Inspector
// ============================================================
#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(ClusterVisualizer))]
public class ClusterVisualizerEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ClusterVisualizer script = (ClusterVisualizer)target;

        UnityEditor.EditorGUILayout.Space(10);
        UnityEditor.EditorGUILayout.LabelField("🚀 Actions", UnityEditor.EditorStyles.boldLabel);

        // Status display
        GUI.backgroundColor = script.isClusteringComplete ? Color.green : Color.yellow;
        string statusText = script.isClusteringComplete ? "✅ Clustering Complete" : "⏳ Waiting for Clustering...";
        UnityEditor.EditorGUILayout.LabelField("Status:", statusText, UnityEditor.EditorStyles.boldLabel);
        GUI.backgroundColor = Color.white;

        UnityEditor.EditorGUILayout.Space(5);

        // Refresh Status button
        if (GUILayout.Button("🔄 Refresh Clustering Status", GUILayout.Height(25)))
        {
            script.RefreshStatus();
        }

        // Generate Overlay button (only enabled if clustering is complete)
        GUI.enabled = script.isClusteringComplete;
        if (GUILayout.Button("🌈 Generate Colored Cluster Overlay", GUILayout.Height(30)))
        {
            script.ForceGenerateOverlay();
        }
        GUI.enabled = true;

        UnityEditor.EditorGUILayout.Space(5);

        // Save buttons
        if (GUILayout.Button("💾 Save Overlay as PNG", GUILayout.Height(25)))
        {
            script.SaveOverlayAsPNG("ClusterOverlay_" + System.DateTime.Now.ToString("HHmmss"));
        }

        if (GUILayout.Button("📸 Save Combined Image (Background + Overlay)", GUILayout.Height(25)))
        {
            script.SaveCombinedImage();
        }

        UnityEditor.EditorGUILayout.Space(5);

        // Help box with filter info
        string filterInfo = script.filterSmallClusters ?
            $"Filtering clusters with < {script.minPointsToShow} points" :
            "Showing all clusters (filter disabled)";
        UnityEditor.EditorGUILayout.HelpBox(
            "1. Press Play to start DBSCAN\n" +
            "2. Wait for DBSCAN to complete (check Console)\n" +
            "3. Click 'Refresh Clustering Status' to check\n" +
            "4. Click 'Generate Colored Cluster Overlay' when ready\n\n" +
            "🔍 Filter: " + filterInfo,
            UnityEditor.MessageType.Info
        );
    }
}
#endif


