using AYellowpaper.SerializedCollections;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using TileData = ProceduralGeneration.TileData;

[CreateAssetMenu(fileName ="Bioms")]
public class TilesLibrary : ScriptableObject
{
    [ShowInInspector]
    public SerializedDictionary<TileOptions, ProceduralGeneration.TileData> tileDatas;

    public List<ProceduralGeneration.TileData> TileOptionsToTileDatas(List<TileOptions> tileOptions)
    {
        List<ProceduralGeneration.TileData> datas = new List<ProceduralGeneration.TileData>();
        foreach (TileOptions tileOption in tileOptions)
        {
            datas.Add(tileDatas[tileOption]);
        }
        return datas;
    }
}
public enum TileOptions
{
    Forest,
    Swamp,
    Plains,
    Mountains,
    Lake,
    Field,
    Hill
}