 using UnityEngine;

public class TeleportMarkerScript : MonoBehaviour
{
    [SerializeField] private LineRenderer line;

    // Update is called once per frame
    void Update()
    {
        transform.position = line.GetPosition(1) + Vector3.up / 10000;
        transform.rotation = Quaternion.identity;
    }
}
    