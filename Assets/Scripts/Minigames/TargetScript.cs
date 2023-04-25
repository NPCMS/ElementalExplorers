using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;

public class TargetScript : NetworkBehaviour
{
    [SerializeReference] private GameObject targetModel;
    [SerializeReference] private GameObject targetDestroyedModel;
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
        Invoke(nameof(DestroyTarget), 1.5f);
    }

    [ClientRpc]
    private void TriggerTargetClientRpc()
    {
        Debug.Log("trigger target destroy");
        // swap models
        targetModel.SetActive(false);
        targetDestroyedModel.SetActive(true);
        
        var visEffect = GetComponentInParent<VisualEffect>();
        // if hit by this player move to player, else explode
        visEffect.SetVector3("PlayerPosition",
            destroyed ? MultiPlayerWrapper.localPlayer.transform.position : transform.position);
        visEffect.Play();

        // begin destroy animation by adding force
        foreach (Rigidbody rb in targetDestroyedModel.transform.GetComponentsInChildren<Rigidbody>())
        {
            rb.AddExplosionForce(250f, targetDestroyedModel.transform.position, 5);
        }
    }

    private void DestroyTarget()
    {
        transform.parent.gameObject.GetComponent<NetworkObject>().Despawn();
    }
}