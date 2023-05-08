using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class TargetScript : NetworkBehaviour
{
    [SerializeField] private bool isP1;
    [SerializeField] public Animator reverseTarget;

    [SerializeReference] private MeshRenderer flower;
    [SerializeReference] private MeshRenderer stem;
    
    private bool dieAnimationPlayed;
    private bool destroyed;

    private static readonly int Reverse = Animator.StringToHash("Reverse");

    public void TriggerTarget()
    {
        Debug.Log("Destroy call");
        if (destroyed) return;
        destroyed = true;
        // notify spawner to spawn a new target
        if (isP1)
        {
            RaceController.Instance.GetMinigameInstance().GetComponentInChildren<TargetSpawner>()
                .HitTargetP1ServerRpc(transform.position);
        }
        else
        {
            RaceController.Instance.GetMinigameInstance().GetComponentInChildren<TargetSpawner>()
                .HitTargetP2ServerRpc(transform.position);
        }

        StartCoroutine(ExplodeAnimation());
        ExplodeServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void ExplodeServerRpc()
    {
        ExplodeAnimationClientRpc();
    }
    
    [ClientRpc]
    private void ExplodeAnimationClientRpc()
    {
        StartCoroutine(ExplodeAnimation());
    }

    private IEnumerator ExplodeAnimation()
    {
        if (dieAnimationPlayed) yield break;
        dieAnimationPlayed = true;
        
        reverseTarget.SetBool(Reverse, true);
        yield return new WaitForSeconds(0.5f);
        stem.enabled = false;
        flower.enabled = false;
    }
}