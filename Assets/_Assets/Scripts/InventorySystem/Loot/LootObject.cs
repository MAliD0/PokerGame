using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEditor.PackageManager;
using UnityEngine;

public class LootObject : NetworkBehaviour, IInteractable
{
    [SerializeField] SpriteRenderer sr;

    private readonly NetworkVariable<FixedString64Bytes> _itemId =
        new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<int> _amount =
        new(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    public override void OnNetworkSpawn()
    {
        _itemId.OnValueChanged += (_, __) => RefreshVisual();
        _amount.OnValueChanged += (_, __) => RefreshVisual();

        RefreshVisual();
        //_t0 = Time.time;
    }
    public void InitServer(string itemId, int amount)
    {
        if (!IsServer) return;
        _itemId.Value = itemId;
        _amount.Value = Mathf.Max(1, amount);
        RefreshVisual(); // хост тоже увидит сразу
    }

    // === CLIENT VISUAL ===
    private void RefreshVisual()
    {
        if (!sr) sr = GetComponent<SpriteRenderer>();
        var def = ItemDatabase.instance.GetItem(_itemId.Value.ToString());
        sr.sprite = def ? def.itemIcon : null;

        // (опционально) показать количество – наклейка/TextMeshPro и т.п.
        // Здесь можно повесить простую надпись или точечки.
    }

    public void OnInteract(GameObject interactor, ulong interacterId)
    {
        if(interactor.TryGetComponent<NetworkBehaviour>(out NetworkBehaviour networkBehaviour))
        {
            InteractRequestServerRpc(networkBehaviour.NetworkObjectId, interacterId);
        }

    }
    [ServerRpc(RequireOwnership = false)]
    public void InteractRequestServerRpc(ulong objectId, ulong clientId)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectId, out var no))
        {
            Debug.LogWarning($"NO Interactor {objectId} is not found!");
            return;
        }

        var target = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
        };

        no.gameObject.GetComponent<PlayerManager>().AddItemClientRpc(_itemId.Value.ToString(), _amount.Value, target);
        NetworkObject.Despawn(true);
    }
}
