using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ScoreScreenUI : MonoBehaviour
{

    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text winLossText;

    [SerializeField] private GameObject newGameBtn;

    private string winText = "You Win!";
    private string lossText = "You Lose!";
    private string drawText = "It's a draw";
    
    void Awake()
    {
        if(!newGameBtn.TryGetComponent<UIInteraction>(out UIInteraction interaction))
            Debug.LogError("No UI interaction script");
        
        interaction.AddCallback((RaycastHit hit, SteamInputCore.Button button) =>
        {
            // new game
            
        });
    }

    public void setScore(int playerScore, int otherScore)
    {
        if (playerScore > otherScore)
            winLossText.text = winText;
        else if (playerScore < otherScore)
            winLossText.text = lossText;
        else
            winLossText.text = drawText;

        scoreText.text = $"{playerScore} - {otherScore}";
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
