using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName ="ItemData",menuName ="InventorySystem/Items")]
public class ItemData : ScriptableObject
{
    public string Name { get { return name; } set { Name= name; } }
    [Title("$Name",titleAlignment:TitleAlignments.Left)]
    [PreviewField]
    [HideLabel]
    public Sprite itemIcon;
    public ItemType itemType;

    public bool isStackable;
    [ShowIf("isStackable")] public int maxStack = 24;

    public virtual string GetItemID()
    {
        return Name;
    }
}
public enum ItemType
{
    Tool,
    Placeable,
    Resource
}