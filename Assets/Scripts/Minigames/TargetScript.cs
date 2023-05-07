using Unity.Netcode;
using UnityEngine;

public class TargetScript : NetworkBehaviour
{
    [SerializeField] private bool isP1;
    [SerializeField] public Animator reverseTarget;

    private bool destroyed;
    private static readonly int Reverse = Animator.StringToHash("Reverse");

    public void TriggerTarget()
    {
        TriggerTargetServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void TriggerTargetServerRpc(ServerRpcParams rpcParams = default)
    {
        Debug.Log("Destroy call");
        if (destroyed) return;
        destroyed = true;
        // notify spawner to spawn a new target
        if (isP1)
        {
            RaceController.Instance.GetMinigameInstance().GetComponentInChildren<TargetSpawner>()
                .HitTargetP1(transform.position, MultiPlayerWrapper.localPlayer.OwnerClientId == rpcParams.Receive.SenderClientId);
        }
        else
        {
            RaceController.Instance.GetMinigameInstance().GetComponentInChildren<TargetSpawner>()
                .HitTargetP2(transform.position, MultiPlayerWrapper.localPlayer.OwnerClientId != rpcParams.Receive.SenderClientId);
        }

        Explode();
    }

    public void Explode()
    {
        // make target explode
        TriggerTargetClientRpc();
        // destroy target after 1.5s
        Invoke(nameof(DestroyTarget), 0.5f);
    }

    [ClientRpc]
    private void TriggerTargetClientRpc()
    {
        Debug.Log("trigger target destroy");
        reverseTarget.SetBool(Reverse, true);
    }

    private void DestroyTarget()
    {
        transform.gameObject.GetComponent<NetworkObject>().Despawn();
    }
}