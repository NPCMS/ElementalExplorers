using Unity.Netcode;
using UnityEngine;

public class RPCManager : NetworkBehaviour
{
    [SerializeField] private TileInfo tileInfo;
    
    public static RPCManager Instance;

    public void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void CallSetPipelineCoords(Vector2Int[] tiles, Vector2 selectedCoords, bool defaultSelected)
    {
        SetPipelineCoordsClientRpc(tiles, selectedCoords, defaultSelected);
    }

    [ClientRpc]
    private void SetPipelineCoordsClientRpc(Vector2Int[] tiles, Vector2 selectedCoords, bool defaultSelected)
    {
        Debug.Log("Set Pipeline ClientRPC recieved!");
        tileInfo.SetTiles(tiles);
        tileInfo.selectedCoords = selectedCoords;
        tileInfo.useDefault = defaultSelected;
        voiceLinePlayed = false;
    }

    private bool voiceLinePlayed;
    
    [ClientRpc]
    public void PlayReturnToSpaceShipVoiceLineClientRpc(bool isGameHost)
    {
        if (voiceLinePlayed) return;
        if ((MultiPlayerWrapper.isGameHost && isGameHost) || (!MultiPlayerWrapper.isGameHost && !isGameHost))
        {
            voiceLinePlayed = true;
            StartCoroutine(SpeakerController.speakerController.PlayAudio("12 - this player reached dropship"));
        }
        else
        {
            voiceLinePlayed = true;
            StartCoroutine(SpeakerController.speakerController.PlayAudio("12 - other player reached dropship"));
        }
        foreach (var man in FindObjectsOfType<PlayerMinigameManager>())
        {
            man.firstMinigame = true;
        }
    }
}
