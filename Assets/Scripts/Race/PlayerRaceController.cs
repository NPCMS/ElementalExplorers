using UnityEngine;

public class PlayerRaceController : MonoBehaviour
{

    [SerializeReference] public HUDController hudController;
    

    private void OnTriggerEnter(Collider other)
    {
        if (!enabled) return;
        if (other.gameObject.CompareTag("Checkpoint"))
        {
            other.gameObject.GetComponent<CheckpointController>().PassCheckpoint();
        }
    }
}
