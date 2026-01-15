using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonEventSystem : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler, IPointerDownHandler, IPointerUpHandler
{
    public bool PointerOverElement = false;
    public static bool PointerClick = false;
    public static int PointerClickButtons = -1;
    public static Vector2 PointerPos = Vector2.zero;
    public static Vector2 PointerDeltaPos = Vector2.zero;

    private static bool CanResetDeltaPos;

    public void OnPointerEnter(PointerEventData data)
    {
        PointerOverElement = true;
    }

    public void OnPointerExit(PointerEventData data)
    {
        PointerOverElement = false;
    }

    public void OnPointerClick(PointerEventData data)
    {    }

    public void OnPointerDown(PointerEventData data)
    {
        PointerClickButtons = (int)data.button;
        PointerClick = true;
    }

    public void OnPointerUp(PointerEventData data)
    {
        PointerClick = false;
    }

    public void OnPointerMove(PointerEventData data)
    {
        PointerPos = data.position;
        PointerDeltaPos = data.delta;
        CanResetDeltaPos = false;
    }

    public void Update()
    {
        if (!PointerClick && PointerClickButtons != -1)
        {
            PointerClickButtons = -1;
        }

        if (!CanResetDeltaPos)
            CanResetDeltaPos = true;
        else if (PointerDeltaPos != Vector2.zero)
            PointerDeltaPos = Vector2.zero;
    }
}
