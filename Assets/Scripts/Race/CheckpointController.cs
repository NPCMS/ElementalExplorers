using UnityEngine;

public class CheckpointController : MonoBehaviour
{
    [SerializeField] public int checkpoint;
    [SerializeField] private bool finish;
    public RaceController raceController;
    public bool passed;

    public void PassCheckpoint(float time)
    {
        if (passed) return;
        if (raceController != null)
        {
            raceController.PassCheckpoint(checkpoint, time, finish); 
        }
        else
        {
            Debug.LogWarning("Checkpoint not connected to race controller");
            gameObject.SetActive(false);
        }
    }
}
