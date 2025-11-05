using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MessageMenu : MonoBehaviour
{
    public Button SendMessageButton;
    public TMP_InputField InputField;

    public static MessageMenu instance;

    public Action<string> onMessageSent;

    private void Awake()
    {
        instance = this;
        SendMessageButton.onClick.AddListener(() => { onMessageSent?.Invoke(InputField.text); });
    }
}
