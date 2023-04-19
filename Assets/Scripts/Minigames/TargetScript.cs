using UnityEngine;

public class TargetScript : MonoBehaviour
{
    [SerializeReference] private GameObject
        targetModel;
    [SerializeReference] private GameObject
        targetDestroyedModel;

    // Start is called before the first frame update
    public void TriggerTarget()
    {
        // notify spawner
        GetComponentInParent<TargetSpawner>().HitTarget(transform.position);

        // swap models
        targetModel.SetActive(false);
        targetDestroyedModel.SetActive(true);
        // begin destroy animation by adding force
        foreach (Rigidbody rb in targetDestroyedModel.transform.GetComponentsInChildren<Rigidbody>())
        {
            rb.AddExplosionForce(250f, targetDestroyedModel.transform.position, 5);
        }
        Invoke(nameof(DestroyTarget), 1.5f);
    }

    private void DestroyTarget()
    {
        Destroy(transform.parent.gameObject);
    }
}