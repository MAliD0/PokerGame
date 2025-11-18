using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [SerializeField] List<ItemSlot> slots;
    [Space]
    public int inventorySize = 10;
    [FoldoutGroup("Test")][SerializeField] ItemData data;
    [FoldoutGroup("Test")][SerializeField] [Range(1,15)]int number;

    public Action onInventoryUpdate;

    [Button]
    private void InitSlots()
    {
        slots = new List<ItemSlot>(inventorySize);
        for (int i = 0; i < inventorySize; i++)
        {
            slots.Add(new ItemSlot());
        };
    }

    [Button]
    private void AddItemTest()
    {
        AddItem(data, number);
    }
    [Button]
    private void RemoveItemTest()
    {
        RemoveItem(data, number);
    }

    public bool HasItem(ItemData item, int number)
    {
        List<ItemSlot> items = slots.FindAll(x => x.itemData == item);

        int numberInInventory = 0;

        foreach (var i in items)
        {
            numberInInventory += i.number;
        }

        return numberInInventory >= number;
    }

    public int AddItem(string data, int number)
    {
        return AddItem(ItemDatabase.instance.GetItem(data), number);
    }
    public int AddItem(ItemData data, int number)
    {
        int excess = number;
        while (true)
        {
            ItemSlot itemSlot = slots.Find(x => x.itemData == data && !x.IsFull());

            if (itemSlot != null)
            {
                excess = itemSlot.AddCount(excess);
                print(excess);
                if (excess > 0)
                    continue;
                else
                {
                    break;
                }
            }
            else
            {
                itemSlot = slots.Find(x => x.itemData == null);
                if (itemSlot != null)
                {
                    itemSlot.SetItemData(data);
                    excess = itemSlot.AddCount(excess);
                    print(excess);
                    if (excess > 0)
                        continue;
                    else
                        break;
                }
                else
                    break;
            }
        }
        print($"+{data.Name}: {number}|{excess}");
        onInventoryUpdate?.Invoke();
        return excess;
    }

    public int RemoveItem(ItemData data, int number)
    {
        int excess = number;
        while (true)
        {
            ItemSlot itemSlot = slots.FindLast(x => x.itemData == data);

            if (itemSlot != null)
            {
                excess = itemSlot.RemoveCount(excess);
                if (excess > 0)
                    continue;
                else
                {
                    break;
                }
            }
            else
            {
                break;
            }
        }
        print($"-{data.Name}: {number}|{excess}");
        onInventoryUpdate?.Invoke();
        return excess;
    }
    
    public void MoveItems(int from, int to)
    {
        if (from > slots.Count && from < 0) return;
        if (to > slots.Count && to < 0) return;

        ItemSlot a = slots[from];
        ItemSlot b = slots[to];

        if (a == null && b == null) return;

        if(b.itemData == null)
        {
            b.SetItemData(a);
            a.SetItemData(null);
            
            onInventoryUpdate?.Invoke();
            return;
        }

        if(a.itemData == null)
        {
            a.SetItemData(b);
            b.SetItemData(a);

            onInventoryUpdate?.Invoke();
            return; 
        }

        if(a.itemData != b.itemData)
        {
            ItemSlot aSlot = new ItemSlot(a);
            ItemSlot bSlot = new ItemSlot(b);

            a.SetItemData(bSlot);
            b.SetItemData(aSlot);

            onInventoryUpdate?.Invoke();
            return;
        }
        else
        {
            int excess = b.AddCount(a.number);
            a.RemoveCount(a.number-excess);

            onInventoryUpdate?.Invoke();
            return;
        }
    }

    public List<ItemSlot> GetInventoryItems()
    {
        return slots;
    }
}
