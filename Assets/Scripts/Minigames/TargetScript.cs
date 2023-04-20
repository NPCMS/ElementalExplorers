using Unity.Netcode;
using UnityEngine;

public class TargetScript : NetworkBehaviour
{
    [SerializeReference] private GameObject targetModel;
    [SerializeReference] private GameObject targetDestroyedModel;
    
    public void TriggerTarget()
    {
        TriggerTargetServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void TriggerTargetServerRpc()
    {
        // notify spawner to spawn a new target
        GetComponentInParent<TargetSpawner>().HitTarget(transform.position);

        // make target explode
        TriggerTargetClientRpc();
        
        // destroy target after 1.5s
        Invoke(nameof(DestroyTarget), 1.5f);
    }

    [ClientRpc]
    public void TriggerTargetClientRpc()
    {
        // swap models
        targetModel.SetActive(false);
        targetDestroyedModel.SetActive(true);
        
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