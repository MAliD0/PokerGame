using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Sirenix.OdinInspector;
using System.Linq;

public class GhostBuildManager : MonoBehaviour
{
    public static GhostBuildManager Instance;
    [SerializeField] MapLayerLogic ghostLayerLogic;
    [SerializeField] MapLayerGraphics ghostLayerGraphics;

    [FoldoutGroup("Colours")][SerializeField] Color possiblePlacementColor;
    [FoldoutGroup("Colours")][SerializeField] Color unpossiblePlacementColor;

    [SerializeField] Color currentColor;
    
    [SerializeField] Vector2Int lastAnchorPoint;
    [SerializeField] Vector2Int lastSubtilePoint;

    /*TODO:
    1. Add tilemap ghost placing;
    */
        
    private void Awake() {
        Instance = this;
    }
    void Start()
    {
        ghostLayerLogic = new MapLayerLogic(new MapBounds(-127,127,-127,127, false));

        ghostLayerGraphics.Init(ghostLayerLogic);
        ghostLayerLogic.onMapTilePlaced +=  OnGhostTilePlaced;
    }
    private void OnGhostTilePlaced(Dictionary<Vector2Int ,HashSet<Vector2Int>> cells, MapBlockData data)
    {
        if (data.mapBlockType != MapBlockType.GameObject) return;

        Vector2Int[] occupiedTiles = cells.Keys.ToArray();

        Vector2Int anchor = occupiedTiles[0];
        Vector2Int anchorSubTile = cells[occupiedTiles[0]].First();

        Vector2 worldPos = ghostLayerLogic.SubtileToWorldPosition(occupiedTiles[0], anchorSubTile);

        var prefab = WorldMapManager.Instance.blockLibrary.GetMapBlockData(data.GetItemID())?.gameObject;

        if (!prefab)
        {
            Debug.LogError($"[TilePlaced] Prefab id={data.GetItemID()} не найден");
            return;
        }

        var go = Instantiate(prefab, worldPos, Quaternion.identity);
        go.GetComponentInChildren<SpriteRenderer>().color = currentColor;
        go.tag = "Ghost";
        
        foreach(Vector2Int subAnchor in cells.Keys)
        {
            ghostLayerGraphics.BindObject(subAnchor, cells[subAnchor].ToList(), go, "ghostBlock");
        }
    }

    public bool PlaceGhost(Vector2 pos, MapBlockData blockData)
    {
        Vector2Int newAnchorPoint = ghostLayerLogic.WorldToCell(pos);
        Vector2Int newSubtilePoint= ghostLayerLogic.WorldToLocalSubtile(pos);
        
        bool placementSucceed = WorldMapManager.Instance.CheckIfPlacementIsPossible(pos, blockData.GetItemID());

        if(lastAnchorPoint == newAnchorPoint && lastSubtilePoint == newSubtilePoint) return placementSucceed;

        lastAnchorPoint = newAnchorPoint;
        lastSubtilePoint = newSubtilePoint;

        ghostLayerLogic.RemoveAllTiles();

        if(placementSucceed)
        {
            currentColor = possiblePlacementColor;
        }
        else
        {
            currentColor = unpossiblePlacementColor;
        }
        
        ghostLayerGraphics.LayerVisualsTiles.color = currentColor;
        ghostLayerLogic.PlaceBlock(pos, blockData);

        return placementSucceed;
    }
}
