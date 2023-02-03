using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button createLobbyBtn;
    [SerializeField] private Button joinLobbyBtn;
    [SerializeField] private TMP_InputField lobbyCodeInput;
    [SerializeField] private NetworkRelay networkRelay;
    [SerializeField] private GameObject lobbyUI;
    [SerializeField] private GameObject mainMenuUI;
    [SerializeField] private LobbyMenuUI lobbyMenuUI;

    private void Awake()
    {
        // Set restrictions on entered text
        lobbyCodeInput.characterValidation = TMP_InputField.CharacterValidation.Alphanumeric;
        lobbyCodeInput.characterLimit = 6;
        lobbyCodeInput.lineType = TMP_InputField.LineType.SingleLine;
        lobbyCodeInput.onValidateInput += delegate (string s, int i, char c) { return char.ToUpper(c); };

        createLobbyBtn.onClick.AddListener(() =>
        {
            networkRelay.CreateRelay();
            lobbyUI.SetActive(true);
            mainMenuUI.SetActive(false);
        });

        joinLobbyBtn.onClick.AddListener(() =>
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
