using TMPro;
using UnityEngine;

public class ScoreScreenUI : MonoBehaviour
{

    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text winLossText;

    [SerializeReference] private LobbyMenuUI lobbyMenuUI;
    [SerializeReference] private GameObject tutorial;

    [SerializeField] private UIInteraction newGameBtn;

    private string winText = "You Win!";
    private string lossText = "You Lose!";
    private string drawText = "It's a draw";
    
    void Awake()
    {
        tutorial.SetActive(false);
        
        newGameBtn.AddCallback((RaycastHit hit, SteamInputCore.Button button) =>
        {
            // new game
            tutorial.SetActive(true);
            lobbyMenuUI.RemoveScoreScreen();
            gameObject.SetActive(false);
        });
    }

    public void SetScore(int playerScore, int otherScore)
    {
        if (playerScore > otherScore)
            winLossText.text = winText;
        else if (playerScore < otherScore)
            winLossText.text = lossText;
        else
            winLossText.text = drawText;

        scoreText.text = $"{playerScore} - {otherScore}";
    }
}
