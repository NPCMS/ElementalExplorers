using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    // rotation values
    private float _xRot = 0;
    private float _yRot = 0;
    
    // parameters
    [SerializeField] private float sensitivity;

    // Start is called before the first frame update
    void Start()
    {
        // hide cursor
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        // handle input
        Vector2 input = new Vector2(Input.GetAxis("Mouse X"), -Input.GetAxis("Mouse Y"));
        
        // apply inputs
        _yRot += (input.x * sensitivity);
        _xRot += (input.y * sensitivity);
        
        // clamp x rot
        _xRot = Mathf.Clamp(_xRot, -70, 70);
        
        // update transform
        transform.eulerAngles = new Vector3(_xRot, _yRot, 0);

    }
}
