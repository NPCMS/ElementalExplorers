using UnityEngine;

namespace Netcode.SessionManagement
{
    public struct SessionPlayerData : ISessionPlayerData
    {
        public int PlayerNumber;
        public Vector3 PlayerPosition;
        public Quaternion PlayerRotation;
        public bool HasCharacterSpawned;
        public GameObject SpawnedPlayer;

        public SessionPlayerData(ulong clientID, bool isConnected = false, bool hasCharacterSpawned = false, GameObject spawnedPlayer = null)
        {
            ClientID = clientID;
            PlayerNumber = -1;
            PlayerPosition = Vector3.zero;
            PlayerRotation = Quaternion.identity;
            IsConnected = isConnected;
            HasCharacterSpawned = hasCharacterSpawned;
            SpawnedPlayer = spawnedPlayer;
        }

        public bool IsConnected { get; set; }
        public ulong ClientID { get; set; }

        public void Reinitialize()
        {
            HasCharacterSpawned = false;
            SpawnedPlayer = null;
        }
    }
}
