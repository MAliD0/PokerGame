using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using TMPro;

public class InventoryUISlot : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IEndDragHandler, IDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] Image itemSprite;
    [SerializeField] TextMeshProUGUI countField;
    public ItemData itemData;

    public Action<InventoryUISlot> onSlotPressed;
    public Action<InventoryUISlot> onDragStarted;
    public Action<InventoryUISlot> onDragEnded;
    public Action<InventoryUISlot> onSlotHover;
    public Action<InventoryUISlot> onSlotExit;


    public int index;

    public void ConnectItem(ItemSlot itemSlot)
    {
        if(itemSlot.itemData == null)
        {
            itemData = null;
            itemSprite.sprite = null;
            countField.text = string.Empty;
        }
        else
        {
            this.itemData = itemSlot.itemData;
            itemSprite.sprite = itemData.itemIcon;
            countField.text = itemSlot.number.ToString();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        onSlotPressed?.Invoke(this);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        onDragStarted?.Invoke(this);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        onDragEnded?.Invoke(this);
    }

    public void OnDrag(PointerEventData eventData) { }

    public void OnPointerEnter(PointerEventData eventData)
    {
        onSlotHover?.Invoke(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        onSlotExit?.Invoke(this);
    }
}
