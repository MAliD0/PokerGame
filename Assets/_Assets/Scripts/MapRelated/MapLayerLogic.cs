using AYellowpaper.SerializedCollections;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Слой данных. Поддерживает отрицательные координаты; границы опциональны.
/// Для мульти-блоков offsets ОБЯЗАТЕЛЬНО содержат (0,0).
/// </summary>
[Serializable]
public class MapLayerLogic
{
    public SerializedDictionary<Vector2Int, MapTile> LayerTiles;
    public SerializedDictionary<Vector2Int, int> anchorHp;

    public event Action<List<Vector2Int>, MapBlockData> onMapTilePlaced;
    public event Action<List<Vector2Int>, MapBlockType> onMapTileRemoved;
    public event Action<List<Vector2Int>, MapBlockData, int /*hp*/, int /*maxHp*/> onTileHealthChanged;

    private readonly MapBounds _bounds;

    // Старый конструктор (совместимость): 0..width-1, 0..height-1
    public MapLayerLogic(int mapWidth, int mapHeight)
        : this(new MapBounds(0, mapWidth - 1, 0, mapHeight - 1, useBounds: true)) { }

    // Новый гибкий конструктор
    public MapLayerLogic(MapBounds bounds)
    {
        _bounds = bounds;
        LayerTiles = new SerializedDictionary<Vector2Int, MapTile>();
        anchorHp = new SerializedDictionary<Vector2Int, int>();
    }

    public MapTile GetMapTile(Vector2Int pos) => LayerTiles.TryGetValue(pos, out var t) ? t : null;
    public bool IsTilePresented(Vector2Int pos) => LayerTiles.ContainsKey(pos);

    private bool EnsureZeroOffset(MapBlockData data)
    {
        if (!data.isMultiblock) return true;
        foreach (var off in data.tileOffsets)
            if (off == Vector2Int.zero) return true;

        Debug.LogError($"[MapLayerLogic] '{data?.name}' id={data?.GetItemID()} isMultiblock, но нет (0,0) в tileOffsets.");
        return false;
    }

    public bool CanBePlaced(Vector2Int pos, MapBlockData data)
    {
        if (data == null) return false;
        if (!EnsureZeroOffset(data)) return false;

        if (data.isMultiblock)
        {
            foreach (var off in data.tileOffsets)
            {
                var cell = pos + off;
                if (!_bounds.Contains(cell))
                {
                    Debug.LogWarning($"[Place] {cell} вне допустимых границ");
                    return false;
                }
                if (IsTilePresented(cell))
                {
                    Debug.LogWarning($"[Place] {cell} занята");
                    return false;
                }
            }
            return true;
        }

        if (!_bounds.Contains(pos))
        {
            Debug.LogWarning($"[Place] {pos} вне допустимых границ");
            return false;
        }
        if (IsTilePresented(pos))
        {
            Debug.LogWarning($"[Place] {pos} занята");
            return false;
        }
        return true;
    }

    private List<Vector2Int> CollectGroupCells(Vector2Int anchor, MapBlockData data)
    {
        var list = new List<Vector2Int>();
        if (data.isMultiblock)
        {
            foreach (var off in data.tileOffsets)
                list.Add(anchor + off);
        }
        else list.Add(anchor);
        return list;
    }

    public bool PlaceBlock(Vector2Int pos, MapBlockData data)
    {
        if (!CanBePlaced(pos, data)) return false;

        var group = CollectGroupCells(pos, data);
        foreach (var cell in group)
        {
            if (LayerTiles.ContainsKey(cell)) continue;
            LayerTiles.Add(cell, new MapTile(cell, data, pos /*anchor*/));
        }

        if (data.breakable)
            SetHealth(pos, data, Mathf.Max(1, data.maxHealth), fireEvent: false);

        onMapTilePlaced?.Invoke(group, data);

        if (data.breakable)
            onTileHealthChanged?.Invoke(group, data, GetHealth(pos), data.maxHealth);

        return true;
    }

    public void RemoveTile(Vector2Int anyCellInGroup)
    {
        if (!LayerTiles.TryGetValue(anyCellInGroup, out var tile)) return;

        var data = tile.BlockData;
        var anchor = tile.Anchor;
        var group = CollectGroupCells(anchor, data);

        foreach (var cell in group) LayerTiles.Remove(cell);
        
        anchorHp.Remove(anchor);
        onMapTileRemoved?.Invoke(group, data.mapBlockType);
    }

    public int GetHealth(Vector2Int anchor)
    => anchorHp.TryGetValue(anchor, out var hp) ? hp : -1;

    public void SetHealth(Vector2Int anchor, MapBlockData data, int hp, bool fireEvent = true)
    {
        if (data == null || !data.breakable) return;
        
        hp = Mathf.Clamp(hp, 0, Mathf.Max(1, data.maxHealth));

        if (anchorHp.ContainsKey(anchor))
        {
            anchorHp[anchor] = hp;
        }
        else
        {
            anchorHp.Add(anchor, hp);
        }

        if (fireEvent)
            onTileHealthChanged?.Invoke((List<Vector2Int>)EnumerateGroupCells(anchor, data), data, hp, data.maxHealth);
    }

    public bool Damage(Vector2Int anchor, MapBlockData data, int amount)
    {
        if (data == null || !data.breakable) return false;
        int maxHp = Mathf.Max(1, data.maxHealth);
        int cur = GetHealth(anchor);
        if (cur < 0) cur = maxHp;

        int next = Mathf.Clamp(cur - Mathf.Max(1, amount), 0, maxHp);
        anchorHp[anchor] = next;
        onTileHealthChanged?.Invoke((List<Vector2Int>)EnumerateGroupCells(anchor, data), data, next, maxHp);
        return next <= 0;
    }

    // утилиты
    private IEnumerable<Vector2Int> EnumerateGroupCells(Vector2Int anchor, MapBlockData data)
    {
        if (data.isMultiblock)
            foreach (var off in data.tileOffsets) yield return anchor + off;
        else
            yield return anchor;
    }
}
