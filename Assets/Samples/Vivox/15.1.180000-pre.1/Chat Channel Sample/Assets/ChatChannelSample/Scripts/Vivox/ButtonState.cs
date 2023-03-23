using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonState : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private bool _isPressed;
    private bool _isDown;
    private bool _isUp;

    public bool isPressed
    {
        get
        {
            return _isPressed;
        }
    }
    public bool isDown
    {
        get
        {
            var currentIsDown = _isDown;
            _isDown = false;
            return currentIsDown;
        }
    }
    public bool isUp
    {
        get
        {
            var currentIsUp = _isUp;
            _isUp = false;
            return currentIsUp;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _isDown = true;
        _isPressed = true;
        _isUp = false;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _isDown = false;
        _isPressed = false;
        _isUp = true;
    }
}
