using Netcode.SceneManagement;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialState : NetworkBehaviour
{
    [SerializeField]
    private DropshipManager dropshipManager;

    private bool dropped;
    
    private string nextSceneName = "Precompute";

    private void Awake()
    {
        dropshipManager.OpenDoors();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            SceneLoaderWrapper.Instance.LoadScene(nextSceneName, true, LoadSceneMode.Additive);
        }
        
        if (!dropped && SceneManager.GetActiveScene().name == nextSceneName && dropshipManager.GetPlayersInDropship().Count == 2)
        {
            dropped = true;
            StartCoroutine(dropshipManager.Drop());
        }
    }
}
