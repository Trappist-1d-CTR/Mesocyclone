using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonEventSystem : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler, IPointerDownHandler, IPointerUpHandler
{
    public bool PointerOverElement = false;
    public static bool PointerClick = false;
    public static int PointerClickButtons = -1;
    public static bool[] PointerDownButtons = new bool[3] { false, false, false };
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
        PointerDownButtons[(int)data.button] = true;
        PointerClick = true;
    }

    public void OnPointerUp(PointerEventData data)
    {
        PointerDownButtons[(int)data.button] = false;
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

    public static int PointerDownNumber()
    {
        return (PointerDownButtons[0] ? 1 : 0) + (PointerDownButtons[1] ? 1 : 0) + (PointerDownButtons[2] ? 1 : 0);
    }

    public static bool PointerDown(params int[] Buttons)
    {
        foreach (int id in Buttons)
        {
            if (!PointerDownButtons[id])
                return false;
        }

        return Buttons.Length == PointerDownNumber();
    }
}
