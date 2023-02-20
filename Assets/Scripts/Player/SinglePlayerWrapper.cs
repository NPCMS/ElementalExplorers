using UnityEngine;

public class SinglePlayerWrapper : MonoBehaviour
{
    // Start is called when a player is spawned in
    private void Start()
    {
        gameObject.GetComponentInChildren<InitPlayer>().StartPlayer();
    }
}
