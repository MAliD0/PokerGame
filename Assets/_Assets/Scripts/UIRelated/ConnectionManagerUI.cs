using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class ConnectionManagerUI : GameUIElement
{
    [SerializeField] TextMeshProUGUI serverCodeText;
    [SerializeField] Button serverCreateButton;
    [Space]
    [SerializeField] Button ConnectButton;
    [SerializeField] TMP_InputField codeInputField;

    public Action<string> onConnectButtonPressed;
    string serverCode;

    protected void Start()
    {
        base.Start();
        ConnectionManager.instance.onServerActivate += OnServerCreated;

        ConnectButton.onClick.AddListener(() =>
        {
            print("Trying to connect: " + serverCode);

            serverCode = codeInputField.text;
            onConnectButtonPressed?.Invoke(serverCode);
            ConnectionManager.instance.JoinRelay(serverCode);
        });

        serverCreateButton.onClick.AddListener(() =>
        {
            ConnectionManager.instance.CreateRelay();
        });
    }

    public void OnServerCreated(string code)
    {
        serverCreateButton.enabled = false;
        serverCodeText.text = "Code: "+ code;
        serverCode = code;
    }

    public void ServerCodeToClipboard()
    {
        GUIUtility.systemCopyBuffer = serverCode;
    }
}
