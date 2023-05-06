using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;

public class TargetScript : NetworkBehaviour
{
    [SerializeField] private bool isP1;

    private bool destroyed;

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
        // var visEffect = GetComponentInParent<VisualEffect>();
        // // if hit by this player move to player, else explode
        // visEffect.SetVector3("PlayerPosition",
        //     destroyed ? MultiPlayerWrapper.localPlayer.transform.position : transform.position);
        // visEffect.Play();
    }

    private void DestroyTarget()
    {
        transform.gameObject.GetComponent<NetworkObject>().Despawn();
    }
}