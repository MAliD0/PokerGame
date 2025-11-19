using System;
using UnityEngine;

[Serializable]
public class MapSubTile
{
    [field: SerializeField] public Vector2Int LocalPosition { get; private set; }
    [field: SerializeField] public MapBlockData BlockData { get; private set; }

    public bool IsOccupied => BlockData != null;

    public MapSubTile(Vector2Int localPosition, MapBlockData blockData)
    {
        LocalPosition = localPosition;
        BlockData = blockData;
    }

    public void SetData(MapBlockData blockData) => BlockData = blockData;

    public void SetData(MapBlockData blockData, Vector2Int anchor)
    {
        BlockData = blockData;
    }

}
