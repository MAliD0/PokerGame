using System.Collections.Generic;
using UnityEngine;

public static class PoissonDicsSampling
{
    public static List<Vector2> GeneratePoints(
        float radius,
        Vector2 sampleRegionSize,
        int numSamplesBeforeRejection = 30)
    {
        float cellSize = radius / Mathf.Sqrt(2);

        int gridWidth  = Mathf.CeilToInt(sampleRegionSize.x / cellSize);
        int gridHeight = Mathf.CeilToInt(sampleRegionSize.y / cellSize);

        int[,] grid = new int[gridWidth, gridHeight];

        List<Vector2> points = new List<Vector2>();
        List<Vector2> spawnPoints = new List<Vector2>();

        // WORLD is centered around 0,0:
        // x: -width/2  ... +width/2
        // y: -height/2 ... +height/2
        Vector2 half = sampleRegionSize / 2f;

        // start at world center (0,0)
        spawnPoints.Add(Vector2.zero);

        while (spawnPoints.Count > 0)
        {
            int spawnIndex = Random.Range(0, spawnPoints.Count);
            Vector2 spawnCenter = spawnPoints[spawnIndex];
            bool accepted = false;

            for (int i = 0; i < numSamplesBeforeRejection; i++)
            {
                float angle = Random.value * Mathf.PI * 2;
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 candidate = spawnCenter + dir * Random.Range(radius, radius * 2);

                if (IsValid(candidate, sampleRegionSize, cellSize, radius, points, grid))
                {
                    accepted = true;

                    points.Add(candidate);
                    spawnPoints.Add(candidate);

                    // convert world → grid index
                    int gx = (int)((candidate.x + half.x) / cellSize);
                    int gy = (int)((candidate.y + half.y) / cellSize);

                    grid[gx, gy] = points.Count;
                    break;
                }
            }

            if (!accepted)
                spawnPoints.RemoveAt(spawnIndex);
        }

        return points;
    }

    static bool IsValid(
        Vector2 candidate,
        Vector2 sampleRegionSize,
        float cellSize,
        float radius,
        List<Vector2> points,
        int[,] grid)
    {
        Vector2 half = sampleRegionSize / 2f;

        // candidate must be inside negative/positive region
        if (candidate.x < -half.x || candidate.x > half.x ||
            candidate.y < -half.y || candidate.y > half.y)
            return false;

        int cellX = (int)((candidate.x + half.x) / cellSize);
        int cellY = (int)((candidate.y + half.y) / cellSize);

        int searchStartX = Mathf.Max(0, cellX - 2);
        int searchEndX   = Mathf.Min(grid.GetLength(0) - 1, cellX + 2);
        int searchStartY = Mathf.Max(0, cellY - 2);
        int searchEndY   = Mathf.Min(grid.GetLength(1) - 1, cellY + 2);

        for (int x = searchStartX; x <= searchEndX; x++)
        {
            for (int y = searchStartY; y <= searchEndY; y++)
            {
                int pointIndex = grid[x, y] - 1;
                if (pointIndex != -1)
                {
                    float sqrDist = (candidate - points[pointIndex]).sqrMagnitude;
                    if (sqrDist < radius * radius)
                        return false;
                }
            }
        }

        return true;
    }

    // -----------------------------------------------------------------------
    // CONVERSION: List<Vector2> → float[,]
    // Coordinates are in world space (-half..+half), grid will be centered.
    // -----------------------------------------------------------------------
    public static float[,] ConvertToGrid(
        List<Vector2> points,
        float radius,
        Vector2 sampleRegionSize)
    {
        float cellSize = radius / Mathf.Sqrt(2);
        Vector2 half = sampleRegionSize / 2f;

        int width  = Mathf.CeilToInt(sampleRegionSize.x / cellSize);
        int height = Mathf.CeilToInt(sampleRegionSize.y / cellSize);

        float[,] grid = new float[width, height];

        foreach (var p in points)
        {
            // world → positive grid index
            int gx = (int)((p.x + half.x) / cellSize);
            int gy = (int)((p.y + half.y) / cellSize);

            if (gx >= 0 && gx < width && gy >= 0 && gy < height)
                grid[gx, gy] = 1f;
        }

        return grid;
    }
}
