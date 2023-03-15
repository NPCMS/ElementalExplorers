using System.Collections;
using TMPro;
using UnityEngine;

public class HUDController : MonoBehaviour
{
    [SerializeReference] private TextMeshPro timer;
    [SerializeReference] private TextMeshPro countdown;

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

    // Shows 3 2 1 go!!!!
    public void StartCountdown()
    {
        StartCoroutine(CountdownRoutine());
    }

    private IEnumerator CountdownRoutine()
    {
        countdown.text = "3";
        yield return new WaitForSeconds(1);
        countdown.text = "2";
        yield return new WaitForSeconds(1);
        countdown.text = "1";
        yield return new WaitForSeconds(1);
        countdown.text = "GO!";
        yield return new WaitForSeconds(1);
        countdown.text = "";
    }
}
