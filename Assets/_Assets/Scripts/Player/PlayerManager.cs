using Cinemachine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class PlayerManager : NetworkBehaviour
{
    [SerializeField] PlayerController playerMovement;
    [SerializeField] PlayerAnimationController playerAnimationController;
    [SerializeField] PlayerInputManager playerInputManager;
    [SerializeField] PlayerInteractionManager playerInteractionManager;
    [SerializeField] PlayerCraftingManager playerCraftingManager;

    [SerializeField] Inventory inventory;
    [SerializeField] DSManager dsManager;
    [SerializeField] Rigidbody2D rb;


    [Space]
    [Header("Test Fields:")]
    [SerializeField] int damage;
    [SerializeField] ItemSlot itemHeld;
    [Header("Settings:")]
    [SerializeField] int maxMessageLength = 36;
    [SerializeField] float castRadius;


    private void Start()
    {
        if (!IsOwner) return;

        playerMovement.rb = rb;
        MessageMenu.instance.onMessageSent += (x) => { if (!dsManager.dsStarted) { MessageServerRpc(x); } };

        InventoryUI.Instance.ConnectInventory(inventory);

        InventoryUI.Instance.onItemIndexSelected += (x) =>
        {
            ItemSlot itemSlot = inventory.GetInventoryItems()[x];

            itemHeld = itemSlot;
        };

        playerInputManager.onMouseLeftPress += (x) =>
        {
            Vector2Int vector2Int = UtillityMath.VectorToVectorInt(x);

            if (itemHeld.itemData == null) return;

            if (WorldMapManager.Instance.CheckIfPlacementIsPossible(x, itemHeld.itemData.GetItemID()))
            {
                WorldMapManager.Instance.SetTileRequestServerRpc(x, itemHeld.itemData.GetItemID());
                inventory.RemoveItem(itemHeld.itemData, 1);
            }
        };
        playerInputManager.onMouseRightPress += (x) => 
        {
            Vector2Int vector2Int = new Vector2Int();

            vector2Int.x = x.x < 0 ? -Mathf.CeilToInt(Mathf.Abs(x.x)) : (int)x.x;
            vector2Int.y = x.y < 0 ? -Mathf.CeilToInt(Mathf.Abs(x.y)) : (int)x.y;

            WorldMapManager.Instance.DamageTileRequestServerRpc(x, damage);

        };
        playerInputManager.onInteractPress += () =>
        {
            IInteractable[] interactables = playerInteractionManager.CastForInteractables(transform.position, castRadius);
            if (interactables.Length > 0)
            {
                interactables[0].OnInteract(this.gameObject, this.OwnerClientId);
            }
        };

        //InventoryUI.Instance.SetInventoryItems(WorldMapManager.Instance.blockLibrary.dataLibrary.Values.ToList<ItemData>());
    }
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            var vcam = FindObjectOfType<CinemachineVirtualCamera>();
            if (vcam != null)
            {
                vcam.Follow = transform;
                vcam.LookAt = transform;
            }

        }
    }

    [ClientRpc]
    private void MessageClientRpc(string text)
    {

        Debug.Log("Enter Client RPC:" + text);
        dsManager.SayText(text);
    }

    [ServerRpc(RequireOwnership = false)]
    private void MessageServerRpc(string text)
    {
        Debug.Log("Enter Client RPC:" + text);

        MessageClientRpc(text.Substring(0, Mathf.Min(text.Length, maxMessageLength)));
    }

    [ClientRpc]
    public void AddItemClientRpc(string itemId, int number, ClientRpcParams rpcParams)
    {
        inventory.AddItem(itemId, number);
    }

    private void Update()
    {
        if (!IsOwner) return;

        float inputX = playerInputManager.movementDirection.x;
        float inputY = playerInputManager.movementDirection.y;

        if (inputX != 0)
            playerAnimationController.FlipSprite(inputX > 0);

        playerAnimationController.SetBool("IsWalking", (inputX != 0 || inputY != 0));

        playerMovement.Move(inputX, inputY);
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;
    }

    public void OpenCraftingMenu(List<ItemData> craftableItems)
    {
        playerCraftingManager.OpenCraftingMenu(craftableItems);
    }
}
