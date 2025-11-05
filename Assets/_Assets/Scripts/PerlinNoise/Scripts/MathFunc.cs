

using System;
using System.Collections.Generic;
using UnityEngine;

public static class MathFunc 
{
    public static double Map(double n, double start1, double stop1, double start2, double stop2, bool withinBounds = false)
    {
        // Validate parameters (assuming the validation is similar to p5.js)
        if (stop1 == start1 || stop2 == start2)
        {
            throw new ArgumentException("Invalid range for mapping.");
        }

        // Calculate the mapped value
        double newval = (n - start1) / (stop1 - start1) * (stop2 - start2) + start2;

        // Constrain the value if withinBounds is true
        if (!withinBounds)
        {
            return newval;
        }

        if (start2 < stop2)
        {
            return Constrain(newval, start2, stop2);
        }
        else
        {
            return Constrain(newval, stop2, start2);
        }
    }

    private static double Constrain(double value, double min, double max)
    {
        return Math.Max(Math.Min(value, max), min);
    }
    public static bool IsBetween(float x, float min, float max)
    {
        return x >= min && x <= max;
    }
    public static List<Vector2Int> FindLocalMaxima(float[,] noiseMap)
    {
        List<Vector2Int> maximas = new List<Vector2Int>();
        for (int x = 0; x < noiseMap.GetLength(0); x++)
        {
            for (int y = 0; y < noiseMap.GetLength(1); y++)
            {
                var noiseVal = noiseMap[x, y];
                if (CheckNeighbours(x, y, noiseMap, (neighbourNoise) => neighbourNoise > noiseVal))
                {
                    maximas.Add(new Vector2Int(x, y));
                }

            }
        }
        return maximas;
    }
    public static List<Vector2Int> FindLocalMinima(float[,] noiseMap)
    {
        List<Vector2Int> minima = new List<Vector2Int>();
        for (int x = 0; x < noiseMap.GetLength(0); x++)
        {
            for (int y = 0; y < noiseMap.GetLength(1); y++)
            {
                var noiseVal = noiseMap[x, y];
                if (CheckNeighbours(x, y, noiseMap, (neighbourNoise) => neighbourNoise < noiseVal))
                {
                    minima.Add(new Vector2Int(x, y));
                }

            }
        }
        return minima;
    }
    static List<Vector2Int> directions = new List<Vector2Int>
    {
        new Vector2Int( 0, 1), //N
        new Vector2Int( 1, 1), //NE
        new Vector2Int( 1, 0), //E
        new Vector2Int(-1, 1), //SE
        new Vector2Int(-1, 0), //S
        new Vector2Int(-1,-1), //SW
        new Vector2Int( 0,-1), //W
        new Vector2Int( 1,-1)  //NW
    };
    private static bool CheckNeighbours(int x, int y, float[,] noiseMap, Func<float, bool> failCondition)
    {
        foreach (var dir in directions)
        {
            var newPost = new Vector2Int(x + dir.x, y + dir.y);

            if (newPost.x < 0 || newPost.x >= noiseMap.GetLength(0) || newPost.y < 0 || newPost.y >= noiseMap.GetLength(1))
            {
                continue;
            }

            if (failCondition(noiseMap[x + dir.x, y + dir.y]))
            {
                return false;
            }
        }
        return true;
    }
    public static float RangeMap(float inputValue, float inMin, float inMax, float outMin, float outMax)
    {
        return outMin + (inputValue - inMin) * (outMax - outMin) / (inMax - inMin);
    }
}
