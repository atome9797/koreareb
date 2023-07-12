using UnityEngine;
using UnityEngine.EventSystems;

public class CursorEvent : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private CursorControl cursorControl;

    private void Awake()
    {
        cursorControl = FindObjectOfType<CursorControl>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        cursorControl.SetPointerCursor();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        cursorControl.SetDefaultCursor();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        cursorControl.SetDefaultCursor();
    }
}