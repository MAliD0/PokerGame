using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System;
using Unity.Netcode;
using Sirenix.OdinInspector;
using AYellowpaper.SerializedCollections;
using System.Linq;

/// <summary>
/// Только визуал: ставим тайлы, держим локальные кэши клетка→GO и id→GO (для не-сетевых).
/// НИКАКОЙ сетевой логики здесь нет.
/// </summary>

[Serializable]
public class MapLayerGraphics : NetworkBehaviour
{
    private MapLayerLogic layerLogic;

    [Header("Tilemap visuals")]
    public Tilemap LayerVisualsTiles;

    [Header("Runtime caches")]
    [ShowInInspector, ReadOnly] public SerializedDictionary<Vector2Int, SerializedDictionary<Vector2Int, GameObject>> CellToGO;
    private readonly Dictionary<string, GameObject> _netlessById = new(); // только не-сетевые

    public void Init(MapLayerLogic logic)
    {
        layerLogic = logic;
        CellToGO = new SerializedDictionary<Vector2Int, SerializedDictionary<Vector2Int, GameObject>>();

        layerLogic.onMapTilePlaced += OnMapTilePlaced;
        layerLogic.onMapTileRemoved += OnMapTileRemoved;
        layerLogic.onAllTilesRemoved += OnAllTilesRemoved;
    }

    private void OnMapTilePlaced(Dictionary<Vector2Int,HashSet<Vector2Int>> positions, MapBlockData data)
    {
        // Рисуем только ТАЙЛЫ. GO спавнятся/биндятся менеджером через RPC.
        if (data.mapBlockType == MapBlockType.Tile && data.tile != null)
        {
            foreach (var pos in positions.Keys)
                LayerVisualsTiles.SetTile(new Vector3Int(pos.x, pos.y, 0), data.tile);
        }
    }

    private void OnMapTileRemoved(Dictionary<Vector2Int, HashSet<Vector2Int>> positions, MapBlockType type)
    {
        // Для тайлов — снимаем визуалы; для GO — ничего (анбинд делает менеджер).
        if (type == MapBlockType.Tile)
        {
            foreach (var pos in positions)
                LayerVisualsTiles.SetTile(new Vector3Int(pos.Key.x, pos.Key.y, 0), null);
        }
    }
    
    //this method will not be used for server purposes
    private void OnAllTilesRemoved()
    {
        LayerVisualsTiles.ClearAllTiles();

        if(CellToGO == null) return;

        foreach(Vector2Int key in CellToGO.Keys)
        {
            foreach(Vector2Int subtileKey in CellToGO[key].Keys)
            {
                if(!CellToGO.ContainsKey(key)) continue;

                if(!CellToGO[key].ContainsKey(subtileKey)) continue;

                if(CellToGO[key].TryGetValue(subtileKey, out GameObject go))
                {
                    Destroy(go);
                    Debug.Log("Destroy object");
                }
            }
        }
        CellToGO.Clear();
    }

    // ------------------------ API для менеджера ------------------------

    public void BindObject(Vector2Int anchor ,List<Vector2Int> cells, GameObject go, string netlessId = null)
    {
        if(go == null || netlessId == null)
        {
            UnbindByCells(anchor, cells.ToArray());
        }
        else
        {
            if (netlessId != null)
                _netlessById[netlessId] = go;

            if(!CellToGO.ContainsKey(anchor))
                CellToGO.Add(anchor, new SerializedDictionary<Vector2Int, GameObject>());

            foreach (var cell in cells)
                CellToGO[anchor].Add(cell, go);
        }
    }
    public void UnbindByCells(Vector2Int anchor, Vector2Int[] subtiles, bool destroyNonNetworked = false)
    {
        HashSet<Vector2Int> subtilesToRemove = new HashSet<Vector2Int>();

        if (CellToGO.TryGetValue(anchor, out var subtilesWithGO))
        {
            foreach(var subtile in subtilesWithGO.Keys)
            {
                subtilesToRemove.Add(subtile);        
            }


            foreach(var subtileToRemove in subtilesToRemove)
                CellToGO[anchor].Remove(subtileToRemove);
            
            if(CellToGO[anchor].Count == 0)
                CellToGO.Remove(anchor);
        }
    }

    public void UnbindByCells(DictEntry[] cells, bool destroyNonNetworked = false)
    {
        GameObject any = null;
        var tiles = DictEntry.DictEntryToDictionary(cells.ToList());

        Dictionary<Vector2Int, HashSet<Vector2Int>> subtilesToRemove = new Dictionary<Vector2Int, HashSet<Vector2Int>>();
        HashSet<Vector2Int> tilesToRemove = new HashSet<Vector2Int>();

        foreach (var c in tiles.Keys)
        {
            if (CellToGO.TryGetValue(c, out var subtilesWithGO))
            {
                foreach(var subtile in subtilesWithGO.Keys)
                {
                    if(CellToGO[c].TryGetValue(subtile, out GameObject go))
                        any = go;

                    if(!subtilesToRemove.TryGetValue(c, out var subtileList))
                    {
                        HashSet<Vector2Int> newSubtileList = new HashSet<Vector2Int>();
                        newSubtileList.Add(subtile);
                        subtilesToRemove.Add(c, newSubtileList);
                    }
                    else
                        subtileList.Add(subtile);
                }
            }
            if(CellToGO[c].Count == 0)
                tilesToRemove.Add(c);
        }

        foreach(var tile in subtilesToRemove.Keys)
        {
            foreach(var subtile in subtilesToRemove[tile])
                CellToGO.Remove(subtile);
        }
        foreach(var tile in tilesToRemove)
            CellToGO.Remove(tile);


        if (destroyNonNetworked && any != null && !any.TryGetComponent<NetworkObject>(out _))
            Destroy(any);
    }

    public void UnbindById(string netlessId)
    {
        if (!_netlessById.TryGetValue(netlessId, out var go) || go == null)
            return;

            // временный список для ключей, где нужно будет удалить
            List<Vector2Int> outerKeysToClear = new List<Vector2Int>();

            foreach (var outer in CellToGO)
            {
                var inner = outer.Value;

                // временный список ключей внутреннего словаря на удаление
                List<Vector2Int> innerKeysToRemove = new List<Vector2Int>();

                foreach (var kvp in inner)
                {
                    if (kvp.Value == go)
                        innerKeysToRemove.Add(kvp.Key);
                }

                // удаляем найденные ключи внутреннего словаря
                foreach (var key in innerKeysToRemove)
                    inner.Remove(key);

                // если внутренний словарь теперь пуст — можно удалить верхний ключ
                if (inner.Count == 0)
                    outerKeysToClear.Add(outer.Key);
            }

            // удаляем пустые внешние пары
            foreach (var key in outerKeysToClear)
                CellToGO.Remove(key);
                        
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
