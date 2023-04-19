using UnityEngine;

public class TeleporterController : MonoBehaviour
{
    // player reaches teleporter
    public void OnTriggerEnter(Collider player)
    {
        if (!player.CompareTag("Player")) return;
        Debug.Log("Player entered teleporter");
        player.GetComponentInChildren<PlayerMinigameManager>().EnterMinigame();
        var raceController = RaceController.Instance;
        raceController.TeleportLocalPlayerToMinigame();
        raceController.PlayerReachedTeleporterServerRpc();
    }
}
