using AYellowpaper.SerializedCollections;
using System;
using UnityEngine;

namespace ProceduralGeneration
{
    [Serializable]
    public class TileData
    {
        public Sprite sprite;
        public RuleTile ruleTile;
        public SerializedDictionary<TileOptions, float> options;

        [Range(0, 1)]
        public float chance;

        public bool CheckConnectivity(TileOptions option)
        {
            return (options.ContainsKey(option));
        }
        public int GetNumberOfOptions()
        {
            return options.Count;
        }
    }

}
