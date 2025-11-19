using AYellowpaper.SerializedCollections;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEditor.PlayerSettings;

/// <summary>
/// Слой данных. Поддерживает отрицательные координаты; границы опциональны.
/// Для мульти-блоков offsets ОБЯЗАТЕЛЬНО содержат (0,0).
/// </summary>
[Serializable]
public class MapLayerLogic
{
    //public SerializedDictionary<Vector2Int, MapTile> LayerTiles;

    // maps anchorTile cell -> (local anchorSubtile subtileWorldPosition -> MapSubTile)
    public SerializedDictionary<Vector2Int, SerializedDictionary<Vector2Int, MapTile>> LayerTiles;

    public SerializedDictionary<Vector2Int, SerializedDictionary<Vector2Int,int>> anchorHp;

    public event Action<Dictionary<Vector2Int, HashSet<Vector2Int>>, MapBlockData> onMapTilePlaced;
    public event Action<Dictionary<Vector2Int, HashSet<Vector2Int>>, MapBlockType> onMapTileRemoved;
    public event Action<Dictionary<Vector2Int, HashSet<Vector2Int>>, MapBlockData, int /*hp*/, int /*maxHp*/> onTileHealthChanged;

    private readonly MapBounds _bounds;

    // size of a anchorSubtile in world units
    private float cellSize = 0.125f;

    // number of subtiles per one whole anchorTile (1.0 / cellSize)
    private const int SubtilesPerCell = 8;

    // Старый конструктор (совместимость): 0..width-1, 0..height-1
    public MapLayerLogic(int mapWidth, int mapHeight)
        : this(new MapBounds(0, mapWidth - 1, 0, mapHeight - 1, useBounds: true)) { }

    // Новый гибкий конструктор
    public MapLayerLogic(MapBounds bounds)
    {
        _bounds = bounds;
        //LayerTiles = new SerializedDictionary<Vector2Int, MapTile>();
        
        LayerTiles = new SerializedDictionary<Vector2Int, SerializedDictionary<Vector2Int, MapTile>>();
        anchorHp = new SerializedDictionary<Vector2Int, SerializedDictionary<Vector2Int, int>>();
    }

    public MapTile GetMapTile(Vector2 pos)
    {
        Vector2Int tile = UtillityMath.VectorToVectorInt(pos);
        Vector2Int subtile = WorldToLocalSubtile(pos);

        return GetMapTile(tile, subtile);
    }
    public MapTile GetMapTile(Vector2Int tile, Vector2Int subtile)
    {
        if (LayerTiles.ContainsKey(tile))
        {
            LayerTiles.TryGetValue(tile, out var inner);
            if (inner != null)
            {
                LayerTiles[tile].TryGetValue(subtile, out MapTile value);
                return value;
            }
        }
        return null;
    }
    public bool IsTilePresented(Vector2 subtileWorldPosition){

        Vector2Int tile = UtillityMath.VectorToVectorInt(subtileWorldPosition);
        Vector2Int subTile = WorldToLocalSubtile(subtileWorldPosition);

        if (LayerTiles.ContainsKey(tile))
        {
            LayerTiles.TryGetValue(tile, out var inner);
            if (inner != null)
            {
                return LayerTiles[tile].TryGetValue(subTile, out MapTile value);
            }
        }
        return false;
    } 

    // safe check for a anchorSubtile at given cell and local anchorSubtile coordinate (0..7)
    public bool IsSubTilePresented(Vector2Int pos, Vector2Int subtilePos)
    {
        if (!LayerTiles.ContainsKey(pos)) return false;

        if (!LayerTiles.TryGetValue(pos, out var inner)) return false;

        return inner.ContainsKey(subtilePos);
    }

    // Convert world position to integral anchorTile cell (consistent with UtillityMath.VectorToVectorInt)
    public Vector2Int WorldToCell(Vector2 worldPos)
    {
        // use project's helper if desired; this is a standard floor-based conversion
        int cx = Mathf.FloorToInt(worldPos.x);
        int cy = Mathf.FloorToInt(worldPos.y);
        return new Vector2Int(cx, cy);
    }

    // Convert world position to local anchorSubtile index inside its cell (0..SubtilesPerCell-1)
    public Vector2Int WorldToLocalSubtile(Vector2 worldPos)
    {
        var cell = WorldToCell(worldPos);
        float relX = worldPos.x - cell.x; // in [0,1)
        float relY = worldPos.y - cell.y; // in [0,1)
        int localX = Mathf.FloorToInt(relX / cellSize);
        int localY = Mathf.FloorToInt(relY / cellSize);
        // Clamp just in case of floating precision edge cases
        localX = Mathf.Clamp(localX, 0, SubtilesPerCell - 1);
        localY = Mathf.Clamp(localY, 0, SubtilesPerCell - 1);
        return new Vector2Int(localX, localY);
    }

    // returns the anchor anchorTile cell and local anchorSubtile index (the clicked anchorSubtile — used as the middle of the footprint)
    public (Vector2Int tileCell, Vector2Int localSubtile) GetAnchorFromClick(Vector2 clickWorldPos)
    {
        Vector2Int baseCell = WorldToCell(clickWorldPos);
        Vector2Int baseLocal = WorldToLocalSubtile(clickWorldPos);
        return (baseCell, baseLocal);
    }

    // Given a clicked world position and an object size in subtiles (sizeX,sizeY),
    // return a map: tileCell -> set of local subtiles covered by that footprint.
    // Anchor is the clicked anchorSubtile (treated as the middle of the footprint).
    public Dictionary<Vector2Int, HashSet<Vector2Int>> GetFootprintCellLocalPairs(Vector2Int tile, Vector2Int subtile,int sizeX, int sizeY, bool anchorIsTopLeft = false)
    {
        var result = new Dictionary<Vector2Int, HashSet<Vector2Int>>();

        // base cell and local index where user clicked
        Vector2Int baseCell = tile;
        Vector2Int baseLocal = subtile;

        result.Add(baseCell, new HashSet<Vector2Int>());

        //adding center anchorSubtile as anchor
        result[baseCell].Add(baseLocal);

        // compute starting local indices depending on anchoring mode
        int startLocalX;
        int startLocalY;

        if (anchorIsTopLeft)
        {
            // clicked anchorSubtile is the top-left corner of footprint
            startLocalX = baseLocal.x;
            startLocalY = baseLocal.y;
        }
        else
        {
            // clicked anchorSubtile is the middle of the footprint:
            // shift start so the footprint is centered on the clicked anchorSubtile.
            startLocalX = baseLocal.x - Mathf.FloorToInt(sizeX / 2f);
            startLocalY = baseLocal.y - Mathf.FloorToInt(sizeY / 2f);
        }

        for (int dx = 0; dx < sizeX; dx++)
        {
            for (int dy = 0; dy < sizeY; dy++)
            {
                int globalLocalX = startLocalX + dx;
                int globalLocalY = startLocalY + dy;

                // compute which anchorTile cell offset this global local lands in
                int cellOffsetX = Mathf.FloorToInt(globalLocalX / (float)SubtilesPerCell);
                int cellOffsetY = Mathf.FloorToInt(globalLocalY / (float)SubtilesPerCell);

                int localX = globalLocalX - cellOffsetX * SubtilesPerCell;
                int localY = globalLocalY - cellOffsetY * SubtilesPerCell;

                // normalize local coordinates (should be 0..7)
                localX = Mathf.Clamp(localX, 0, SubtilesPerCell - 1);
                localY = Mathf.Clamp(localY, 0, SubtilesPerCell - 1);

                var tileCell = new Vector2Int(baseCell.x + cellOffsetX, baseCell.y + cellOffsetY);
                var localPos = new Vector2Int(localX, localY);

                if (!result.ContainsKey(tileCell))
                    result.Add(tileCell, new HashSet<Vector2Int>());

                result[tileCell].Add(localPos);
            }
        }

        return result;
    }

    public Dictionary<Vector2Int, HashSet<Vector2Int>> GetFootprintCellLocalPairs(Vector2 clickWorldPos, int sizeX, int sizeY, bool anchorIsTopLeft = false)
    {
        Vector2Int baseCell = WorldToCell(clickWorldPos);
        Vector2Int baseLocal = WorldToLocalSubtile(clickWorldPos);

        return GetFootprintCellLocalPairs(baseCell, baseLocal, sizeX, sizeY, anchorIsTopLeft);
    }

    public bool IsFootprintOccupied(Vector2Int tile, Vector2Int subtile ,int sizeX, int sizeY, bool anchorIsTopLeft = false)
    {
        Dictionary<Vector2Int, HashSet<Vector2Int>> pairs = GetFootprintCellLocalPairs(tile,subtile, sizeX, sizeY, anchorIsTopLeft);

        foreach (Vector2Int tileCell in pairs.Keys)
        {
            foreach (Vector2Int localPos in pairs[tileCell])
            {
                // check bounds for each anchorTile cell (optional)
                if (!_bounds.Contains(tileCell))
                {
                    Debug.LogWarning($"[Place] {tileCell} out of bounds");
                    return true;
                }

                if (IsSubTilePresented(tileCell, localPos))
                {
                    Debug.LogWarning($"[Place] subtile {localPos} in cell {tileCell} occupied");
                    return true;
                }

                // also check if the full anchorTile (main anchorTile) blocks placement if necessary:
                if (IsTilePresented(tileCell))
                {
                    Debug.LogWarning($"[Place] main tile {tileCell} occupied");
                    return true;
                }
            }
        }

        return false;
    }


    // Example helper that checks whether any anchorSubtile in the footprint is already present/occupied.
    // clickWorldPos = world coordinates of click, sizeX/Y = object size in subtiles.
    public bool IsFootprintOccupied(Vector2 clickWorldPos, int sizeX, int sizeY, bool anchorIsTopLeft = false)
    {
        Dictionary<Vector2Int, HashSet<Vector2Int>> pairs = GetFootprintCellLocalPairs(clickWorldPos, sizeX, sizeY, anchorIsTopLeft);

        foreach (Vector2Int tileCell in pairs.Keys)
        {
            foreach (Vector2Int localPos in pairs[tileCell])
            {
                // check bounds for each anchorTile cell (optional)
                if (!_bounds.Contains(tileCell))
                {
                    Debug.LogWarning($"[Place] {tileCell} out of bounds");
                    return true;
                }

                if (IsSubTilePresented(tileCell, localPos))
                {
                    Debug.LogWarning($"[Place] subtile {localPos} in cell {tileCell} occupied");
                    return true;
                }

                // also check if the full anchorTile (main anchorTile) blocks placement if necessary:
                if (IsTilePresented(tileCell))
                {
                    Debug.LogWarning($"[Place] main tile {tileCell} occupied");
                    return true;
                }
            }
        }

        return false;
    }

    // Replace CanBePlaced(Vector2 pos, MapBlockData data)
    public bool CanBePlaced(Vector2 pos, MapBlockData data)
    {
        if (data == null) return false;

        // Whole-tile placement (tilemap tile that occupies the entire tile)
        if (data.mapBlockType == MapBlockType.Tile)
        {
            Vector2Int tile = UtillityMath.VectorToVectorInt(pos);

            // Multitile (tile-grid offsets)
            if (data.isMultiblock && data.tileOffsets != null && data.tileOffsets.Count > 0)
            {
                foreach (var off in data.tileOffsets)
                {
                    var cell = tile + off;
                    if (!_bounds.Contains(cell))
                    {
                        Debug.LogWarning($"[Place] {cell} out of bounds");
                        return false;
                    }

                    if (LayerTiles.TryGetValue(cell, out var inner) && inner != null && inner.Count > 0)
                    {
                        Debug.LogWarning($"[Place] tile {cell} already occupied");
                        return false;
                    }
                }
                return true;
            }

            // Single tile
            if (!_bounds.Contains(tile))
            {
                Debug.LogWarning($"[Place] {tile} out of bounds");
                return false;
            }
            if (LayerTiles.TryGetValue(tile, out var innerTile) && innerTile != null && innerTile.Count > 0)
            {
                Debug.LogWarning($"[Place] tile {tile} already occupied");
                return false;
            }
            return true;
        }

        // Non-tile (subtile) placement: use subtile footprint checks
        if (data.isMultiblock)
        {
            // object size in subtiles specified by blockSize
            if (IsFootprintOccupied(pos, data.blockSize.x, data.blockSize.y, anchorIsTopLeft: false))
                return false;
            return true;
        }

        // Single subtile placement
        if (!_bounds.Contains(UtillityMath.VectorToVectorInt(pos)))
        {
            Debug.LogWarning($"[Place] {pos} out of bounds");
            return false;
        }
        var tileCell = UtillityMath.VectorToVectorInt(pos);
        var local = WorldToLocalSubtile(pos);
        if (IsSubTilePresented(tileCell, local))
        {
            Debug.LogWarning($"[Place] subtile {local} in {tileCell} occupied");
            return false;
        }
        return true;
    }
    public bool CanBePlaced(Vector2Int tile, Vector2Int subtile, MapBlockData data)
    {
        if (data == null) return false;

        if (data.isMultiblock)
        {
            // If you want subtiles-based placement for multiblock, use IsFootprintOccupied.
            // Here is an example using blockSize as anchorSubtile dimensions:
            Vector2 clickWorld = tile;
            int sizeX = data.blockSize.x; // number of subtiles in X
            int sizeY = data.blockSize.y; // number of subtiles in Y

            if (data.mapBlockType == MapBlockType.Tile)
            {
                if (IsFootprintOccupied(clickWorld, sizeX, sizeY, anchorIsTopLeft: false))
                    return false;
            }
            else
            {
                if (IsFootprintOccupied(tile, subtile, sizeX, sizeY, anchorIsTopLeft: false))
                    return false;
            }
        }

        if (!_bounds.Contains(UtillityMath.VectorToVectorInt(tile)))
        {
            Debug.LogWarning($"[Place] {tile} вне допустимых границ");
            return false;
        }
        if (LayerTiles.ContainsKey(UtillityMath.VectorToVectorInt(tile)))
        {
            Debug.LogWarning($"[Place] {tile} занята");
            return false;
        }

        return true;
    }


    // Replace PlaceBlock(Vector2Int tileIndex, Vector2Int subtileIndex, MapBlockData data)
    public bool PlaceBlock(Vector2Int tileIndex, Vector2Int subtileIndex, MapBlockData data)
    {
        if (!CanBePlaced(tileIndex, subtileIndex, data)) return false;

        Dictionary<Vector2Int, HashSet<Vector2Int>> group;

        if (data.mapBlockType == MapBlockType.Tile)
        {
            // tile-type: behave like whole-tile placement centered at tileIndex
            List<Vector2Int> cells = new List<Vector2Int> { tileIndex };
            group = new Dictionary<Vector2Int, HashSet<Vector2Int>>();
            foreach (var cell in cells)
            {
                if (!LayerTiles.ContainsKey(cell)) LayerTiles.Add(cell, new SerializedDictionary<Vector2Int, MapTile>());

                var set = new HashSet<Vector2Int>();
                for (int lx = 0; lx < SubtilesPerCell; lx++)
                for (int ly = 0; ly < SubtilesPerCell; ly++)
                {
                    var local = new Vector2Int(lx, ly);
                    if (LayerTiles[cell].ContainsKey(local)) continue;
                    LayerTiles[cell].Add(local, new MapTile(local, data, cell));
                    set.Add(local);
                }
                group[cell] = set;

                if (data.breakable)
                {
                    int hp = Mathf.Max(1, data.maxHealth);
                    if (!anchorHp.ContainsKey(cell)) anchorHp[cell] = new SerializedDictionary<Vector2Int, int>();
                    foreach (var l in set) anchorHp[cell][l] = hp;
                }
            }

            onMapTilePlaced?.Invoke(group, data);
            if (data.breakable)
                onTileHealthChanged?.Invoke(group, data, GetHealth(tileIndex, group[tileIndex].First()), data.maxHealth);

            return true;
        }

        // subtile/multisubtile placement (non-tile)
        group = GetFootprintCellLocalPairs(tileIndex, subtileIndex, data.blockSize.x, data.blockSize.y, anchorIsTopLeft: false);

        Vector2Int anchor = group.First().Key;
        Vector2Int subTile = group.First().Value.First();

        foreach (Vector2Int tile in group.Keys)
        {
            if (!LayerTiles.ContainsKey(tile)) LayerTiles.Add(tile, new SerializedDictionary<Vector2Int, MapTile>());

            foreach (Vector2Int tileCell in group[tile])
            {
                if (LayerTiles[tile].ContainsKey(tileCell)) continue;
                LayerTiles[tile].Add(tileCell, new MapTile(tileCell, data, tile));
            }
        }

        if (data.breakable)
            SetHealth(anchor, subTile, data, Mathf.Max(1, data.maxHealth), fireEvent: false);

        onMapTilePlaced?.Invoke(group, data);

        if (data.breakable)
            onTileHealthChanged?.Invoke(group, data, GetHealth(anchor, subTile), data.maxHealth);

        return true;
    }


    // Replace PlaceBlock(Vector2 pos, MapBlockData data)
    public bool PlaceBlock(Vector2 pos, MapBlockData data)
    {
        if (!CanBePlaced(pos, data)) return false;

        // Whole-tile placement
        if (data.mapBlockType == MapBlockType.Tile)
        {
            Vector2Int baseTile = UtillityMath.VectorToVectorInt(pos);

            // determine affected tile cells
            List<Vector2Int> cells = new List<Vector2Int>();
            if (data.isMultiblock && data.tileOffsets != null && data.tileOffsets.Count > 0)
            {
                foreach (var off in data.tileOffsets) cells.Add(baseTile + off);
            }
            else
            {
                cells.Add(baseTile);
            }

            var group = new Dictionary<Vector2Int, HashSet<Vector2Int>>();
            foreach (var cell in cells)
            {
                if (!LayerTiles.ContainsKey(cell)) LayerTiles.Add(cell, new SerializedDictionary<Vector2Int, MapTile>());

                var set = new HashSet<Vector2Int>();
                // fill all local subtiles in this tile
                for (int lx = 0; lx < SubtilesPerCell; lx++)
                for (int ly = 0; ly < SubtilesPerCell; ly++)
                {
                    var local = new Vector2Int(lx, ly);
                    if (LayerTiles[cell].ContainsKey(local)) continue;
                    LayerTiles[cell].Add(local, new MapTile(local, data, cell));
                    set.Add(local);
                }
                group[cell] = set;

                // initialize per-subtile HP if breakable
                if (data.breakable)
                {
                    int hp = Mathf.Max(1, data.maxHealth);
                    if (!anchorHp.ContainsKey(cell)) anchorHp[cell] = new SerializedDictionary<Vector2Int, int>();
                    foreach (var l in set) anchorHp[cell][l] = hp;
                }
            }

            onMapTilePlaced?.Invoke(group, data);
            if (data.breakable)
                onTileHealthChanged?.Invoke(group, data, GetHealth(cells[0], group[cells[0]].First()), data.maxHealth);

            return true;
        }

        // Non-tile placement -> treat as subtile-based placement (existing path)
        // For sub-tile placements we already have a dedicated overload; convert pos->tile and local, then call it
        var tile = UtillityMath.VectorToVectorInt(pos);
        var localSub = WorldToLocalSubtile(pos);
        return PlaceBlock(tile, localSub, data);
    }

    public void RemoveTile(Vector2Int anchorTile, Vector2Int anchorSubtile)
    {
        if (!LayerTiles.TryGetValue(anchorTile, out var subtiles)) return;

        MapTile mapTile = LayerTiles[anchorTile][anchorSubtile];

        var data = mapTile.BlockData;
        var subtilePostion = mapTile.Position;

        Vector2 worldPosition = SubtileToWorldPosition(anchorTile, subtilePostion);

        Dictionary<Vector2Int, HashSet<Vector2Int>> occupiedTiles = GetFootprintCellLocalPairs(worldPosition, data.blockSize.x, data.blockSize.y);

        foreach (Vector2Int tile in occupiedTiles.Keys)
        {
            foreach (Vector2Int subtile in occupiedTiles[tile])
            {
                if (LayerTiles.ContainsKey(tile) && LayerTiles[tile].ContainsKey(subtile))
                {
                    LayerTiles[tile].Remove(subtile);
                    if (LayerTiles[tile].Count == 0)
                        LayerTiles.Remove(tile);

                    if(anchorHp.ContainsKey(tile))
                    {
                        if (anchorHp[tile].ContainsKey(subtile))
                        {
                            anchorHp[tile].Remove(subtile);
                            if (anchorHp[tile].Count == 0)
                                anchorHp.Remove(tile);
                        }
                    }
                }
            }
        }

        //foreach (var cell in group) LayerTiles.Remove(cell);

        onMapTileRemoved?.Invoke(occupiedTiles, data.mapBlockType);
    }

    public int GetHealth(Vector2Int anchor, Vector2Int subTile)
    {
        if(anchorHp.TryGetValue(anchor, out SerializedDictionary<Vector2Int, int> val))
        {
            anchorHp[anchor].TryGetValue(subTile, out int hp);
            return hp;
        }

        return -1;
    }

    public void SetHealth(Vector2Int anchor, Vector2Int subtile,MapBlockData data, int hp, bool fireEvent = true)
    {
        if (data == null || !data.breakable) return;

        hp = Mathf.Clamp(hp, 0, Mathf.Max(1, data.maxHealth));

        if (anchorHp.ContainsKey(anchor))
        {
            anchorHp[anchor].Add(subtile,hp);
        }
        else
        {
            anchorHp.Add(anchor, new SerializedDictionary<Vector2Int, int>());
            anchorHp[anchor].Add(subtile, hp);
        }

        if (fireEvent)
            onTileHealthChanged?.Invoke(GetFootprintCellLocalPairs(anchor, data.blockSize.x, data.blockSize.y), data, hp, data.maxHealth);
    }

    public bool Damage(Vector2Int anchor, Vector2Int subtile,MapBlockData data, int amount)
    {
        if (data == null || !data.breakable) return false;
        int maxHp = Mathf.Max(1, data.maxHealth);
        int cur = GetHealth(anchor, subtile);
        if (cur < 0) cur = maxHp;

        int next = Mathf.Clamp(cur - Mathf.Max(1, amount), 0, maxHp);
        
        anchorHp[anchor].Remove(subtile);
        anchorHp[anchor].Add(subtile, next);

        onTileHealthChanged?.Invoke(GetFootprintCellLocalPairs(anchor, data.blockSize.x, data.blockSize.y), data, next, maxHp);
        return next <= 0;
    }

    //// утилиты
    //private Dictionary<Vector2Int, HashSet<Vector2Int>> EnumerateGroupCells(Vector2Int anchor, MapBlockData data)
    //{
    //    Dictionary<Vector2Int, HashSet<Vector2Int>> result = GetFootprintCellLocalPairs(anchor, data.blockSize.x, data.blockSize.y);
        
    //    //if (data.isMultiblock)
    //    //foreach (var off in data.tileOffsets) yield return anchor + off;
    //    //else
        
    //    yield return result;
    //}

    // Returns world position for given anchorTile cell + local anchorSubtile index.
    // - tileCell: integer anchorTile coordinates (same as keys in LayerTiles).
    // - localSubtile: local anchorSubtile coords inside cell (0..SubtilesPerCell-1).
    // - center: if true returns center of the anchorSubtile, otherwise bottom-left corner.
    //
    // The method handles local indices outside [0..SubtilesPerCell-1] by moving to adjacent cells.
    public Vector2 SubtileToWorldPosition(Vector2Int tileCell, Vector2Int localSubtile, bool center = true)
    {
        // handle local indices that may overflow/underflow the cell by moving to adjacent tiles
        int cellOffsetX = Mathf.FloorToInt(localSubtile.x / (float)SubtilesPerCell);
        int cellOffsetY = Mathf.FloorToInt(localSubtile.y / (float)SubtilesPerCell);

        int localX = localSubtile.x - cellOffsetX * SubtilesPerCell;
        int localY = localSubtile.y - cellOffsetY * SubtilesPerCell;

        // clamp to valid local range (defensive)
        localX = Mathf.Clamp(localX, 0, SubtilesPerCell - 1);
        localY = Mathf.Clamp(localY, 0, SubtilesPerCell - 1);

        // compute resulting anchorTile cell (may be different when localSubtile was outside range)
        var resultCell = new Vector2Int(tileCell.x + cellOffsetX, tileCell.y + cellOffsetY);

        float x = resultCell.x + localX * cellSize + (center ? cellSize * 0.5f : 0f);
        float y = resultCell.y + localY * cellSize + (center ? cellSize * 0.5f : 0f);

        return new Vector2(x, y);
    }
}
