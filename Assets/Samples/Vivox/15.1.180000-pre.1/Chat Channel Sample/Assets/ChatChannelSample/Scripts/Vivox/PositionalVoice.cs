using System;
using UnityEngine;
using UnityEngine.Events;
using VivoxUnity;

[Serializable]
public class PositionalVoice : MonoBehaviour
{
    public class OnParticipantProperty : UnityEvent<IParticipant, System.ComponentModel.PropertyChangedEventArgs> { }
    public OnParticipantProperty m_ParticipantPropertyEvent = new OnParticipantProperty();

    public GameObject PositionalGameObject;

    public bool isSpeaking { get; private set; }
    public Channel3DProperties ChannelProperties { get; private set; }
    private IChannelSession ChannelSession;
    private string positionalChannelName;
    private IParticipant _participant;
    public IParticipant Participant
    {
        get
        {
            return _participant;
        }
        set
        {
            if (value != null)
            {
                _participant = value;
                SetupParticipantHandlers();
            }
        }
    }

    private void SetupParticipantHandlers()
    {
        PositionalGameObject = PositionalGameObject != null ? PositionalGameObject : gameObject;
        ChannelSession = Participant.ParentChannelSession;
        Participant.PropertyChanged -= Participant_PropertyChanged;

        Participant.PropertyChanged += Participant_PropertyChanged;
    }

    private void Participant_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        IParticipant participant = (IParticipant)sender;
        m_ParticipantPropertyEvent.Invoke(participant, e);
        switch (e.PropertyName)
        {
            case "SpeechDetected":
                isSpeaking = participant.SpeechDetected;
                break;
            default:
                break;
        }
    }

    public void Update3dPosition(Transform listener, Transform speaker)
    {
        if (listener == null || speaker == null)
        {
            VxClient.Instance.vivoxDebug.DebugMessage("Cannot set 3D position: Either speaker or listener is null", vx_log_level.log_error);
            return;
        }
        if (ChannelSession != null && ChannelSession.AudioState == ConnectionState.Connected)
        {
            ChannelSession.Set3DPosition(speaker.position, listener.position, listener.forward, listener.up);
        }
        else
        {
            VxClient.Instance.vivoxDebug.DebugMessage("Cannot set 3D position: Either speaker or listener is null", vx_log_level.log_info);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (PositionalGameObject && PositionalGameObject.transform && ChannelSession != null && ChannelSession.AudioState == ConnectionState.Connected)
        {
            Update3dPosition(PositionalGameObject.transform, PositionalGameObject.transform);
        }
    }
    void OnDestroy()
    {
        m_ParticipantPropertyEvent.RemoveAllListeners();
    }
}
