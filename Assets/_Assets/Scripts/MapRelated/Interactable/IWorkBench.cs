using System.Collections.Generic;
using UnityEngine;

public class IWorkBench : MonoBehaviour, IInteractable
{
    [SerializeField] List<ItemData> craftableItems;
    public void OnInteract(GameObject interactor, ulong interacterId)
    {
        interactor.GetComponent<PlayerManager>().OpenCraftingMenu(craftableItems);
    }
}
