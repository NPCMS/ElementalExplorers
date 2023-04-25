using System;
using UnityEngine;

public class WingSuit : MonoBehaviour
{
    [SerializeReference] private Transform head;
    [SerializeReference] private Transform leftHand;
    [SerializeReference] private Transform rightHand;
    [SerializeReference] private Rigidbody body;

    [SerializeField] private float liftForce;
    [SerializeField] private float maxForce;
    [SerializeField] private float minAngleMult;
    
    private void Update()
    {
        if (body.velocity.magnitude == 0) return;
        // vector between hands
        Vector3 handVector = (rightHand.position - leftHand.position).normalized;
        
        // rotate head quart so right is parallel to handVector
        var headRotation = Quaternion.FromToRotation(head.right, handVector);

        var liftDirection = headRotation * head.up;
        var forwardsDirection = headRotation * head.forward;
        var sideDirection = headRotation * head.right;
        Debug.DrawRay(head.position, liftDirection, Color.green);
        // Debug.DrawRay(head.position, forwardsDirection);

        // apply force in direction h'.up using velocity wrt h'.forwards
        float angleMultiplier = Math.Max(minAngleMult, Vector3.Dot(forwardsDirection, body.velocity.normalized));
        float upwardMultiplier;
        if (body.velocity.y > 0)
            upwardMultiplier = Math.Max(0, (float)Math.Sin(Vector3.Angle(Vector3.up, body.velocity.normalized)));
        else
            upwardMultiplier = 1;
        Debug.DrawRay(head.position, 5 * forwardsDirection, Color.red);
        Debug.DrawRay(head.position, 5 * body.velocity.normalized, Color.blue);
        float liftMultiplier = Math.Min(maxForce, angleMultiplier * body.velocity.magnitude * liftForce);
        Debug.Log("Angle multi: " + liftMultiplier + " Force Mult: " + liftMultiplier + " Resultant Vector: " + liftDirection);
        Vector3 addedForce = liftDirection * (liftMultiplier * Time.deltaTime);
        addedForce.y *= upwardMultiplier;
        forwardsDirection.y = 0;
        body.AddForce(liftDirection * (liftMultiplier * Time.deltaTime), ForceMode.VelocityChange);
        body.AddForce(forwardsDirection.normalized * (0.3f * liftMultiplier * Time.deltaTime), ForceMode.VelocityChange);

        sideDirection.y = 0;
        liftDirection.y = 0;
        float sideForce = 0.3f * Vector3.Dot(sideDirection.normalized, liftDirection.normalized) * liftMultiplier * Time.deltaTime;
        body.AddForce(sideDirection.normalized * sideForce, ForceMode.VelocityChange);
    }
}
