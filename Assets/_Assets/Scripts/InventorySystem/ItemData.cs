using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="ItemData",menuName ="InventorySystem/Items")]
public class ItemData : ScriptableObject
{
    public string Name { get { return name; } set { Name= name; } }
    
    [Title("$Name",titleAlignment:TitleAlignments.Left)]
    [PreviewField] [HideLabel] public Sprite itemIcon;
    public ItemType itemType;

    [FoldoutGroup("Settings")] public bool isStackable;
    [ShowIf("isStackable")] public int maxStack = 24;

    [FoldoutGroup("Settings")] public bool isCraftable;
    [ShowIf("isCraftable")] public List<RecipItem> recipeItems;
    [ShowIf("isCraftable")] public int craftAmount = 1;


    public virtual string GetItemID()
    {
        return Name;
    }
}

[Serializable]
public struct RecipItem
{
    public ItemData itemData;
    public int itemNumber;
}
public enum ItemType
{
    Tool,
    Placeable,
    Resource
}