using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VivoxUnity;
using System.Linq;
using System;

public class RosterManager : MonoBehaviour
{
    private const string LobbyChannelName = "lobbyChannel";
    private VivoxVoiceManager _vivoxVoiceManager;

    private Dictionary<ChannelId, List<RosterItem>> rosterObjects = new Dictionary<ChannelId, List<RosterItem>>();

    public GameObject rosterItemPrefab;

    // Start is called before the first frame update
    void Start()
    {
    }

    private void Awake()
    {
        _vivoxVoiceManager = VivoxVoiceManager.Instance;
        _vivoxVoiceManager.OnParticipantAddedEvent += OnParticipantAdded;
        _vivoxVoiceManager.OnParticipantRemovedEvent += OnParticipantRemoved;
        _vivoxVoiceManager.OnUserLoggedOutEvent += OnUserLoggedOut;
        if (_vivoxVoiceManager &&  _vivoxVoiceManager.ActiveChannels.Count > 0)
        {
            var LobbyChannel = _vivoxVoiceManager.ActiveChannels.FirstOrDefault(ac => ac.Channel.Name == LobbyChannelName);
            foreach (var participant in _vivoxVoiceManager.LoginSession.GetChannelSession(LobbyChannel.Channel).Participants)
            {
                UpdateParticipantRoster(participant, participant.ParentChannelSession.Channel, true);
            }
        }
    }

    private void OnDestroy()
    {
        _vivoxVoiceManager.OnParticipantAddedEvent -= OnParticipantAdded;
        _vivoxVoiceManager.OnParticipantRemovedEvent -= OnParticipantRemoved;
        _vivoxVoiceManager.OnUserLoggedOutEvent -= OnUserLoggedOut;
    }

    public void ClearAllRosters()
    {
        foreach(List<RosterItem> rosterList in rosterObjects.Values)
        {
            foreach(RosterItem item in rosterList)
            {
                Destroy(item.gameObject);
            }
            rosterList.Clear();
        }
        rosterObjects.Clear();
    }

    public void ClearChannelRoster(ChannelId channel)
    {
        List<RosterItem> rosterList = rosterObjects[channel];
        foreach(RosterItem item in rosterList)
        {
            Destroy(item.gameObject);
        }
        rosterList.Clear();
        rosterObjects.Remove(channel);
    }

    private void CleanRoster(ChannelId channel)
    {
        RectTransform rt = this.gameObject.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, rosterObjects[channel].Count * 50);
    }

    private void OnChannelDisconnected(ChannelId channel, ConnectionState audioConnectionState)
    {
        if(rosterObjects.Keys.Contains(channel))
        {
            ClearChannelRoster(channel);
        }
    }

    private void OnUserLoggedOut()
    {
        ClearAllRosters();
    }

    void UpdateParticipantRoster(IParticipant participant, ChannelId channel, bool isAddParticipant)
    {
        if (isAddParticipant)
        {
            GameObject newRosterObject = GameObject.Instantiate(rosterItemPrefab, this.gameObject.transform);
            RosterItem newRosterItem = newRosterObject.GetComponent<RosterItem>();
            List<RosterItem> thisChannelList;

            if (rosterObjects.ContainsKey(channel))
            {
                //Add this object to an existing roster
                rosterObjects.TryGetValue(channel, out thisChannelList);
                newRosterItem.SetupRosterItem(participant);
                thisChannelList.Add(newRosterItem);
                rosterObjects[channel] = thisChannelList;
            }
            else
            {
                //Create a new roster to add this object to
                thisChannelList = new List<RosterItem>();
                thisChannelList.Add(newRosterItem);
                newRosterItem.SetupRosterItem(participant);
                rosterObjects.Add(channel, thisChannelList);
            }
            CleanRoster(channel);
        }
        else
        {
            if (rosterObjects.ContainsKey(channel))
            {
                RosterItem removedItem = rosterObjects[channel].FirstOrDefault(p => p.Participant.Account.Name == participant.Account.Name);
                if (removedItem != null)
                {
                    rosterObjects[channel].Remove(removedItem);
                    Destroy(removedItem.gameObject);
                    CleanRoster(channel);
                }
                else
                {
                    Debug.LogError("Trying to remove a participant that has no roster item.");
                }
            }
        }

    }

    void OnParticipantAdded(string userName, ChannelId channel, IParticipant participant)
    {
        Debug.Log("OnPartAdded: " + userName);
        UpdateParticipantRoster(participant, channel, true);
    }

    void OnParticipantRemoved(string userName, ChannelId channel, IParticipant participant)
    {
        Debug.Log("OnPartRemoved: " + participant.Account.Name);
        UpdateParticipantRoster(participant, channel, false);
    }
}