using UnityEngine;

public class TeleporterController : MonoBehaviour
{
    // player reaches teleporter
    public void OnTriggerEnter(Collider collider)
    {
        if (!collider.CompareTag("Player")) return;
        Debug.Log("Player entered teleporter");

        var player = collider.transform.parent.gameObject;
        
        if (!player.GetComponent<InitPlayer>().IsPlayerController()) return;
        
        player.GetComponent<PlayerMinigameManager>().EnterMinigame();
        var raceController = RaceController.Instance;
        raceController.TeleportLocalPlayerToMinigame();
        raceController.PlayerReachedTeleporterServerRpc();
    }
}
