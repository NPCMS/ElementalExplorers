#if AUTH_PACKAGE_PRESENT
using System;
using System.ComponentModel;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Vivox;
using UnityEngine;
using VivoxUnity;

public class PackageInitTester : MonoBehaviour
{

#region Enums

    /// <summary>
    /// Defines properties that can change.  Used by the functions that subscribe to the OnAfterTYPEValueUpdated functions.
    /// </summary>
    public enum ChangedProperty
    {
        None,
        Speaking,
        Typing,
        Muted
    }

    public enum ChatCapability
    {
        TextOnly,
        AudioOnly,
        TextAndAudio
    };

#endregion

#region Delegates/Events

    public delegate void ParticipantValueChangedHandler(string username, Channel channel, bool value);
    public event ParticipantValueChangedHandler OnSpeechDetectedEvent;
    public delegate void ParticipantValueUpdatedHandler(string username, Channel channel, double value);
    public event ParticipantValueUpdatedHandler OnAudioEnergyChangedEvent;


    public delegate void ParticipantStatusChangedHandler(string username, Channel channel, IParticipant participant);
    public event ParticipantStatusChangedHandler OnParticipantAddedEvent;
    public event ParticipantStatusChangedHandler OnParticipantRemovedEvent;

    public delegate void ChannelTextMessageChangedHandler(string sender, IChannelTextMessage channelTextMessage);
    public event ChannelTextMessageChangedHandler OnTextMessageLogReceivedEvent;

    public delegate void LoginStatusChangedHandler();
    public event LoginStatusChangedHandler OnUserLoggedInEvent;
    public event LoginStatusChangedHandler OnUserLoggedOutEvent;

#endregion

    private Uri _serverUri
    {
        get => new Uri(_server);

        set
        {
            _server = value.ToString();
        }
    }
    private string _server = "https://mtu1xp.www.vivox.com/api2";
    private string _domain = "mtu1xp.vivox.com";
    private string _tokenIssuer = "e80e2-lab_t-55236-test";
    private string _tokenKey = "wnr5YSTpvbtkuFUycMGjz6MH3G14rebJ";
    private TimeSpan _tokenExpiration = TimeSpan.FromSeconds(90);

    private Client VivoxClient => VivoxService.Instance.Client;
    // For testing our new integration - requires Unity.Services.Authentication package.
    private Account account;
    // OLD - Use this AccountId and comment out the above Account if you are testing our old integration out.
    //private AccountId account;
    public LoginState LoginState { get; private set; }
    public ILoginSession LoginSession;

    async void Start()
    {
        //InitializationOptions options = new InitializationOptions()
        //    .SetVivoxCredentials(_server, _domain, _tokenIssuer, _tokenKey);
        //await UnityServices.InitializeAsync(options);
        await UnityServices.InitializeAsync(); //will initialize all services that subscribed to Core.
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        VivoxService.Instance.Initialize();

        Debug.Log(UnityServices.State);
        if (VivoxClient != null)
        {
            if (!VivoxClient.Initialized)
            {
                Debug.LogError("The Client isn't initialized yet. Please initialize the Client before trying to login.");
                return;
            }
            Login("BruceWillis");
        }
        else
        {
            Debug.LogError("The Client is null. Vivox SDK operations cannot be executed without an initialized Client object.");
        }
    }

    public void Login(string displayName = null)
    {
        Debug.Log("Is vivox initialized: " + VivoxClient.Initialized);
        // For testing our new integration - requires Unity.Services.Authentication package.
        account = new Account(displayName);
        // OLD - Use the two lines below and comment out the above Account if you are testing our old integration.
        //string uniqueId = Guid.NewGuid().ToString();
        //account = new AccountId(_tokenIssuer, uniqueId, _domain);
        LoginSession = VivoxClient.GetLoginSession(account);
        LoginSession.PropertyChanged += OnLoginSessionPropertyChanged;
        LoginSession.BeginLogin(LoginSession.GetLoginToken(), SubscriptionMode.Accept, null, null, null, ar =>
        {
            try
            {
                LoginSession.EndLogin(ar);
                JoinChannel("Testboi", ChannelType.Echo, ChatCapability.AudioOnly);
                // Uncomment the below line and comment out the above line to test a positional channel.
                //JoinChannel("Testboi", ChannelType.Positional, ChatCapability.AudioOnly, properties: new Channel3DProperties());
            }
            catch (Exception e)
            {
                // Handle error 
                Debug.LogError(nameof(e));
                // Unbind if we failed to login.
                LoginSession.PropertyChanged -= OnLoginSessionPropertyChanged;
                return;
            }
        });
    }


    public void JoinChannel(string channelName, ChannelType channelType, ChatCapability chatCapability,
        bool switchTransmission = true, Channel3DProperties properties = null)
    {
        if (LoginState == LoginState.LoggedIn)
        {
            // For testing our new integration - requires Unity.Services.Authentication package.
            Channel channel = new Channel(channelName, channelType, properties);
            // OLD - Use this ChannelId and comment out the above Channel if you are testing our old integration out.
            //ChannelId channel = new ChannelId(_tokenIssuer, channelName, _domain, channelType, properties);
            IChannelSession channelSession = LoginSession.GetChannelSession(channel);
            channelSession.PropertyChanged += OnChannelPropertyChanged;
            channelSession.Participants.AfterKeyAdded += OnParticipantAdded;
            channelSession.Participants.BeforeKeyRemoved += OnParticipantRemoved;
            channelSession.Participants.AfterValueUpdated += OnParticipantValueUpdated;
            channelSession.BeginConnect(chatCapability != ChatCapability.TextOnly, chatCapability != ChatCapability.AudioOnly, switchTransmission, channelSession.GetConnectToken(), ar =>
            {
                try
                {
                    channelSession.EndConnect(ar);
                }
                catch (Exception e)
                {
                    // Handle error 
                    Debug.LogError($"Could not connect to voice channel: {e.Message}");
                    return;
                }
            });
        }
        else
        {
            Debug.LogError("Cannot join a channel when not logged in.");
        }
    }

    private void OnLoginSessionPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
    {
        if (propertyChangedEventArgs.PropertyName != "State")
        {
            return;
        }
        var loginSession = (ILoginSession)sender;
        LoginState = loginSession.State;
        Debug.Log("Detecting login session change");
        switch (LoginState)
        {
            case LoginState.LoggingIn:
                {
                    Debug.Log("Logging in");
                    break;
                }
            case LoginState.LoggedIn:
                {
                    Debug.Log("Connected to voice server and logged in.");
                    //OnUserLoggedInEvent?.Invoke();
                    break;
                }
            case LoginState.LoggingOut:
                {
                    Debug.Log("Logging out");
                    break;
                }
            case LoginState.LoggedOut:
                {
                    Debug.Log("Logged out");
                    LoginSession.PropertyChanged -= OnLoginSessionPropertyChanged;
                    break;
                }
            default:
                break;
        }
    }

    private void OnParticipantAdded(object sender, KeyEventArg<string> keyEventArg)
    {
        ValidateArgs(new object[] { sender, keyEventArg });

        // INFO: sender is the dictionary that changed and trigger the event.  Need to cast it back to access it.
        var source = (VivoxUnity.IReadOnlyDictionary<string, IParticipant>)sender;
        // Look up the participant via the key.
        var participant = source[keyEventArg.Key];
        var username = participant.Account.Name;
        var channel = participant.ParentChannelSession.Key as Channel;
        var channelSession = participant.ParentChannelSession;

        // Trigger callback
        OnParticipantAddedEvent?.Invoke(username, channel, participant);
    }

    private void OnParticipantRemoved(object sender, KeyEventArg<string> keyEventArg)
    {
        ValidateArgs(new object[] { sender, keyEventArg });

        // INFO: sender is the dictionary that changed and trigger the event.  Need to cast it back to access it.
        var source = (VivoxUnity.IReadOnlyDictionary<string, IParticipant>)sender;
        // Look up the participant via the key.
        var participant = source[keyEventArg.Key];
        var username = participant.Account.Name;
        var channel = participant.ParentChannelSession.Key as Channel;
        var channelSession = participant.ParentChannelSession;

        if (participant.IsSelf)
        {
            Debug.Log($"Unsubscribing from: {channelSession.Key.Name}");
            // Now that we are disconnected, unsubscribe.
            channelSession.PropertyChanged -= OnChannelPropertyChanged;
            channelSession.Participants.AfterKeyAdded -= OnParticipantAdded;
            channelSession.Participants.BeforeKeyRemoved -= OnParticipantRemoved;
            channelSession.Participants.AfterValueUpdated -= OnParticipantValueUpdated;

            // Remove session.
            var user = VivoxClient.GetLoginSession(account);
            user.DeleteChannelSession(channelSession.Channel);
        }

        // Trigger callback
        OnParticipantRemovedEvent?.Invoke(username, channel, participant);
    }

    private static void ValidateArgs(object[] objs)
    {
        foreach (var obj in objs)
        {
            if (obj == null)
                throw new ArgumentNullException(obj.GetType().ToString(), "Specify a non-null/non-empty argument.");
        }
    }

    private void OnParticipantValueUpdated(object sender, ValueEventArg<string, IParticipant> valueEventArg)
    {
        ValidateArgs(new object[] { sender, valueEventArg });

        var source = (VivoxUnity.IReadOnlyDictionary<string, IParticipant>)sender;
        // Look up the participant via the key.
        var participant = source[valueEventArg.Key];

        string username = valueEventArg.Value.Account.Name;
        Channel channel = valueEventArg.Value.ParentChannelSession.Key as Channel;
        string property = valueEventArg.PropertyName;

        switch (property)
        {
            case "SpeechDetected":
                {
                    Debug.Log($"OnSpeechDetectedEvent: {username} in {channel}.");
                    OnSpeechDetectedEvent?.Invoke(username, channel, valueEventArg.Value.SpeechDetected);
                    break;
                }
            case "AudioEnergy":
                {
                    OnAudioEnergyChangedEvent?.Invoke(username, channel, valueEventArg.Value.AudioEnergy);
                    break;
                }
            default:
                break;
        }
    }

    private void OnChannelPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
    {
        ValidateArgs(new object[] { sender, propertyChangedEventArgs });

        //if (_client == null)
        //    throw new InvalidClient("Invalid client.");
        var channelSession = (IChannelSession)sender;

        // IF the channel has removed audio, make sure all the VAD indicators aren't showing speaking.
        if (propertyChangedEventArgs.PropertyName == "AudioState" && channelSession.AudioState == ConnectionState.Disconnected)
        {
            Debug.Log($"Audio disconnected from: {channelSession.Key.Name}");

            foreach (var participant in channelSession.Participants)
            {
                OnSpeechDetectedEvent?.Invoke(participant.Account.Name, channelSession.Channel as Channel, false);
            }
        }

        // IF the channel has fully disconnected, unsubscribe and remove.
        if ((propertyChangedEventArgs.PropertyName == "AudioState" || propertyChangedEventArgs.PropertyName == "TextState") &&
            channelSession.AudioState == ConnectionState.Disconnected &&
            channelSession.TextState == ConnectionState.Disconnected)
        {
            Debug.Log($"Unsubscribing from: {channelSession.Key.Name}");
            // Now that we are disconnected, unsubscribe.
            channelSession.PropertyChanged -= OnChannelPropertyChanged;
            channelSession.Participants.AfterKeyAdded -= OnParticipantAdded;
            channelSession.Participants.BeforeKeyRemoved -= OnParticipantRemoved;
            channelSession.Participants.AfterValueUpdated -= OnParticipantValueUpdated;

            // Remove session.
            var user = VivoxClient.GetLoginSession(account);
            user.DeleteChannelSession(channelSession.Channel);

        }
    }


    private void OnApplicationQuit()
    {
        // Needed to add this to prevent some unsuccessful uninit, we can revisit to do better -carlo
        Client.Cleanup();
        if (VivoxClient != null)
        {
            Debug.Log("Uninitializing client.");
            VivoxClient.Uninitialize();
        }
    }
}
#endif 