using Unity.Netcode;
using UnityEngine;

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
    private void TriggerTargetServerRpc()
    {
        Debug.Log("Destroy call");
        if (destroyed) return;
        destroyed = true;
        // notify spawner to spawn a new target
        if (isP1)
        {
            RaceController.Instance.GetMinigameInstance().GetComponentInChildren<TargetSpawner>()
                .HitTargetP1(transform.position, IsHost);
        }
        else
        {
            RaceController.Instance.GetMinigameInstance().GetComponentInChildren<TargetSpawner>()
                .HitTargetP2(transform.position, !IsHost);
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