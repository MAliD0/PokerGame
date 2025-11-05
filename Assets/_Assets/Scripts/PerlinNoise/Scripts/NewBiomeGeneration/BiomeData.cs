
namespace ProceduralGeneration
{
    using UnityEngine;
    using System.Collections.Generic;

    [CreateAssetMenu(fileName = "New Biome", menuName = "Biome Data")]
    public class BiomeData : ScriptableObject
    {
        public string biomeName;
        public float minMoisture;
        public float maxMoisture;

        public float minTemperature;
        public float maxTemperature;

        public List<TerrainType> terrainTypes;

        public bool isInTempRange(float value)
        {
            return value >= minTemperature && value <= maxTemperature;
        }
        public bool isInMoistureRange(float value)
        {
            return value >= minMoisture && value <= maxMoisture;
        }
    }

}
