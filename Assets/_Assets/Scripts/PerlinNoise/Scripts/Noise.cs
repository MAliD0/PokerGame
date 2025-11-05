using System.Collections.Generic;
using UnityEngine;

namespace NoiseGeneration
{
    public static class Noise
    {
        public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistance, float lacunarity, Vector2 offset)
        {
            float[,] noiseMap = new float[mapWidth, mapHeight];

            System.Random prng = new System.Random(seed);
            Vector2[] octavesOffsets = new Vector2[octaves];
            for (int i = 0; i < octaves; i++)
            {
                float offsetX = prng.Next(-100000, 100000) + offset.x;
                float offsetY = prng.Next(-100000, 100000) + offset.y;
                octavesOffsets[i] = new Vector2(offsetX, offsetY);
            }

            if (scale == 0)
            {
                scale = 0.00001f;
            }

            float maxNoiseHeight = float.MinValue;
            float minNoiseHeight = float.MaxValue;
            float halfWidth = mapWidth / 2f;
            float halfHeight = mapHeight / 2f;


            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    float amplitude = 1;
                    float frequency = 1;
                    float noiseHeight = 0;

                    for (int i = 0; i < octaves; i++)
                    {
                        float sampleX = (x - halfWidth) / scale * frequency + octavesOffsets[i].x;
                        float sampleY = (y - halfHeight) / scale * frequency + octavesOffsets[i].y;

                        float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                        noiseHeight += perlinValue * amplitude;

                        amplitude *= persistance;
                        frequency *= lacunarity;
                    }

                    if (noiseHeight > maxNoiseHeight)
                    {
                        maxNoiseHeight = noiseHeight;
                    }
                    else if (noiseHeight < minNoiseHeight)
                    {
                        minNoiseHeight = noiseHeight;
                    }

                    noiseMap[x, y] = noiseHeight;
                }
            }

            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
                }
            }

            return noiseMap;
        }
        public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistance, float lacunarity, Vector2 offset, float pow)
        {
            float[,] noiseMap = GenerateNoiseMap(mapWidth, mapHeight, seed, scale, octaves, persistance, lacunarity, offset);

            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    noiseMap[x, y] = Mathf.Clamp01(Mathf.Pow(noiseMap[x, y], pow));
                }
            }

            return noiseMap;
        }

        private static int Hash(int x)
        {
            x = (x << 13) ^ x;
            return (x * (x * x * 15731 + 789221) + 1376312589) & 0x7fffffff;
        }

        public static float GenerateNoise(int seed, double x)
        {
            int xi = (int)x;
            double xf = x - xi;
            int h0 = Hash(xi + seed);
            int h1 = Hash(xi + 1 + seed);
            double u = Fade(xf);
            return (float)Lerp(h0 / (double)int.MaxValue, h1 / (double)int.MaxValue, u);
        }
        public static float[,] WorleyNoise(NoiseSettings noiseSettings, int pointsNumber)
        {
            float[,] noise = new float[noiseSettings.mapWidth, noiseSettings.mapHeight];

            Vector2[] points = new Vector2[pointsNumber];

            for (int i = 0; i < pointsNumber; i++)
            {
                float x = UnityEngine.Random.Range(0, noiseSettings.mapWidth);
                float y = UnityEngine.Random.Range(0, noiseSettings.mapHeight);
                points[i] = new Vector2(x, y);
            }

            for (int x = 0; x < noiseSettings.mapWidth; x++)
            {
                for (int y = 0; y < noiseSettings.mapHeight; y++)
                {
                    float minDistance = float.MaxValue;
                    Vector2 pixelPos = new Vector2(x, y);

                    int id = 0;

                    // Find the minimum distance to the points
                    for (int z = 0; z < points.Length; z++)
                    {
                        var point = points[z];
                        float dist = Vector2.Distance(pixelPos, point);
                        if (dist < minDistance)
                        {
                            minDistance = dist;
                            id = z;
                        }
                    }

                    noise[x, y] = (float)MathFunc.Map(id, 0, pointsNumber, 0, 1);
                }
            }

            return noise;
        }
        //public static float[,] IslandNoise(NoiseSettings noiseSettings, float[,] noiseMap, float radius, float strength)
        //{
        //    float a = noiseSettings.mapWidth / 2f;
        //    float b = noiseSettings.mapHeight / 2f;

        //    for (int x = 0; x < noiseSettings.mapWidth; x++)
        //    {
        //        for (int y = 0; y < noiseSettings.mapHeight; y++)
        //        {
        //            float xCoord = (x - a) * (x - a);
        //            float yCoord = (y - b) * (y - b);
        //            bool mask = xCoord + yCoord <= radius * radius;

        //            float distX = Mathf.Abs(x - a);
        //            float distY = Mathf.Abs(y - b);
        //            float dist = Mathf.Sqrt(distX * distX + distY * distY);
        //            float gradientValue = dist / Mathf.Sqrt(a * a + b * b);
        //            gradientValue = Mathf.Clamp01(1 - gradientValue);

        //            if (mask)
        //            {
        //                noiseMap[x, y] *= gradientValue;
        //                if (noiseMap[x, y] > 0)
        //                {
        //                    noiseMap[x, y] *= strength;
        //                }
        //            }
        //            else
        //            {
        //                noiseMap[x, y] *= gradientValue;
        //            }
        //        }
        //    }
        //    return noiseMap;
        //}
        public static float[,] IslandNoise(NoiseSettings noiseSettings, float[,] noiseMap, float radius, float strength)
        {
            float a = noiseSettings.mapWidth / 2f;
            float b = noiseSettings.mapHeight / 2f;


            for (int x = 0; x < noiseSettings.mapWidth; x++)
            {
                for (int y = 0; y < noiseSettings.mapHeight; y++)
                {
                    float value = Mathf.Pow(x - a, 2) + Mathf.Pow(y - b, 2);
                    bool mask = value < radius * radius;

                    if (mask)
                    {
                        float noiseValue = noiseMap[x, y];

                        float d = Mathf.Sqrt(Mathf.Pow(x - a, 2) + Mathf.Pow(y - b, 2));

                        float scale = 1 - (d / radius);

                        if (noiseValue * (scale * strength) > 1 || noiseValue * (scale * strength) < 0)
                        {
                            //Debug.Log($"{x} {y}: " + noiseValue * (scale * strength));
                            //Debug.Log(Mathf.Clamp01(noiseValue * (scale * strength)));
                        }

                        noiseMap[x, y] = Mathf.Clamp01(noiseValue * (scale * strength));
                    }
                    else
                    {
                        noiseMap[x, y] = 0;
                    }
                }
            }

            return noiseMap;
        }

        #region OrganicVoronoi
        public static float[,] GenerateOrganicVoronoi(NoiseSettings noiseSettings, int pointsNumber)
        {
            float[,] noise = new float[noiseSettings.mapWidth, noiseSettings.mapHeight];

            List<(float x, float y)> featurePoints = GenerateJitteredFeaturePoints(noiseSettings, pointsNumber);

            for (int x = 0; x < noiseSettings.mapWidth; x++)
            {
                for (int y = 0; y < noiseSettings.mapHeight; y++)
                {
                    // Apply domain warping
                    float warpedX = x + Mathf.PerlinNoise(x * 0.1f, y * 0.1f) * 10f;
                    float warpedY = y + Mathf.PerlinNoise(x * 0.1f, y * 0.1f) * 10f;

                    float minDistance = float.MaxValue;
                    int regionId = -1;

                    for (int i = 0; i < featurePoints.Count; i++)
                    {
                        float distance = Vector2.Distance(new Vector2(warpedX, warpedY), new Vector2(featurePoints[i].x, featurePoints[i].y));
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            regionId = i;
                        }
                    }

                    // Normalize region ID
                    noise[x, y] = Mathf.InverseLerp(0, pointsNumber, regionId);
                }
            }

            // Blend biomes using Perlin noise
            for (int x = 0; x < noiseSettings.mapWidth; x++)
            {
                for (int y = 0; y < noiseSettings.mapHeight; y++)
                {
                    float biomeNoise = Mathf.PerlinNoise(x * 0.1f, y * 0.1f);
                    noise[x, y] = Mathf.Lerp(noise[x, y], biomeNoise, 0.5f);
                }
            }

            return noise;
        }
        private static List<(float x, float y)> GenerateJitteredFeaturePoints(NoiseSettings noiseSettings, int pointsNumber)
        {
            List<(float x, float y)> featurePoints = new List<(float x, float y)>();

            for (int i = 0; i < pointsNumber; i++)
            {
                float x = UnityEngine.Random.Range(0, noiseSettings.mapWidth);
                float y = UnityEngine.Random.Range(0, noiseSettings.mapHeight);
                featurePoints.Add((x, y));
            }

            return featurePoints;
        }
        #endregion
        public static float[,] PerlinWorm(NoiseSettings noiseSettings, float[,] noiseMap, float minTrashold)
        {
            for (int x = 0; x < noiseSettings.mapWidth; x++)
            {
                for (int y = 0; y < noiseSettings.mapHeight; y++)
                {
                    if (noiseMap[x, y] < minTrashold)
                    {
                        noiseMap[x, y] = 0;
                    }
                }
            }
            return noiseMap;
        }
        private static double Fade(double t)
        {
            return t * t * t * (t * (t * 6 - 15) + 10);
        }
        private static double Lerp(double a, double b, double t)
        {
            return a + t * (b - a);

        }
    }
}
