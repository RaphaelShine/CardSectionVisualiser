using System;
using UnityEngine;
using UnityEngine.InputSystem;

// 마우스·터치 입력을 읽는다. 가사를 누르면 그 가사의 박을 알린다
public class InputManager : MonoBehaviour
{
    [SerializeField] private UIManager uiManager;

    // 누른 가사의 박. GameManager가 받아 시점을 옮긴다
    public event Action<int> OnLyricSelected;

    private void Update()
    {
        if (TryGetPress(out Vector2 position) && uiManager.TryGetLyricBeat(position, out int beat))
            OnLyricSelected?.Invoke(beat);

        // 가사 위에서 휠을 굴리면 가사 목록을 넘긴다
        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            float wheel = mouse.scroll.ReadValue().y;
            if (wheel != 0f && uiManager.IsOverLyrics(mouse.position.ReadValue()))
                uiManager.Scroll(-Mathf.Sign(wheel));
        }
    }

    // 이번 프레임에 눌린 위치(스크린 좌표)
    private static bool TryGetPress(out Vector2 position)
    {
        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            position = mouse.position.ReadValue();
            return true;
        }

        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen != null && touchscreen.primaryTouch.press.wasPressedThisFrame)
        {
            position = touchscreen.primaryTouch.position.ReadValue();
            return true;
        }

        position = default;
        return false;
    }
}
