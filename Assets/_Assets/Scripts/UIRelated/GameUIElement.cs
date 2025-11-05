using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;

public class GameUIElement : MonoBehaviour, IDragHandler
{
    [FoldoutGroup("Base Settings:" , order: 0)]
    public string menuName;
    [FoldoutGroup("Base Settings:", order: 0)]
    public Canvas canvas;
    [FoldoutGroup("Base Settings:", order: 0)]
    public RectTransform mainHolder;
    [FoldoutGroup("Base Settings:", order: 0)]
    public bool isDraggable;
    public bool isActive { get { return enabled; } set { enabled = value; SetActive(isActive); } }
    private new bool enabled;

    protected void Start()
    {
        UIManager.Instance.LinkMenu(this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isDraggable)
        {
            mainHolder.anchoredPosition += eventData.delta / canvas.scaleFactor;
        }
    }

    protected void SetActive(bool active)
    {
        mainHolder.gameObject.SetActive(active);
    }

}
