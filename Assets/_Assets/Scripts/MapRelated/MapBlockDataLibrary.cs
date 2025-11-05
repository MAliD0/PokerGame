using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AYellowpaper.SerializedCollections;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName ="MapBlockLibrary", menuName = "Map/BlockLibrary")]
public class MapBlockDataLibrary : ScriptableObject
{
    public SerializedDictionary<string, MapBlockData> dataLibrary;

    [Button("Generate Library")]
    public void GenerateLibrary()
    {
        MapBlockData[] assets = Resources.LoadAll<MapBlockData>("Tiles");
        foreach (MapBlockData asset in assets)
        {
            if (!dataLibrary.ContainsKey(asset.GetItemID()))
            {
                dataLibrary.Add(asset.GetItemID(), asset);
            }
            else
            {
                Debug.LogWarning($"Item {asset.GetItemID()} already has instance");
            }
        }
    }

    public MapBlockData GetMapBlockData(string id)
    {
        dataLibrary.TryGetValue(id, out MapBlockData mapBlockData);
        return mapBlockData;
    }
}
