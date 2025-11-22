using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public struct V2I : INetworkSerializable
{
    public int x;
    public int y;

    public V2I(int x, int y) { this.x = x; this.y = y; }
    public V2I(Vector2Int v) { x = v.x; y = v.y; }
    public Vector2Int ToVector2Int() => new Vector2Int(x, y);

    public static implicit operator V2I(Vector2Int v) => new V2I(v);
    public static implicit operator Vector2Int(V2I v) => v.ToVector2Int();

    public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
    {
        s.SerializeValue(ref x);
        s.SerializeValue(ref y);
    }
}
