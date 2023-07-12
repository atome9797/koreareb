using UnityEngine;

public class CursorControl : MonoBehaviour
{
    public Texture2D pointerCursor; // 포인터 커서 이미지

    private void Start()
    {
        SetDefaultCursor();
    }

    public void SetPointerCursor()
    {
        Cursor.SetCursor(pointerCursor, Vector2.zero, CursorMode.Auto);
    }
    
    public void SetDefaultCursor()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
}

