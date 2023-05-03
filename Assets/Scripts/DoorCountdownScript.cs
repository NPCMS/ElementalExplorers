using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class DoorCountdownScript : MonoBehaviour
{
    [SerializeReference] private TextMeshProUGUI text;
    [SerializeReference] private Collider col;
    [SerializeReference] private MeshRenderer meshRenderer;
    

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
        col.enabled = false;
        meshRenderer.enabled = false;
        yield return new WaitForSeconds(0.5f);
        
        // Destroy
        Destroy(gameObject);
    } 
}
