using UnityEngine;

public class PlayerRaceController : MonoBehaviour
{
    private float time = 0;
    public bool raceStarted = false;

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
    }
}
