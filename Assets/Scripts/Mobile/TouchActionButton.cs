using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Pointer-down responds immediately; holding Fire repeats at the weapon cooldown.
public sealed class TouchActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public enum ActionKind { Jump, Fire }
    public ActionKind action;
    public PlayerController player;
    public PlayerShooter shooter;
    const int NoPointer = int.MinValue;
    int pointer = NoPointer;
    Image image;
    Color normal;

    void Awake() { image = GetComponent<Image>(); if (image != null) normal = image.color; }
    public void OnPointerDown(PointerEventData e)
    {
        if (pointer != NoPointer || e.button != PointerEventData.InputButton.Left) return;
        pointer = e.pointerId;
        if (image != null) image.color = normal * new Color(0.75f, 0.75f, 0.75f, 1f);
        if (action == ActionKind.Jump && player != null) player.TryJump();
        if (action == ActionKind.Fire && shooter != null) shooter.TryFire();
    }
    public void OnPointerUp(PointerEventData e) { if (pointer == e.pointerId) ResetInput(); }
    void Update()
    {
        if (pointer != NoPointer && action == ActionKind.Fire && shooter != null) shooter.TryFire();
    }
    public void ResetInput()
    {
        pointer = NoPointer;
        if (image != null) image.color = normal;
    }
    void OnDisable() { ResetInput(); }
    void OnApplicationFocus(bool focused) { if (!focused) ResetInput(); }
    void OnApplicationPause(bool paused) { if (paused) ResetInput(); }
}
