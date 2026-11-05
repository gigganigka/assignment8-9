using UnityEngine;
using UnityEngine.EventSystems;

// A real pointer/touch joystick: no virtual Gamepad or device pairing is involved.
// It owns one pointer ID, so a second finger can press Jump or Fire independently.
public sealed class TouchStick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public RectTransform knob;
    public PlayerController player;
    public float radius = 90f;
    const int NoPointer = int.MinValue;
    int pointer = NoPointer;

    public void OnPointerDown(PointerEventData e)
    {
        if (pointer != NoPointer || e.button != PointerEventData.InputButton.Left) return;
        pointer = e.pointerId;
        OnDrag(e);
    }
    public void OnDrag(PointerEventData e)
    {
        if (pointer != e.pointerId) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)transform, e.position, e.pressEventCamera, out Vector2 local)) return;
        Vector2 delta = Vector2.ClampMagnitude(local, radius);
        if (knob != null) knob.anchoredPosition = delta;
        if (player != null) player.SetTouchMove(delta.x / Mathf.Max(1f, radius), true);
    }
    public void OnPointerUp(PointerEventData e)
    {
        if (pointer == e.pointerId) ResetInput();
    }
    public void ResetInput()
    {
        pointer = NoPointer;
        if (knob != null) knob.anchoredPosition = Vector2.zero;
        if (player != null) player.SetTouchMove(0f, false);
    }
    void OnDisable() { ResetInput(); }
    void OnApplicationFocus(bool focused) { if (!focused) ResetInput(); }
    void OnApplicationPause(bool paused) { if (paused) ResetInput(); }
}
