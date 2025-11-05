using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System;
using Unity.Netcode;
using Sirenix.OdinInspector;
using AYellowpaper.SerializedCollections;

/// <summary>
/// Только визуал: ставим тайлы, держим локальные кэши клетка→GO и id→GO (для не-сетевых).
/// НИКАКОЙ сетевой логики здесь нет.
/// </summary>
public class MapLayerGraphics : NetworkBehaviour
{
    private MapLayerLogic layerLogic;

    [Header("Tilemap visuals")]
    public Tilemap LayerVisualsTiles;

    [Header("Runtime caches")]
    [ShowInInspector, ReadOnly] public SerializedDictionary<Vector2Int, GameObject> CellToGO;
    private readonly Dictionary<string, GameObject> _netlessById = new(); // только не-сетевые

    public void Init(MapLayerLogic logic)
    {
        layerLogic = logic;
        CellToGO = new SerializedDictionary<Vector2Int, GameObject>();

        layerLogic.onMapTilePlaced += OnMapTilePlaced;
        layerLogic.onMapTileRemoved += OnMapTileRemoved;
    }

    private void OnMapTilePlaced(List<Vector2Int> positions, MapBlockData data)
    {
        // Рисуем только ТАЙЛЫ. GO спавнятся/биндятся менеджером через RPC.
        if (data.mapBlockType == MapBlockType.Tile && data.tile != null)
        {
            foreach (var pos in positions)
                LayerVisualsTiles.SetTile(new Vector3Int(pos.x, pos.y, 0), data.tile);
        }
    }

    private void OnMapTileRemoved(List<Vector2Int> positions, MapBlockType type)
    {
        // Для тайлов — снимаем визуалы; для GO — ничего (анбинд делает менеджер).
        if (type == MapBlockType.Tile)
        {
            foreach (var pos in positions)
                LayerVisualsTiles.SetTile(new Vector3Int(pos.x, pos.y, 0), null);
        }
    }

    // ------------------------ API для менеджера ------------------------

    public void BindObject(List<Vector2Int> cells, GameObject go, string netlessId = null)
    {
        if (netlessId != null)
            _netlessById[netlessId] = go;

        foreach (var c in cells)
            CellToGO[c] = go;
    }

    public void UnbindByCells(List<Vector2Int> cells, bool destroyNonNetworked = false)
    {
        GameObject any = null;
        foreach (var c in cells)
        {
            if (CellToGO.TryGetValue(c, out var go))
            {
                any = go;
                CellToGO.Remove(c);
            }
        }

        if (destroyNonNetworked && any != null && !any.TryGetComponent<NetworkObject>(out _))
            Destroy(any);
    }

    public void UnbindById(string netlessId)
    {
        if (!_netlessById.TryGetValue(netlessId, out var go) || go == null)
            return;

        // вычищаем все клетки, где этот GO привязан
        var toRemove = new List<Vector2Int>();
        foreach (var kv in CellToGO)
            if (kv.Value == go) toRemove.Add(kv.Key);

        foreach (var c in toRemove) CellToGO.Remove(c);

        _netlessById.Remove(netlessId);
        Destroy(go);
    }

    [Button]
    public void ClearTilemap()
    {
        LayerVisualsTiles.ClearAllTiles();

        // очистка кэшей и уничтожение не-сетевых GO
        foreach (var kv in _netlessById)
            if (kv.Value) Destroy(kv.Value);
        _netlessById.Clear();
        CellToGO.Clear();
    }
}
