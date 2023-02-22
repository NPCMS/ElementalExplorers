using UnityEngine;

public class SinglePlayerWrapper : MonoBehaviour
{
    // as the object is in single player, the spawned in player is initialised to be controlled by the user
    private void Start()
    {
        gameObject.GetComponentInChildren<InitPlayer>().StartPlayer();
    }
}
