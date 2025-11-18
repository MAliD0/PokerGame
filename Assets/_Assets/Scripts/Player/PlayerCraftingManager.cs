using System.Collections.Generic;
using UnityEngine;

public class PlayerCraftingManager : MonoBehaviour
{
    [SerializeField] CraftingMenuUI craftingMenu;
    [SerializeField] Inventory playerInventory;
    private void Start()
    {
        craftingMenu = CraftingMenuUI.instance;
        craftingMenu.onItemCraftAttempt += ItemCraftAttemptHandler;
    }
    public void OpenCraftingMenu(List<ItemData> craftableItems)
    {
        UIManager.Instance.EnableMenu("CraftingMenu");
        craftingMenu.OpenCraftingMenu(craftableItems);
    
    }

    private void ItemCraftAttemptHandler(ItemData itemData)
    {
        foreach (var recipeItem in itemData.recipeItems)
        {
            if(!playerInventory.HasItem(recipeItem.itemData, recipeItem.itemNumber))
            {
                return;
            }
        }

        foreach (var recipeItem in itemData.recipeItems)
        {
            playerInventory.RemoveItem(recipeItem.itemData, recipeItem.itemNumber);
        }

        playerInventory.AddItem(itemData, itemData.craftAmount);
    }
}