using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class ConnectionManager : NetworkBehaviour
{
    [Header("Settings:")]
    [SerializeField] int playerNumber = 3;//not including host;
    [InlineButton("JoinButton", "Join")][SerializeField] string joinCode;

    public Action<string> onServerActivate;
    public Action onPlayerConnection;

    public static ConnectionManager instance;

    private void Awake()
    {
        instance = this;
    }

    async void Start()
    {
        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed in:" + AuthenticationService.Instance.PlayerId);
        };
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    [Button("CreateRelay")]
    public async void CreateRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(playerNumber);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log(joinCode);

            RelayServerData relayServerData = new RelayServerData(allocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            NetworkManager.Singleton.StartHost();

            onServerActivate?.Invoke(joinCode);

        }
        catch (RelayServiceException ex)
        {
            Debug.Log(ex);
        }

    }

    private void JoinButton()
    {
        Debug.Log("Join");
        if (joinCode == null) { return; }
        JoinRelay(joinCode);
    }
    public async void JoinRelay(string joinCode)
    {
        try
        {
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            RelayServerData relayServerData = new RelayServerData(allocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            NetworkManager.Singleton.StartClient();
        }
        catch (RelayServiceException ex)
        {
            Debug.Log(ex);
        }
    }

    public ulong[] OtherClientIds()
    {
        var ids = new List<ulong>(NetworkManager.Singleton.ConnectedClientsIds);
        ids.Remove(NetworkManager.Singleton.LocalClientId);
        return ids.ToArray();
    }

    public ClientRpcParams SendAllExceptHost()
    {
        var rpc = IsHost
        ? new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = OtherClientIds() } }
        : default;
        return rpc;
    }
}
