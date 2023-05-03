using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BodyPoseScript : MonoBehaviour
{
    [Header("Head Ref")] [SerializeReference]
    private Transform vrCameraRef;

    [SerializeField] private Vector3 torsoOffsetFromCamera;

    [Header("Hands Ref")] [SerializeReference]
    private Transform leftHand;

    [SerializeReference] private Transform rightHand;

    [Header("Player Ref")] [SerializeReference]
    private Transform playerRef;

    // Update is called once per frame
    void LateUpdate()
    {
        // torso position should be the offset from the head
        gameObject.transform.position = vrCameraRef.position + torsoOffsetFromCamera;

        // heuristic for rotation is just average of camera forward and torso to hand vectors
        Vector3 leftHandVec = leftHand.position - playerRef.position;
        Vector3 rightHandVec = rightHand.position - playerRef.position;
        Vector3 camForward = vrCameraRef.forward;

        Vector2 leftHandXZVec = new Vector2(leftHandVec.x, leftHandVec.z).normalized;
        Vector2 rightHandXZVec = new Vector2(rightHandVec.x, rightHandVec.z).normalized;
        Vector2 camForwardXZVec = new Vector2(camForward.x, camForward.z).normalized;

        Vector2 perpCam = new Vector2(camForwardXZVec.y, -camForwardXZVec.x);
        
        // clamps hand poses so they are never behind the head and worst case are perpendicular
        if (Vector2.Dot(leftHandXZVec, camForwardXZVec) < 0) // left hand is behind head
        {
            if (Vector2.Dot(leftHandXZVec, perpCam) < 0) leftHandXZVec = -perpCam;
            else leftHandXZVec = perpCam;
        }
        
        if (Vector2.Dot(rightHandXZVec, camForwardXZVec) < 0) // right hand is behind head
        {
            if (Vector2.Dot(rightHandXZVec, perpCam) < 0) rightHandXZVec = -perpCam;
            else rightHandXZVec = perpCam;
        }

        Vector2 averageVec = (leftHandXZVec + rightHandXZVec + camForwardXZVec) / 3;
        Vector3 finalAverageVec = new Vector3(averageVec.x, 0, averageVec.y);

        transform.LookAt(transform.position + finalAverageVec);
    }
}