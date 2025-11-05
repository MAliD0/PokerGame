using System;
using UnityEngine;

[Serializable]
public struct MapBounds
{
    public bool UseBounds;
    public int MinX, MaxX, MinY, MaxY;

    public MapBounds(int minX, int maxX, int minY, int maxY, bool useBounds = true)
    {
        MinX = minX; MaxX = maxX; MinY = minY; MaxY = maxY; UseBounds = useBounds;
    }

    public bool Contains(Vector2Int p)
    {
        if (!UseBounds) return true;
        return p.x >= MinX && p.x <= MaxX && p.y >= MinY && p.y <= MaxY;
    }
}
