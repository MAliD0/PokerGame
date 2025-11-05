using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemDatabase : MonoBehaviour
{
    [HideInInspector]public static ItemDatabase instance;

    public MapBlockDataLibrary blockLibrary;
    public ItemLibrary itemLibrary;

    private void Awake()
    {
        instance = this;
    }
    public ItemData GetItem(string itemId)
    {
        ItemData item = blockLibrary.GetMapBlockData(itemId);
        ItemData itemData = itemLibrary.GetMapBlockData(itemId);

        return item==null? itemData: item;
    }
}
