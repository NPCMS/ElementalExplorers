using UnityEngine;

public class CheckpointController : MonoBehaviour
{
    [SerializeField] public int checkpoint;
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
            raceController.PassCheckpoint(checkpoint, time, finish);
            if (finish)
            {
                Debug.Log("Finished!!!!!");
                gameObject.GetComponent<ParticleSystem>().Play();
            }
        }
        else
        {
            Debug.LogWarning("Checkpoint not connected to race controller");
            gameObject.GetComponent<MeshRenderer>().enabled = false;
        }
    }
}
