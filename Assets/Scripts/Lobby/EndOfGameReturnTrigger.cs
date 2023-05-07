using System.Collections;
using Netcode.ConnectionManagement;
using Netcode.ConnectionManagement.ConnectionState;
using Netcode.SceneManagement;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EndOfGameReturnTrigger : NetworkBehaviour
{
    private ConnectionManager _connectionManager;

    private bool voiceLinePlayed;
    
    private void Awake()
    {
        _connectionManager = FindObjectOfType<ConnectionManager>();
    }
    
    private void OnTriggerEnter (Collider col) {
        // Add the GameObject collided with to the list.
        if (_connectionManager.m_CurrentState is OfflineState || (MultiPlayerWrapper.isGameHost && col.CompareTag("Player")))
        {
            StartCoroutine(TeleportToSpaceShip(col.GetComponentInParent<MultiPlayerWrapper>() == MultiPlayerWrapper.localPlayer));
        }
    }

    private IEnumerator TeleportToSpaceShip(bool isGameHost)
    {
        PlayVoiceLineClientRpc(isGameHost);
        yield return new WaitForSeconds(10f);
        SceneLoaderWrapper.Instance.LoadScene("SpaceshipScene", true, LoadSceneMode.Additive);
    }

    [ClientRpc]
    private void PlayVoiceLineClientRpc(bool isGameHost)
    {
        if (voiceLinePlayed) return;
        voiceLinePlayed = true;
        if ((MultiPlayerWrapper.isGameHost && isGameHost) || (!MultiPlayerWrapper.isGameHost && !isGameHost))
        {
            StartCoroutine(SpeakerController.speakerController.PlayAudio("12 - this player reached dropship"));
        }
        else
        {
            StartCoroutine(SpeakerController.speakerController.PlayAudio("12 - other player reached dropship"));
        }
        foreach (var man in FindObjectsOfType<PlayerMinigameManager>())
        {
            man.firstMinigame = true;
        }
    }
}
