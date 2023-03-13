using System.Collections.Generic;
using Unity.Multiplayer.Samples.Utilities;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Netcode.SceneManagement
{
    public class SceneLoaderWrapper : NetworkBehaviour
    {
        /// <summary>
        /// Manages a loading screen by wrapping around scene management APIs. It loads scene using the SceneManager,
        /// or, on listening servers for which scene management is enabled, using the NetworkSceneManager and handles
        /// the starting and stopping of the loading screen.
        /// </summary>

        [SerializeField]
        ClientLoadingScreen m_ClientLoadingScreen;

        bool IsNetworkSceneManagementEnabled => NetworkManager != null && NetworkManager.SceneManager != null && NetworkManager.NetworkConfig.EnableSceneManagement;

        public static SceneLoaderWrapper Instance { get; protected set; }

        public virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
            }
            DontDestroyOnLoad(this);
        }

        public virtual void Start()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        public override void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            base.OnDestroy();
        }

        public override void OnNetworkDespawn()
        {
            if (NetworkManager != null && NetworkManager.SceneManager != null)
            {
                NetworkManager.SceneManager.OnSceneEvent -= OnSceneEvent;
            }
        }

        /// <summary>
        /// Initializes the callback on scene events. This needs to be called right after initializing NetworkManager
        /// (after StartHost, StartClient or StartServer)
        /// </summary>
        public virtual void AddOnSceneEventCallback()
        {
            if (IsNetworkSceneManagementEnabled)
            {
                NetworkManager.SceneManager.OnSceneEvent += OnSceneEvent;
            }
        }

        /// <summary>
        /// Loads a scene asynchronously using the specified loadSceneMode, with NetworkSceneManager if on a listening
        /// server with SceneManagement enabled, or SceneManager otherwise. If a scene is loaded via SceneManager, this
        /// method also triggers the start of the loading screen.
        /// </summary>
        /// <param name="sceneName">Name or path of the Scene to load.</param>
        /// <param name="useNetworkSceneManager">If true, uses NetworkSceneManager, else uses SceneManager</param>
        /// <param name="loadSceneMode">If LoadSceneMode.Single then all current Scenes will be unloaded before loading.</param>
        public virtual void LoadScene(string sceneName, bool useNetworkSceneManager, LoadSceneMode loadSceneMode = LoadSceneMode.Single)
        {
            if (useNetworkSceneManager)
            {
                if (IsSpawned && IsNetworkSceneManagementEnabled && !NetworkManager.ShutdownInProgress)
                {
                    if (NetworkManager.IsServer)
                    {
                        // If is active server and NetworkManager uses scene management, load scene using NetworkManager's SceneManager
                        NetworkManager.SceneManager.LoadScene(sceneName, loadSceneMode);
                    }
                }
            }
            else
            {
                // Load using SceneManager
                var loadOperation = SceneManager.LoadSceneAsync(sceneName, loadSceneMode);
                if (loadSceneMode == LoadSceneMode.Single)
                {
                    m_ClientLoadingScreen.StartLoadingScreen(sceneName);
                }
            }
        }

        void OnSceneLoaded(Scene secondScene, LoadSceneMode loadSceneMode)
        {
            if (!IsSpawned || NetworkManager.ShutdownInProgress)
            {
                m_ClientLoadingScreen.StopLoadingScreen();
            }

            if (loadSceneMode == LoadSceneMode.Additive)
            {
                Scene firstScene = SceneManager.GetActiveScene();
                MatchEntryToExit(firstScene, secondScene);
                MovePlayersToNewScene(secondScene);

                // Set the Active scene to scene B
                SceneManager.SetActiveScene(secondScene);
            }
        }

        void OnSceneEvent(SceneEvent sceneEvent)
        {
            switch (sceneEvent.SceneEventType)
            {
                case SceneEventType.Load: // Server told client to load a scene
                    // Only executes on client
                    if (NetworkManager.IsClient)
                    {
                        // Only start a new loading screen if scene loaded in Single mode, else simply update
                        if (sceneEvent.LoadSceneMode == LoadSceneMode.Single)
                        {
                            m_ClientLoadingScreen.StartLoadingScreen(sceneEvent.SceneName);
                        }
                    }
                    break;
                case SceneEventType.LoadEventCompleted: // Server told client that all clients finished loading a scene
                    // Only executes on client
                    if (NetworkManager.IsClient)
                    {
                        m_ClientLoadingScreen.StopLoadingScreen();
                    }
                    break;
                case SceneEventType.Synchronize: // Server told client to start synchronizing scenes
                {
                    // todo: this is a workaround that could be removed once MTT-3363 is done
                    // Only executes on client that is not the host
                    if (NetworkManager.IsClient && !NetworkManager.IsHost)
                    {
                        // unload all currently loaded additive scenes so that if we connect to a server with the same
                        // main scene we properly load and synchronize all appropriate scenes without loading a scene
                        // that is already loaded.
                        UnloadAdditiveScenes();
                    }
                    break;
                }
                case SceneEventType.SynchronizeComplete: // Client told server that they finished synchronizing
                    // Only executes on server
                    if (NetworkManager.IsServer)
                    {
                        // Send client RPC to make sure the client stops the loading screen after the server handles what it needs to after the client finished synchronizing, for example character spawning done server side should still be hidden by loading screen.
                        StopLoadingScreenClientRpc(new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { sceneEvent.ClientId } } });
                    }
                    break;
            }
        }

        public void UnloadAdditiveScenes()
        {
            var activeScene = SceneManager.GetActiveScene();
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && scene != activeScene)
                {
                    SceneManager.UnloadSceneAsync(scene);
                }
            }
        }

        private void MatchEntryToExit(Scene firstScene, Scene secondScene)
        {
            // Get the first scene exit point
            Transform exit = GetConnectionPoint(firstScene, false);

            // Get the second scene entry point
            Transform entry = GetConnectionPoint(secondScene, true);

            // Move Second Scene entry point to first scene exit point
            List<GameObject> secondObjects = new List<GameObject>();
            secondScene.GetRootGameObjects(secondObjects);  
            Vector3 translation = exit.position - entry.position;
            foreach (GameObject o in secondObjects)
            {
                o.transform.position += translation;
            }
            float angle = exit.rotation.eulerAngles.y - entry.rotation.eulerAngles.y;
            foreach (GameObject o in secondObjects)
            {
                o.transform.RotateAround(exit.position, Vector3.up, angle);
            }
        }

        private Transform GetConnectionPoint(Scene scene, bool entry)
        {
            string pointName;
            if (entry)
            {
                pointName = "Entry";
            }
            else
            {
                pointName = "Exit";
            }
        
            List<GameObject> sceneObjects = new List<GameObject>();
            scene.GetRootGameObjects(sceneObjects);
            GameObject connection = sceneObjects.Find(x => x.name == pointName);
            return connection.transform;
        }

        private void MovePlayersToNewScene(Scene secondScene)
        {
            // Move the Players to the other scene
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            foreach (var player in players)
            {
                if (player.transform.root == player.transform)
                {
                    SceneManager.MoveGameObjectToScene(player, secondScene);
                }
            }
        }

        [ClientRpc]
        void StopLoadingScreenClientRpc(ClientRpcParams clientRpcParams = default)
        {
            m_ClientLoadingScreen.StopLoadingScreen();
        }
    }
}
