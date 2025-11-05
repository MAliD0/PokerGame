using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Burst.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;

public class InventoryUI : GameUIElement
{
    //Later replace it somehow
    public static InventoryUI Instance;

    [SerializeField] Inventory inventory;

    [SerializeField] Transform slotsHolder;
    [SerializeField] GameObject itemSlotPrefab;
    
    [SerializeField] List<InventoryUISlot> slots = new List<InventoryUISlot>();

    //test purpose
    public Action<int> onItemIndexSelected;

    private InventoryUISlot startDragSlot;
    private InventoryUISlot endDragSlot;

    private InventoryUISlot selectedSlot;

    private void Awake()
    {
        Instance = this;
    }

    public void ConnectInventory(Inventory inventory)
    {
        if (this.inventory != null)
        {
            this.inventory.onInventoryUpdate = null;
        }

        foreach (InventoryUISlot uISlot in slots)
        {
            Destroy(uISlot.gameObject);
        }

        slots.Clear();
        slots = new List<InventoryUISlot>();

        this.inventory = inventory; 
        inventory.onInventoryUpdate += () => { SetInventoryItems(inventory.GetInventoryItems()); };

        for (int i = 0; i < inventory.inventorySize; i++)
        {
            print("Create");
            InventoryUISlot slot = Instantiate(itemSlotPrefab, slotsHolder).GetComponent<InventoryUISlot>();

            slot.onSlotPressed += OnSlotPressedHandler;
            slot.onDragStarted += DragStartedHandler;
            slot.onDragEnded += DragEndHandler;
            slot.onSlotHover += OnSlotHoverHandler;
            slot.onSlotExit += OnSlotExitHandler;

            slot.index = i;

            slots.Add(slot);
        }
    }
    private void Start()
    {
        base.Start();
        
        if(inventory!=null)
            ConnectInventory(inventory);
    }
    public void SetInventoryItems(List<ItemSlot> items)
    {
        for (int i = 0; i < inventory.inventorySize; i++)
        {
            slots[i].ConnectItem(items[i]);
        }
    }

    private void DragStartedHandler(InventoryUISlot slot)
    {
        if(slot != null)
        {
            startDragSlot = slot;
        }
        print("Start Drag: " + slot.index);
    }
    private void DragEndHandler(InventoryUISlot slot)
    {
        if(slot != null)
        {
            if(selectedSlot != null)
            {
                inventory.MoveItems(startDragSlot.index, selectedSlot.index);
            }
        }
        print("End Drag: "+ slot.index);
    }


    public void OnSlotPressedHandler(InventoryUISlot slot)
    {
        onItemIndexSelected?.Invoke(slot.index);
    }

    public void OnSlotHoverHandler(InventoryUISlot slot)
    {
        selectedSlot = slot;
    }
    public void OnSlotExitHandler(InventoryUISlot slot)
    {
        if (slot.index == selectedSlot.index)
        {
            selectedSlot = null;
        }
    }
}
