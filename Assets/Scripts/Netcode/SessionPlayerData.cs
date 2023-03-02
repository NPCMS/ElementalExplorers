using Unity.BossRoom.Infrastructure;
using Unity.Multiplayer.Samples.BossRoom;
using UnityEngine;
using ISessionPlayerData = Netcode.ISessionPlayerData;

namespace Unity.BossRoom.ConnectionManagement
{
    public struct SessionPlayerData : ISessionPlayerData
    {
        public int PlayerNumber;
        public Vector3 PlayerPosition;
        public Quaternion PlayerRotation;
        public bool HasCharacterSpawned;

        public SessionPlayerData(ulong clientID, bool isConnected = false, bool hasCharacterSpawned = false)
        {
            ClientID = clientID;
            PlayerNumber = -1;
            PlayerPosition = Vector3.zero;
            PlayerRotation = Quaternion.identity;
            IsConnected = isConnected;
            HasCharacterSpawned = hasCharacterSpawned;
        }

        public bool IsConnected { get; set; }
        public ulong ClientID { get; set; }

        public void Reinitialize()
        {
            HasCharacterSpawned = false;
        }
    }
}
