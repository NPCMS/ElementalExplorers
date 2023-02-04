using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private UIInteraction createLobbyBtn;
    [SerializeField] private UIInteraction joinLobbyBtn;
    [SerializeField] private TMP_Text lobbyCodeInput;
    [SerializeField] private NetworkRelay networkRelay;
    [SerializeField] private GameObject lobbyUI;
    [SerializeField] private GameObject mainMenuUI;
    [SerializeField] private LobbyMenuUI lobbyMenuUI;

    private void Awake()
    {
        createLobbyBtn.AddCallback(() =>
        {
            networkRelay.CreateRelay();
            lobbyUI.SetActive(true);
            mainMenuUI.SetActive(false);
        });

        joinLobbyBtn.AddCallback(() =>
        {
            string joinCode = lobbyCodeInput.text;
            try
            {
                networkRelay.JoinRelay(joinCode);
                lobbyMenuUI.setJoinCodeText(joinCode);

                lobbyUI.SetActive(true);
                mainMenuUI.SetActive(false);
            }
            catch (ArgumentNullException e)
            {
                Debug.Log(e);
            }
        });
    }
}
