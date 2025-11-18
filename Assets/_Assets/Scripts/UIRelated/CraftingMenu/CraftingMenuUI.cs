using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CraftingMenuUI : GameUIElement
{
    [SerializeField] GameObject UISlot;

    [SerializeField] Transform craftingOptionsParent;
    [SerializeField] Transform craftingRecipyParent;
    [SerializeField] GameObject buttonPrefab;

    public static CraftingMenuUI instance;
    public Action<ItemData> onItemCraftAttempt;

    private void Awake()
    {
        instance = this;
    }

    public void OpenCraftingMenu(List<ItemData> craftableItems)
    {
        foreach (Transform child in craftingOptionsParent.transform)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < craftableItems.Count; i++)
        {
            InventoryUISlot uISlot = Instantiate(UISlot, craftingOptionsParent).GetComponent<InventoryUISlot>();
            uISlot.ConnectItem(new ItemSlot(craftableItems[i]));
            uISlot.index = i;
            uISlot.onSlotPressed += ShowItemsRecipy;
        }
    }

    public void CloseCraftingMenu()
    {
        foreach (Transform child in craftingOptionsParent.transform)
        {
            Destroy(child.gameObject);
        }

        foreach (Transform child in craftingRecipyParent.transform)
        {
            Destroy(child.gameObject);
        }
    }

    private void ShowItemsRecipy(InventoryUISlot uISlotPressed)
    {
        foreach (Transform child in craftingRecipyParent.transform)
        {
            Destroy(child.gameObject);
        }

        List<RecipItem> recipeItems = uISlotPressed.itemData.recipeItems;

        for (int i = 0; i < recipeItems.Count; i++)
        {
            InventoryUISlot uiSlotRecipyItem = Instantiate(UISlot, craftingRecipyParent).GetComponent<InventoryUISlot>();
            uiSlotRecipyItem.ConnectItem(new ItemSlot(recipeItems[i].itemData, recipeItems[i].itemNumber));
        }

        Instantiate(buttonPrefab, craftingRecipyParent).GetComponent<Button>().onClick.AddListener(() => { 
            print($"Trying to craft item {uISlotPressed.itemData}");
            onItemCraftAttempt?.Invoke(uISlotPressed.itemData);
        });
    }
}
