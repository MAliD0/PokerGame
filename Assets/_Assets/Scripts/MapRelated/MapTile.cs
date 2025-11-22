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
    [field: SerializeField] public Vector2Int ParentTile { get; private set; }
    [field: SerializeField] public Vector2Int SubtileAnchor { get; private set; }
    [field: SerializeField] public Vector2Int TileAnchor { get; private set; }
    public bool IsOccupied => BlockData != null;

    public MapTile(Vector2Int position, MapBlockData blockData, Vector2Int anchor, Vector2Int subtileAnchor, Vector2Int tileAnchor)
    {
        Position = position;
        BlockData = blockData;
        ParentTile = anchor;
        SubtileAnchor = subtileAnchor;
        TileAnchor = tileAnchor;
    }

    public void SetData(MapBlockData blockData) => BlockData = blockData;

    public void SetData(MapBlockData blockData, Vector2Int anchor)
    {
        BlockData = blockData;
        ParentTile = anchor;
    }
}
