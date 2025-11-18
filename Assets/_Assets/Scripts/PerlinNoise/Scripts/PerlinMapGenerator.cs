using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using static Unity.Collections.AllocatorManager;
using NoiseGeneration;
using Unity.Collections.LowLevel.Unsafe;

namespace ProceduralGeneration
{
    public class PerlinMapGenerator : MonoBehaviour
    {
        public enum NoiseMode { OneDimensionMap, PerlinMap, IslandMap, PerlinWorm, WorleyNoise, ScaledPerlin }
        public enum DrawMode { ColorMap, NoiseMap, TileMap, BiomsGeneration }

        [SerializeField] private NoiseMode noiseMode;
        [SerializeField] private DrawMode drawMode;
        #region NoiseSettings
        [SerializeField] NoiseSettings noiseSettings;
        [ShowInInspector]
        public static int mapChunkSize = 241;
        [SerializeField] int seed;
        #endregion

        [Space]
        [SerializeField] bool autoUpdate;


        [SerializeField] MapDisplay mapDisplay;
        [ShowIf("drawMode", DrawMode.ColorMap)]
        public TerrainType[] terrainTypes;

        #region IslandSettings
        [FoldoutGroup("Island Settings")]
        [HorizontalGroup("Island Settings/island")]
        [SerializeField] int radius;

        [HorizontalGroup("Island Settings/island")]
        [SerializeField] float strength;
        #endregion
        #region PerlinWorm
        [FoldoutGroup("Perlin Worm")]
        [HorizontalGroup("Perlin Worm/Trashold")]
        [Range(0, 1)][SerializeField] float minTrashold;
        [HorizontalGroup("Perlin Worm/Trashold")]
        [Range(0, 1)][SerializeField] float maxTrashold;
        #endregion
        #region WorleyNoise
        [FoldoutGroup("Worley Noise")]
        [HorizontalGroup("Worley Noise/Values")]
        [Range(0, 10)][SerializeField] int pointsNumber;
        #endregion

        #region TileMap
        [FoldoutGroup("Tile Map")][HorizontalGroup("Tile Map/Ref")][SerializeField] WorldMapManager worldMapManager;
        [FoldoutGroup("Tile Map")][HorizontalGroup("Tile Map/Ref")][SerializeField] TilesLibrary tilesLibrary;
        [FoldoutGroup("Tile Map")]
        [SerializeField] TileType[] tileTypes;
        #endregion
        #region BiomsGenerator
        [FoldoutGroup("Bioms Map")]
        [FoldoutGroup("Bioms Map")][SerializeField] NoiseSettings temperatureNoiseSettings;
        [FoldoutGroup("Bioms Map")][SerializeField] NoiseSettings moistureNoiseSettings;
        [FoldoutGroup("Bioms Map")][SerializeField] List<BiomeData> biomes;
        #endregion

        #region AdditionalGenerations
        [FoldoutGroup("AdditionalGeneration")] List<AdditionalGeneratingObject> additionalGeneratingObjects;

        #endregion

        #region Maximas/Minimas
        [FoldoutGroup("MaximasMinimas")]
        [FoldoutGroup("MaximasMinimas")][SerializeField] RiverGenerator riverGenerator;
        [FoldoutGroup("MaximasMinimas")][SerializeField] bool generateRivers;
        [FoldoutGroup("MaximasMinimas")][SerializeField] bool showMinimas;
        [FoldoutGroup("MaximasMinimas")][SerializeField] bool showMaximas;
        [FoldoutGroup("MaximasMinimas")][SerializeField][Range(0, 1f)] float maximas;
        [FoldoutGroup("MaximasMinimas")][SerializeField][Range(0, 1f)] float minimas;
        [FoldoutGroup("MaximasMinimas")]
        [SerializeField] int numberOfPoints;
        #endregion

        private float startPos;


        struct AdditionalGeneratingObject
        {
            public NoiseSettings noiseSettings;
            public MapBlockData mapBlockData;

            public NoiseMode noiseMode;

            [FoldoutGroup("Settings")] public float minValue;
            [FoldoutGroup("Settings")] public float maxValue;

            [FoldoutGroup("Island Settings")]
            [HorizontalGroup("Island Settings/island")]
            [SerializeField] public int radius;

            [HorizontalGroup("Island Settings/island")]
            [SerializeField] public float strength;
        }


        [Button]
        public void GenerateMap()
        {
            float[,] noiseMap = Noise.GenerateNoiseMap(noiseSettings.mapWidth, noiseSettings.mapHeight, seed, noiseSettings.scale, noiseSettings.octaves, noiseSettings.persistance, noiseSettings.lacunarity, noiseSettings.offset);
            Color[] colourMap = new Color[noiseSettings.mapWidth * noiseSettings.mapHeight];

            mapDisplay.DisablePlane(false);

            switch (noiseMode)
            {
                case NoiseMode.OneDimensionMap:
                    mapDisplay.DisablePlane(true);
                    break;
                case NoiseMode.IslandMap:
                    noiseMap = Noise.IslandNoise(noiseSettings, noiseMap, radius, strength);
                    mapDisplay.DrawTexture(TextureGenerator.TextureFromHeightMap(noiseMap));
                    break;
                case NoiseMode.PerlinWorm:
                    noiseMap = Noise.IslandNoise(noiseSettings, noiseMap, radius, strength);
                    noiseMap = Noise.PerlinWorm(noiseSettings, noiseMap, minTrashold);
                    break;
                case NoiseMode.WorleyNoise:
                    noiseMap = Noise.GenerateOrganicVoronoi(noiseSettings, pointsNumber);
                    break;
                case NoiseMode.ScaledPerlin:
                    noiseMap = Noise.GenerateNoiseMap(noiseSettings.mapWidth, noiseSettings.mapHeight, seed, noiseSettings.scale, noiseSettings.octaves, noiseSettings.persistance, noiseSettings.lacunarity, noiseSettings.offset, noiseSettings.pow);
                    break;
            }

            if (generateRivers)
            {
                riverGenerator.SetupRiverGenerator(noiseMap, minimas, maximas);
                riverGenerator.GenerateRivers();
            }

            switch (drawMode)
            {
                case DrawMode.ColorMap:
                    for (int y = 0; y < noiseSettings.mapHeight; y++)
                    {
                        for (int x = 0; x < noiseSettings.mapWidth; x++)
                        {
                            float currentHeight = noiseMap[x, y];
                            bool foundColor = false;
                            for (int i = 0; i < terrainTypes.Length; i++)
                            {
                                if (currentHeight <= terrainTypes[i].height)
                                {
                                    colourMap[y * noiseSettings.mapWidth + x] = terrainTypes[i].color;
                                    foundColor = true;
                                    break;
                                }
                            }
                            if (!foundColor)
                                colourMap[y * noiseSettings.mapWidth + x] = terrainTypes[terrainTypes.Length - 1].color;
                        }
                    }
                    mapDisplay.DrawTexture(TextureGenerator.TextureFromColourMap(colourMap, noiseSettings.mapWidth, noiseSettings.mapHeight));
                    break;
                case DrawMode.NoiseMap:
                    mapDisplay.DrawTexture(TextureGenerator.TextureFromHeightMap(noiseMap));
                    break;
                case DrawMode.TileMap:
                    mapDisplay.DisablePlane(true);
                    
                    worldMapManager.ClearTilemaps();

                    var tilePositions = new List<Vector3Int>();
                    var tilePosition = new Vector3Int();

                    var tileVariant = new List<TileBase>();

                    for (int x = 0; x < noiseSettings.mapWidth; x++)
                    {
                        for (int y = 0; y < noiseSettings.mapHeight; y++)
                        {
                            tilePosition = new Vector3Int(x - (noiseSettings.mapWidth / 2), y - (noiseSettings.mapHeight / 2));
                            tilePositions.Add(tilePosition);
                         
                            float currentHeight = noiseMap[x, y];
                            for (int i = 0; i < tileTypes.Length; i++)
                            {
                                if (currentHeight <= tileTypes[i].height)
                                {
                                    tileVariant.Add(tileTypes[i].tile);
                                    worldMapManager.SetTileRequestServerRpc((Vector2Int)tilePosition, tileTypes[i].tile.name);
                                    
                                    //tilemap.SetTile(new Vector3Int(x, y), tileTypes[i].tile);
                                    break;
                                }
                            }
                        }
                    }
                    

                    break;
                case DrawMode.BiomsGeneration:
                    float[,] moistureNoiseMap = Noise.GenerateNoiseMap(moistureNoiseSettings.mapWidth, moistureNoiseSettings.mapHeight, seed, moistureNoiseSettings.scale, moistureNoiseSettings.octaves, moistureNoiseSettings.persistance, moistureNoiseSettings.lacunarity, moistureNoiseSettings.offset);
                    float[,] temperatureNoiseMap = Noise.GenerateNoiseMap(temperatureNoiseSettings.mapWidth, temperatureNoiseSettings.mapHeight, seed, temperatureNoiseSettings.scale, temperatureNoiseSettings.octaves, temperatureNoiseSettings.persistance, temperatureNoiseSettings.lacunarity, temperatureNoiseSettings.offset);

                    TerrainType[,] biomes = new TerrainType[noiseSettings.mapWidth, noiseSettings.mapHeight];

                    for (int y = 0; y < noiseSettings.mapHeight; y++)
                    {
                        for (int x = 0; x < noiseSettings.mapWidth; x++)
                        {
                            float heightValue = noiseMap[x, y];
                            float temperatureValue = temperatureNoiseMap[x, y];
                            float moistureValue = moistureNoiseMap[x, y];

                            BiomeData biome = GetBiomeAtPoint(heightValue, temperatureValue, moistureValue);

                            TerrainType terrainType = biome.terrainTypes
                                .OrderByDescending(t => t.height)
                                .First(t => heightValue >= t.height);

                            colourMap[y * noiseSettings.mapWidth + x] = terrainType.color;
                        }
                    }

                    mapDisplay.DrawTexture(TextureGenerator.TextureFromColourMap(colourMap, noiseSettings.mapWidth, noiseSettings.mapHeight));
                    break;
            }

            if (showMaximas)
            {
                var result = MathFunc.FindLocalMaxima(noiseMap);
                result = result.Where(pos => noiseMap[pos.x, pos.y] > maximas).OrderBy(pos => noiseMap[pos.x, pos.y]).Take(numberOfPoints).ToList();
                foreach (var item in result)
                {
                    colourMap[item.y * noiseSettings.mapWidth + item.x] = new Color(0, 0, 255);
                    Debug.Log(item);
                }

                mapDisplay.DrawTexture(TextureGenerator.TextureFromColourMap(colourMap, noiseSettings.mapWidth, noiseSettings.mapHeight));
            }
            if (showMinimas)
            {
                var result = MathFunc.FindLocalMinima(noiseMap);
                result = result.Where(pos => noiseMap[pos.x, pos.y] < minimas && noiseMap[pos.x, pos.y] != 0).OrderBy(pos => noiseMap[pos.x, pos.y]).Take(numberOfPoints).ToList();
                foreach (var item in result)
                {
                    colourMap[item.y * noiseSettings.mapWidth + item.x] = new Color(255, 0, 0);
                    Debug.Log(item);
                }
                mapDisplay.DrawTexture(TextureGenerator.TextureFromColourMap(colourMap, noiseSettings.mapWidth, noiseSettings.mapHeight));
            }
        }

        [Button]
        private void GenerateAdditionalObject(AdditionalGeneratingObject additionalGeneratingObject)
        {
            NoiseMode noiseMode = additionalGeneratingObject.noiseMode;
            NoiseSettings noiseSettings = additionalGeneratingObject.noiseSettings;

            float[,] noiseMap = Noise.GenerateNoiseMap(noiseSettings.mapWidth, noiseSettings.mapHeight, seed, noiseSettings.scale, noiseSettings.octaves, noiseSettings.persistance, noiseSettings.lacunarity, noiseSettings.offset);

            switch (noiseMode)
            {
                case NoiseMode.IslandMap:
                    noiseMap = Noise.IslandNoise(noiseSettings, noiseMap, additionalGeneratingObject.radius, additionalGeneratingObject.strength);
                    mapDisplay.DrawTexture(TextureGenerator.TextureFromHeightMap(noiseMap));
                    break;
                case NoiseMode.PerlinWorm:
                    noiseMap = Noise.PerlinWorm(noiseSettings, noiseMap, minTrashold);
                    break;
                case NoiseMode.WorleyNoise:
                    noiseMap = Noise.GenerateOrganicVoronoi(noiseSettings, pointsNumber);
                    break;
                case NoiseMode.ScaledPerlin:
                    noiseMap = Noise.GenerateNoiseMap(noiseSettings.mapWidth, noiseSettings.mapHeight, seed, noiseSettings.scale, noiseSettings.octaves, noiseSettings.persistance, noiseSettings.lacunarity, noiseSettings.offset, noiseSettings.pow);
                    break;
            }

            switch (drawMode)
            {
                case DrawMode.TileMap:

                    var tilePosition = new Vector3Int();
                    var tileVariant = additionalGeneratingObject.mapBlockData;

                    for (int x = 0; x < noiseSettings.mapWidth; x++)
                    {
                        for (int y = 0; y < noiseSettings.mapHeight; y++)
                        {
                            tilePosition = new Vector3Int(x - (noiseSettings.mapWidth / 2), y - (noiseSettings.mapHeight / 2));
                            float currentHeight = noiseMap[x, y];

                            if(currentHeight > additionalGeneratingObject.minValue && currentHeight < additionalGeneratingObject.maxValue)
                            {
                                worldMapManager.SetTileRequestServerRpc((Vector2Int)tilePosition, additionalGeneratingObject.mapBlockData.GetItemID());
                            }
                        }
                    }

                break;
            }
        }

        public BiomeData GetBiomeAtPoint(float height, float temperature, float moisture)
        {
            foreach (var biome in biomes)
            {
                if (temperature >= biome.minTemperature &&
                    temperature <= biome.maxTemperature &&
                    moisture >= biome.minMoisture &&
                    moisture <= biome.maxMoisture)
                {
                    return biome;
                }
            }
            return biomes[0]; // Fallback biome
        }

        private void OnValidate()
        {
            if (autoUpdate)
            {
                GenerateMap();
            }
        }

        private void OnDrawGizmos()
        {
            if (noiseMode == NoiseMode.OneDimensionMap)
            {
                float xoff = startPos + 0.02f;
                float prevY = 0;
                for (int x = 1; x < noiseSettings.mapWidth; x++)
                {
                    float firstFunc = (float)MathFunc.Map(Mathf.Sin(Mathf.Deg2Rad * xoff * 10), -1, 1.0, 0.0, noiseSettings.mapHeight);

                    float secondFunc = (float)MathFunc.Map(Noise.GenerateNoise(seed, xoff), 0, 1, -noiseSettings.scale, noiseSettings.scale);
                    float y = firstFunc + secondFunc;

                    Gizmos.DrawLine(new Vector3(x - 1, prevY), new Vector3(x, y));
                    prevY = y;
                    xoff += 0.02f;
                }
                startPos += 0.02f;
            }
        }
    
    }

    

    [Serializable]
    public struct HeightBioms
    {
        public string name;
        public float height;
        public List<TerrainType> moistureBased;
    }

    [Serializable]
    public struct TerrainType
    {
        public string name;
        public float height;
        public Color color;
        public float heightMultiplier;
    }
    [Serializable]
    public struct TileType
    {
        public float height;
        public TileBase tile;
    }

}
