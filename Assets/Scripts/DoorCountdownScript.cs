using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DoorCountdownScript : MonoBehaviour
{
    [SerializeReference] private TextMeshProUGUI text;

    private void Start()
    {
        text.text = "";
    }

    public void StartCountdown()
    {
        StartCoroutine(nameof(Countdown));
    }

    private IEnumerator Countdown()
    {
        text.text = "3";
        yield return new WaitForSeconds(1.0f);
        
        text.text = "2";
        yield return new WaitForSeconds(1.0f);
        
        text.text = "1";
        yield return new WaitForSeconds(1.0f);
        
        text.text = "GO";
        yield return new WaitForSeconds(0.3f);
        
        // Destroy
        Destroy(gameObject);
    } 
}
