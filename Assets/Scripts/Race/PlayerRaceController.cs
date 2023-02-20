using UnityEngine;

public class PlayerRaceController : MonoBehaviour
{

    [SerializeField] private HUDController hudController;
    
    private float time;
    public bool raceStarted;

    private void OnTriggerEnter(Collider other)
    {
        if (!enabled) return;
        if (other.gameObject.CompareTag("Checkpoint"))
        {
            other.gameObject.GetComponent<CheckpointController>().PassCheckpoint(time);
        }
    }

    private void Update()
    {
        if (!raceStarted) return;
        time += Time.deltaTime;
        hudController.UpdateTime(time);
    }
}
