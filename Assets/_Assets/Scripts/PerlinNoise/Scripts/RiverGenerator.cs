using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NoiseGeneration;

namespace ProceduralGeneration
{
    public class RiverGenerator : MonoBehaviour
    {
        public NoiseSettings riverSettings;
        public Vector2 riverStartPosition;
        public int riverLength = 50;
        public bool bold = true;
        public bool converganceOn = true;
        private float[,] noiseMap;
        private float minimas;
        private float maximas;

        public void SetupRiverGenerator(float[,] noiseMap, float minimas, float maximas)
        {
            this.noiseMap = noiseMap;
            this.minimas = minimas;
            this.maximas = maximas;
        }

        public float[,] GenerateRivers()
        {
            var result = MathFunc.FindLocalMaxima(noiseMap);
            var toCreate = result.Where(pos => noiseMap[pos.x, pos.y] > maximas).OrderBy(a => Guid.NewGuid()).Take(5).ToList();
            var waterMinimas = MathFunc.FindLocalMinima(noiseMap);
            waterMinimas = waterMinimas.Where(pos => noiseMap[pos.x, pos.y] < minimas).OrderBy(pos => noiseMap[pos.x, pos.y]).Take(20).ToList();
            foreach (var item in toCreate)
            {
                //SetTileTo(item.x, item.y, maxPosTile);
                noiseMap = CreateRiver(item, waterMinimas);

            }
            return noiseMap;
        }

        private float[,] CreateRiver(Vector2Int startPosition, List<Vector2Int> waterMinimas)
        {
            PerlinWorm worm;
            if (converganceOn)
            {
                var closestWaterPos = waterMinimas.OrderBy(pos => Vector2.Distance(pos, startPosition)).First();
                worm = new PerlinWorm(riverSettings, startPosition, closestWaterPos, noiseMap);
            }
            else
            {
                worm = new PerlinWorm(riverSettings, startPosition, noiseMap);
            }

            var position = worm.MoveLength(riverLength);
            return PlaceRiverTile(position);
        }

        float[,] PlaceRiverTile(List<Vector2> positons)
        {
            foreach (var pos in positons)
            {
                var tilePos = pos;
                if (tilePos.x < 0 || tilePos.x >= noiseMap.Length || tilePos.y < 0 || tilePos.y >= noiseMap.Length)
                    break;

                noiseMap[(int)tilePos.x, (int)tilePos.y] = minimas;

                if (bold && noiseMap[(int)tilePos.x, (int)tilePos.y] < maximas)
                {
                    try
                    {
                        noiseMap[(int)tilePos.x + 1, (int)tilePos.y] = minimas;
                    }
                    catch (Exception) { }
                    try
                    {
                        noiseMap[(int)tilePos.x, (int)tilePos.y + 1] = minimas;
                    }
                    catch (Exception) { }

                    try
                    {
                        noiseMap[(int)tilePos.x - 1, (int)tilePos.y] = minimas;
                    }
                    catch (Exception) { }
                    try
                    {
                        noiseMap[(int)tilePos.x, (int)tilePos.y - 1] = minimas;
                    }
                    catch (Exception) { }


                }
            }
            return noiseMap;
        }

    }
}
