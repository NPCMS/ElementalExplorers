using System.Collections;
using Netcode.ConnectionManagement;
using Netcode.ConnectionManagement.ConnectionState;
using Netcode.SceneManagement;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EndOfGameReturnTrigger : MonoBehaviour
{
    private ConnectionManager _connectionManager;

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
        RPCManager.Instance.PlayReturnToSpaceShipVoiceLineClientRpc(isGameHost);
        yield return new WaitForSeconds(10f);
        SceneLoaderWrapper.Instance.LoadScene("SpaceshipScene", true, LoadSceneMode.Additive);
    }
}
