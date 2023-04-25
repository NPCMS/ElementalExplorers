using UnityEngine;

public class stairWalkScript : MonoBehaviour
{
    private void OnCollisionStay(Collision collisionInfo)
    {
        collisionInfo.rigidbody.AddForce(Vector3.up * 5, ForceMode.Impulse);
    }
}
