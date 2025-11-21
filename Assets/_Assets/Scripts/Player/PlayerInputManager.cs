using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayerInputManager : MonoBehaviour
{
    public Action<Vector2> onMouseLeftPress;
    public Action<Vector2> onMouseRightPress;
    public Action onInteractPress;
    private PlayerControlls inputActions;
    public Vector2 movementDirection;

    private void OnEnable()
    {
        inputActions = new PlayerControlls();
        inputActions.Main.Enable();

        inputActions.Main.Move.performed += SetMove;
        inputActions.Main.Move.canceled += SetMove;

        inputActions.Main.Place.started+= SetLeftClick;
        inputActions.Main.Break.started += SetRightClick;

        inputActions.Main.Interact.started += SetInteractClick;
    }

    private void OnDisable()
    {
        inputActions.Main.Disable();   
    }

    private void SetRightClick(InputAction.CallbackContext ctx)
    {
        if (IsPointerOverUIElement(GetEventSystemRaycastResults())) return;
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        onMouseRightPress?.Invoke(mousePos);
        Debug.Log("MousePos R:" + mousePos.x + "; " + mousePos.y + " | " + UtillityMath.VectorToVectorInt(mousePos).x + "; " + UtillityMath.VectorToVectorInt(mousePos).y);
    }
    private void SetLeftClick(InputAction.CallbackContext ctx)
    {
        if (IsPointerOverUIElement(GetEventSystemRaycastResults())) return;
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        onMouseLeftPress?.Invoke(mousePos);
        Debug.Log("MousePos A:" + mousePos.x + "; " + mousePos.y + " | " + UtillityMath.VectorToVectorInt(mousePos).x + "; " + UtillityMath.VectorToVectorInt(mousePos).y);
    }
    private void SetMove(InputAction.CallbackContext ctx)
    {
        movementDirection = ctx.ReadValue<Vector2>();
    }
    public bool IsHoveringOverUI()
    {
        return EventSystem.current.IsPointerOverGameObject();
    }
    public static bool IsPointerOverUIElement(List<RaycastResult> eventSystemRaysastResults)
    {
        for (int index = 0; index < eventSystemRaysastResults.Count; index++)
        {
            RaycastResult curRaysastResult = eventSystemRaysastResults[index];

            if (curRaysastResult.gameObject.layer == LayerMask.NameToLayer("UI"))
                return true;
        }

        return false;
    }
    static List<RaycastResult> GetEventSystemRaycastResults()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;

        List<RaycastResult> raysastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raysastResults);

        return raysastResults;
    }
    private void SetInteractClick(InputAction.CallbackContext ctx)
    {
        onInteractPress?.Invoke();
    }

}
