using System;
using UnityEngine;

/// <summary>
/// Anchor is subTiles holding tile
/// Position is local index positoin of the subtile
/// </summary>
[Serializable]
public class MapTile
{
    [field: SerializeField] public Vector2Int Position { get; private set; }
    [field: SerializeField] public MapBlockData BlockData { get; private set; }
    [field: SerializeField] public Vector2Int Anchor { get; private set; }

    public bool IsOccupied => BlockData != null;

    public MapTile(Vector2Int position, MapBlockData blockData, Vector2Int anchor)
    {
        Position = position;
        BlockData = blockData;
        Anchor = anchor; 
    }

    public void SetData(MapBlockData blockData) => BlockData = blockData;

    public void SetData(MapBlockData blockData, Vector2Int anchor)
    {
        BlockData = blockData;
        Anchor = anchor;
    }
}
