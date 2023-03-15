using TMPro;
using UnityEngine;

public class HUDController : MonoBehaviour
{
    [SerializeField] private TextMeshPro timer;
    
    private bool trackingPlayer;
    private bool trackingCheckpoint;
    private Transform cam;

    public void UpdateTime(float time)
    {
        if (time > 60)
        {
            timer.text = Mathf.Floor(time / 60) + "m " + (time % 60).ToString("0.00") + "s";
        }
        else
        {
            timer.text = time.ToString("0.00") + "s";
        }
    }
}
