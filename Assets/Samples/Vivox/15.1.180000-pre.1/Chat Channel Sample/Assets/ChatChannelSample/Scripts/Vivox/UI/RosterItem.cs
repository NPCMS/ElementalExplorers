using UnityEngine;
using UnityEngine.UI;
using VivoxUnity;

public class RosterItem : MonoBehaviour
{
    private VivoxVoiceManager _vivoxVoiceManager;

    // Player specific items.
    public IParticipant Participant;
    public Text PlayerNameText;

    public Image ChatStateImage;
    public Sprite MutedImage;
    public Sprite SpeakingImage;
    public Sprite NotSpeakingImage;

    private bool isMuted;
    public bool IsMuted
    {
        get { return isMuted; }
        private set
        {
            if (Participant.IsSelf)
            {
                // Muting/unmuting the local input device.
                _vivoxVoiceManager.AudioInputDevices.Muted = value;
            }
            else
            {
                // Check if a participant is in audio other wise you cant hear them anyways
                if (Participant.InAudio)
                {
                    Participant.LocalMute = value;
                }
            }
            isMuted = value;
            UpdateChatStateImage();
        }                           
    }

    private bool isSpeaking;
    public bool IsSpeaking
    {
        get { return isSpeaking; }
        private set
        {
            if  (ChatStateImage && !IsMuted)
            {
                isSpeaking = value;
                UpdateChatStateImage();
            }
        }
    }

    private void UpdateChatStateImage()
    {
        if (IsMuted)
        {
            ChatStateImage.sprite = MutedImage;
            ChatStateImage.gameObject.transform.localScale = Vector3.one;
        }
        else
        {
            if (isSpeaking)
            {
                ChatStateImage.sprite = SpeakingImage;
                ChatStateImage.gameObject.transform.localScale = Vector3.one;
            }
            else
            {
                ChatStateImage.sprite = NotSpeakingImage;
                //ChatStateImage.gameObject.transform.localScale = Vector3.one * 0.85f;
            }
        }
    }

    public void SetupRosterItem(IParticipant participant)
    {
        _vivoxVoiceManager = VivoxVoiceManager.Instance;
        Participant = participant;

        PlayerNameText.text = Participant.Account.DisplayName;
        IsMuted = participant.IsSelf ? _vivoxVoiceManager.AudioInputDevices.Muted : Participant.LocalMute;
        IsSpeaking = participant.SpeechDetected;
        gameObject.GetComponent<Button>().onClick.AddListener(() =>
        {
            IsMuted = !IsMuted;
        });
        Participant.PropertyChanged += (obj, args) =>
        {
            switch (args.PropertyName)
            {
                case "SpeechDetected":
                    IsSpeaking = Participant.SpeechDetected;
                    break;
            }
        };
    }
}