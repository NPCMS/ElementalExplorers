using UnityEngine;

public class CheckpointController : MonoBehaviour
{
    [SerializeField] private int checkpoint;
    [SerializeField] private bool finish;
    public RaceController raceController;
    private bool passed;

    public void PassCheckpoint(float time)
    {
        if (passed) return;
        passed = true;

        if (raceController != null)
        {
            gameObject.GetComponent<MeshRenderer>().enabled = false;
            raceController.SetCheckPointServerRPC(checkpoint, time);
            if (finish)
            {
                Debug.Log("Finished!!!!!");
            }
        }
        else
        {
            Debug.LogWarning("Checkpoint not connected to race controller");
            gameObject.GetComponent<MeshRenderer>().enabled = false;
        }
    }
}
