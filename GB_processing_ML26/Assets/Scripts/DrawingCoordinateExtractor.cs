using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>
/// Extracts drawing pixel coordinates from a drawing mask texture.
/// Saves them to a CSV file for use with DBSCAN clustering.
/// </summary>
public class DrawingCoordinateExtractor : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("The drawing mask texture (black background, white lines)")]
    public Texture2D drawingMask;

    [Header("Output")]
    [Tooltip("Name of this video (used in CSV export)")]
    public string videoName = "Video_1";

    [Header("Settings")]
    [Tooltip("Threshold to consider a pixel as 'drawn' (0-1)")]
    public float drawThreshold = 0.5f;

    [Tooltip("Export folder path (relative to Assets folder)")]
    public string exportFolder = "Data/";

    [Header("Debug")]
    [Tooltip("Show the extracted coordinate count in console")]
    public bool logResults = true;

    /// <summary>
    /// Extracts coordinates from the assigned drawing mask.
    /// Returns a list of Vector2Int (x, y) positions.
    /// </summary>
    public List<Vector2Int> ExtractCoordinates()
    {
        if (drawingMask == null)
        {
            Debug.LogError("No drawing mask assigned!");
            return new List<Vector2Int>();
        }

        List<Vector2Int> coordinates = new List<Vector2Int>();
        
        // Get pixel data
        Color[] pixels = drawingMask.GetPixels();
        int width = drawingMask.width;
        int height = drawingMask.height;

        // Iterate through all pixels
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                // Check if pixel is drawn (white) based on threshold
                if (pixels[index].r > drawThreshold)
                {
                    coordinates.Add(new Vector2Int(x, y));
                }
            }
        }

        if (logResults)
        {
            Debug.Log($"Extracted {coordinates.Count} drawing coordinates from {videoName}");
        }

        return coordinates;
    }

    /// <summary>
    /// Exports coordinates to a CSV file.
    /// </summary>
    public void ExportToCSV(List<Vector2Int> coordinates)
    {
        if (coordinates == null || coordinates.Count == 0)
        {
            Debug.LogWarning("No coordinates to export!");
            return;
        }

        // Create export folder if it doesn't exist
        string fullPath = Path.Combine(Application.dataPath, exportFolder);
        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
        }

        // Create CSV file
        string fileName = $"{videoName}_coordinates.csv";
        string filePath = Path.Combine(fullPath, fileName);

        StringBuilder csv = new StringBuilder();
        csv.AppendLine("X,Y"); // Header

        foreach (Vector2Int coord in coordinates)
        {
            csv.AppendLine($"{coord.x},{coord.y}");
        }

        File.WriteAllText(filePath, csv.ToString());

        Debug.Log($"Exported {coordinates.Count} coordinates to: {filePath}");
    }

    /// <summary>
    /// One-click method: Extract and export.
    /// </summary>
    public void ProcessAndExport()
    {
        List<Vector2Int> coordinates = ExtractCoordinates();
        if (coordinates.Count > 0)
        {
            ExportToCSV(coordinates);
        }
        else
        {
            Debug.LogWarning("No coordinates extracted. Check your drawing mask and threshold.");
        }
    }

    /// <summary>
    /// Combines multiple coordinate lists into one CSV file.
    /// Useful when processing multiple videos.
    /// </summary>
    public void ExportAllToSingleCSV(Dictionary<string, List<Vector2Int>> allCoordinates)
    {
        if (allCoordinates == null || allCoordinates.Count == 0)
        {
            Debug.LogWarning("No data to export!");
            return;
        }

        // Create export folder
        string fullPath = Path.Combine(Application.dataPath, exportFolder);
        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
        }

        string fileName = "all_drawing_coordinates.csv";
        string filePath = Path.Combine(fullPath, fileName);

        StringBuilder csv = new StringBuilder();
        csv.AppendLine("VideoName,X,Y");

        foreach (var kvp in allCoordinates)
        {
            string videoName = kvp.Key;
            List<Vector2Int> coords = kvp.Value;
            
            foreach (Vector2Int coord in coords)
            {
                csv.AppendLine($"{videoName},{coord.x},{coord.y}");
            }
        }

        File.WriteAllText(filePath, csv.ToString());
        Debug.Log($"Exported all coordinates to: {filePath}");
    }

    // ============================================================
    // Optional: Editor Button
    // ============================================================
    #if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(DrawingCoordinateExtractor))]
    public class DrawingCoordinateExtractorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            DrawingCoordinateExtractor extractor = (DrawingCoordinateExtractor)target;

            UnityEditor.EditorGUILayout.Space(10);
            UnityEditor.EditorGUILayout.LabelField("Actions", UnityEditor.EditorStyles.boldLabel);

            if (GUILayout.Button("Extract & Export Coordinates"))
            {
                extractor.ProcessAndExport();
            }
        }
    }
    #endif
}