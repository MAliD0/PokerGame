using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class DSUIManager : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI textBox;
    [SerializeField] RectTransform bubble;
    [SerializeField] GameObject dialogueBoxHolder;

    public string currentText;
    [SerializeField] float minSizeDeltaY = 100;
    [SerializeField] float bottomSize = 48f;

    private void Awake()
    {
        CloseDialogueBox();
    }

    public void StopDialogue()
    {
        CloseDialogueBox();
    }

    public void SetCurrentText(string text)
    {
        currentText = text;

        textBox.text = currentText;
        textBox.ForceMeshUpdate();
        Vector2 renderedSize = textBox.GetRenderedValues(false);
        float newHeight = renderedSize.y + bottomSize;
        newHeight = minSizeDeltaY > newHeight ? minSizeDeltaY : newHeight;
        bubble.sizeDelta = new Vector2(bubble.sizeDelta.x, newHeight);
    }

    public void OpenDialogueBox(string sentence)
    {
        dialogueBoxHolder.SetActive(true);
    }

    public void CloseDialogueBox()
    {
        dialogueBoxHolder.SetActive(false);
        textBox.text = "";
        bubble.sizeDelta = new Vector2(bubble.sizeDelta.x, minSizeDeltaY);
    }
}
