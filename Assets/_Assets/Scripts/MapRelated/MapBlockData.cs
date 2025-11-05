using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName ="MapBlock",menuName ="Map/MapBlock")]
public class MapBlockData : ItemData
{
    public MapBlockType mapBlockType;
    new ItemType itemType = ItemType.Placeable;

    [ShowIf("mapBlockType", MapBlockType.Tile)]
    public TileBase tile;

    [ShowIf("mapBlockType", MapBlockType.GameObject)]
    public GameObject gameObject;

    public MapLayerType mapLayerType;

    public bool isMultiblock;
    public bool breakable;
    [ShowIf("breakable")]
    public int maxHealth;

    [ShowIf("isMultiblock")]
    public List<Vector2Int> tileOffsets = new List<Vector2Int>();

    public bool hasLoot;
    [ShowIf("hasLoot")] [SerializeField] bool dropSelf;
    [HideIf("dropSelf")] [ShowIf("hasLoot")] public List<LootRules> loot= new List<LootRules>();
    
    
    private void OnValidate()
    {
        if (dropSelf && !loot.Contains(new LootRules { item = this, chance = 100, min = 1, max = 1 }))
        {
            loot.Add(new LootRules { item = this, chance = 100, min = 1, max = 1 });
        }
    }

    [Serializable]
    public struct LootRules
    {
        public ItemData item;
        [Range(0,100)]public int chance;
        [Range (1,100)] public int min;
        [Range(1, 100)] public int max;
    }
    public override string GetItemID()
    {
        if(mapBlockType == MapBlockType.Tile)
        {
            return tile.name;
        }
        else if(mapBlockType == MapBlockType.GameObject) 
        {
            return gameObject.name;
        }else { 
            return base.GetItemID();
        }
    }
}

public enum MapBlockType
{
    Tile,
    GameObject
}
public enum MapLayerType
{
    waterGround,
    backGround,//a earth tiles
    wallGround,//on a wall
    foreGround//on a earth
}
