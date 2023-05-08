using UnityEngine;

public class UIDirectionControl : MonoBehaviour
{
    // This class is used to make sure world space UI
    // elements such as the health bar face the correct direction.

    private void Start()
    {
    }


    private void Update()
    {
        gameObject.transform.LookAt(Camera.main.transform);
    }
}