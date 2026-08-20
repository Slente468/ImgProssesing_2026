using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System;

/// <summary>
/// Memory-efficient DBSCAN with chunked processing to prevent PC crashes.
/// Unity 6.3 compatible.
/// </summary>
public class DBSCANClusterer : MonoBehaviour
{
    [Header("Input Data")]
    [Tooltip("Folder containing CSV files (inside StreamingAssets)")]
    public string dataFolder = "Data/Coordinates/";

    [Header("DBSCAN Parameters")]
    [Tooltip("Maximum distance between two points to be considered neighbors (in pixels)")]
    public float eps = 10.0f;

    [Tooltip("Minimum number of points to form a cluster")]
    public int minPts = 5;

    [Header("Performance")]
    [Tooltip("Cell size for spatial index (higher = faster but less accurate)")]
    public float cellSize = 20.0f;

    [Header("Chunking")]
    [Tooltip("Maximum points to process at once (reduces memory)")]
    public int maxPointsPerChunk = 50000;

    [Tooltip("Downsample factor (2 = half, 4 = quarter)")]
    public int downsampleFactor = 4;

    [Header("Output")]
    [Tooltip("Save clustered results to CSV")]
    public bool saveResults = true;

    [Header("Debug")]
    [Tooltip("Show progress messages in Console")]
    public bool logProgress = true;

    private List<Point> allPoints = new List<Point>();
    private List<Cluster> clusters = new List<Cluster>();
    private int totalPointsLoaded = 0;

    public struct Point
    {
        public float x;
        public float y;
        public string videoName;
        public int index;

        public Point(float x, float y, string videoName, int index)
        {
            this.x = x;
            this.y = y;
            this.videoName = videoName;
            this.index = index;
        }
    }

    public struct Cluster
    {
        public int id;
        public List<int> pointIndices;
        public float centroidX;
        public float centroidY;

        public Cluster(int id)
        {
            this.id = id;
            pointIndices = new List<int>();
            centroidX = 0;
            centroidY = 0;
        }
    }

    void Start()
    {
        // Start async processing to prevent freezing
        StartCoroutine(RunDBSCANAsync());
    }

    private IEnumerator RunDBSCANAsync()
    {
        Debug.Log("=== Starting DBSCAN Clustering (Async) ===");

        // Step 1: Load all data with downsampling
        yield return StartCoroutine(LoadAllCSVFilesAsync());

        if (allPoints.Count == 0)
        {
            Debug.LogError("No points loaded!");
            yield break;
        }

        Debug.Log($"Loaded {allPoints.Count} points from {dataFolder}");

        // Step 2: If still too many points, sample them
        if (allPoints.Count > maxPointsPerChunk)
        {
            Debug.Log($"Too many points ({allPoints.Count}). Taking random sample...");
            List<Point> sampledPoints = new List<Point>();
            int sampleSize = Math.Min(maxPointsPerChunk, allPoints.Count);

            // Random sampling
            List<int> indices = new List<int>();
            for (int i = 0; i < allPoints.Count; i++) indices.Add(i);
            Shuffle(indices);

            for (int i = 0; i < sampleSize; i++)
            {
                sampledPoints.Add(allPoints[indices[i]]);
            }
            allPoints = sampledPoints;
            Debug.Log($"Sampled down to {allPoints.Count} points");
        }

        // Step 3: Run DBSCAN in chunks
        yield return StartCoroutine(RunDBSCANChunked());

        // Step 4: Log results
        LogResults();

        // Step 5: Save results
        if (saveResults)
        {
            SaveClusteredData();
        }

        Debug.Log("=== DBSCAN Complete ===");
    }

    private void Shuffle(List<int> list)
    {
        System.Random rng = new System.Random();
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            int temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    private IEnumerator LoadAllCSVFilesAsync()
    {
        string fullPath = Path.Combine(Application.streamingAssetsPath, dataFolder);

        if (!Directory.Exists(fullPath))
        {
            fullPath = Path.Combine(Application.dataPath, dataFolder);
            if (!Directory.Exists(fullPath))
            {
                Debug.LogError($"Directory not found: {fullPath}");
                yield break;
            }
        }

        string[] files = Directory.GetFiles(fullPath, "*.csv");
        Debug.Log($"Found {files.Length} CSV files");

        int pointIndex = 0;

        foreach (string file in files)
        {
            string videoName = Path.GetFileNameWithoutExtension(file);
            videoName = videoName.Replace("_coordinates", "");

            if (logProgress)
                Debug.Log($"Loading: {videoName}...");

            int loadedCount = LoadCSVFileWithDownsample(file, videoName, ref pointIndex);

            if (logProgress)
                Debug.Log($"  Loaded {loadedCount} points from {videoName}");

            // Yield every file to keep UI responsive
            yield return null;
        }
    }

    private int LoadCSVFileWithDownsample(string filePath, string videoName, ref int pointIndex)
    {
        int count = 0;
        string[] lines = File.ReadAllLines(filePath);

        // Skip header and downsampled iteration
        int lineCounter = 0;
        for (int i = 1; i < lines.Length; i += downsampleFactor)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line))
                continue;

            string[] parts = line.Split(',');

            if (parts.Length >= 2)
            {
                if (float.TryParse(parts[0], out float x) &&
                    float.TryParse(parts[1], out float y))
                {
                    allPoints.Add(new Point(x, y, videoName, pointIndex));
                    pointIndex++;
                    count++;
                }
            }

            lineCounter++;
        }

        return count;
    }

    private IEnumerator RunDBSCANChunked()
    {
        int numPoints = allPoints.Count;
        int[] clusterIds = new int[numPoints];
        bool[] visited = new bool[numPoints];

        for (int i = 0; i < numPoints; i++)
        {
            clusterIds[i] = -1;
            visited[i] = false;
        }

        int currentClusterId = 0;
        SpatialGrid grid = new SpatialGrid(allPoints, cellSize);

        int processedCount = 0;

        for (int i = 0; i < numPoints; i++)
        {
            if (visited[i])
                continue;

            visited[i] = true;
            List<int> neighbors = grid.GetNeighbors(i, allPoints, eps);

            if (neighbors.Count < minPts)
            {
                clusterIds[i] = -2;
            }
            else
            {
                ExpandClusterChunked(i, neighbors, currentClusterId, clusterIds, visited, grid);
                currentClusterId++;
            }

            processedCount++;

            // Yield every 1000 points to keep UI responsive
            if (processedCount % 1000 == 0)
            {
                if (logProgress)
                    Debug.Log($"Processing point {processedCount}/{numPoints}... Found {currentClusterId} clusters so far");
                yield return null;
            }
        }

        BuildClusters(clusterIds);
    }

    private void ExpandClusterChunked(int pointIndex, List<int> neighbors, int clusterId,
                                   int[] clusterIds, bool[] visited, SpatialGrid grid)
    {
        clusterIds[pointIndex] = clusterId;

        for (int i = 0; i < neighbors.Count; i++)
        {
            int neighborIdx = neighbors[i];

            if (!visited[neighborIdx])
            {
                visited[neighborIdx] = true;
                List<int> neighborNeighbors = grid.GetNeighbors(neighborIdx, allPoints, eps);

                if (neighborNeighbors.Count >= minPts)
                {
                    foreach (int nn in neighborNeighbors)
                    {
                        if (!neighbors.Contains(nn))
                        {
                            neighbors.Add(nn);
                        }
                    }
                }
            }

            if (clusterIds[neighborIdx] == -1 || clusterIds[neighborIdx] == -2)
            {
                clusterIds[neighborIdx] = clusterId;
            }
        }
    }

    private void BuildClusters(int[] clusterIds)
    {
        Dictionary<int, List<int>> clusterMap = new Dictionary<int, List<int>>();

        for (int i = 0; i < clusterIds.Length; i++)
        {
            int id = clusterIds[i];
            if (id < 0) continue;

            if (!clusterMap.ContainsKey(id))
                clusterMap[id] = new List<int>();
            clusterMap[id].Add(i);
        }

        clusters.Clear();

        foreach (var kvp in clusterMap)
        {
            Cluster c = new Cluster(kvp.Key);
            c.pointIndices = kvp.Value;

            float sumX = 0, sumY = 0;
            foreach (int idx in c.pointIndices)
            {
                sumX += allPoints[idx].x;
                sumY += allPoints[idx].y;
            }
            c.centroidX = sumX / c.pointIndices.Count;
            c.centroidY = sumY / c.pointIndices.Count;

            clusters.Add(c);
        }
    }

    private void LogResults()
    {
        HashSet<int> clusteredPoints = new HashSet<int>();
        foreach (var cluster in clusters)
        {
            foreach (int idx in cluster.pointIndices)
            {
                clusteredPoints.Add(idx);
            }
        }
        int totalNoise = allPoints.Count - clusteredPoints.Count;

        Debug.Log($"=== DBSCAN Results ===");
        Debug.Log($"Total points: {allPoints.Count}");
        Debug.Log($"Number of clusters: {clusters.Count}");
        Debug.Log($"Noise points: {totalNoise}");

        Debug.Log("=== Cluster Centroids ===");
        for (int i = 0; i < Math.Min(clusters.Count, 20); i++)
        {
            var c = clusters[i];
            Debug.Log($"Cluster {c.id}: ({c.centroidX:F1}, {c.centroidY:F1}) - {c.pointIndices.Count} points");
        }
        if (clusters.Count > 20)
            Debug.Log($"... and {clusters.Count - 20} more clusters");
    }

    private void SaveClusteredData()
    {
        string outputPath = Path.Combine(Application.dataPath, "Data/Results/");
        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);

        string filePath = Path.Combine(outputPath, "dbscan_results.csv");

        using (StreamWriter writer = new StreamWriter(filePath))
        {
            writer.WriteLine("VideoName,X,Y,Cluster");

            Dictionary<int, int> pointToCluster = new Dictionary<int, int>();
            foreach (var cluster in clusters)
            {
                foreach (int idx in cluster.pointIndices)
                {
                    pointToCluster[idx] = cluster.id;
                }
            }

            for (int i = 0; i < allPoints.Count; i++)
            {
                var p = allPoints[i];
                int clusterId = pointToCluster.ContainsKey(i) ? pointToCluster[i] : -1;
                writer.WriteLine($"{p.videoName},{p.x:F1},{p.y:F1},{clusterId}");
            }
        }

        Debug.Log($"Results saved to: {filePath}");
    }

    public List<Cluster> GetClusters() => clusters;
    public List<Point> GetPoints() => allPoints;

    // ============================================================
    // SpatialGrid - Nested class for fast neighbor queries
    // ============================================================
    private class SpatialGrid
    {
        private Dictionary<long, List<int>> grid = new Dictionary<long, List<int>>();
        private float cellSize;

        public SpatialGrid(List<Point> points, float cellSize)
        {
            this.cellSize = cellSize;
            for (int i = 0; i < points.Count; i++)
            {
                long key = GetCellKey(points[i].x, points[i].y);
                if (!grid.ContainsKey(key))
                    grid[key] = new List<int>();
                grid[key].Add(i);
            }
        }

        private long GetCellKey(float x, float y)
        {
            int cx = (int)(x / cellSize);
            int cy = (int)(y / cellSize);
            return ((long)cx << 32) | (uint)cy;
        }

        public List<int> GetNeighbors(int pointIndex, List<Point> points, float eps)
        {
            List<int> result = new List<int>();
            Point p = points[pointIndex];

            float epsSq = eps * eps;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int cx = (int)(p.x / cellSize) + dx;
                    int cy = (int)(p.y / cellSize) + dy;
                    long key = ((long)cx << 32) | (uint)cy;

                    if (!grid.ContainsKey(key))
                        continue;

                    foreach (int idx in grid[key])
                    {
                        if (idx == pointIndex)
                            continue;

                        Point q = points[idx];
                        float distX = p.x - q.x;
                        float distY = p.y - q.y;
                        if (distX * distX + distY * distY <= epsSq)
                        {
                            result.Add(idx);
                        }
                    }
                }
            }

            return result;
        }
    }
}