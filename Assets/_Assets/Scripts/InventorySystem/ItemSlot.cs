using System;
using UnityEngine;

[Serializable]
public class ItemSlot
{
    public ItemData itemData; //{ get; private set; }
    public int number;

    public ItemSlot()
    {
        itemData = null;
        number = 0;
    }
    public ItemSlot(ItemData data, int num = 0)
    {
        this.itemData = data;
        this.number = num;
    }
    public ItemSlot(ItemSlot itemSlot)
    {
        this.itemData = itemSlot.itemData;
        this.number = itemSlot.number;
    }


    public bool IsFull()
    {
        return number == itemData.maxStack;
    }

    public int AddCount(int count)
    {
        int availableSize = itemData.maxStack - number;
        int addValue = Mathf.Min(availableSize, count);
        number += addValue;
        return count-addValue;
    }

    public int RemoveCount(int count)
    {
        int removeNumber = Mathf.Min(count, number);
        number -= removeNumber;
        if(number == 0)
        {
            itemData = null;
        }
        return removeNumber-number;
    }

    public void SetItemData(ItemData data, int number = 0)
    {
        this.itemData = data;
        this.number = number;
    }
    public void SetItemData(ItemSlot item)
    {
        if(item == null)
        {
            itemData = null;
            number = 0;
        }
        else
        {
            this.itemData = item.itemData;
            this.number = item.number;
        }
    }
}
