using System;
using Unity.BossRoom.UnityServices.Auth;
using Unity.BossRoom.Utils;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;

namespace Unity.BossRoom.Gameplay.GameState
{
    /// <summary>
    /// Game Logic that runs when sitting at the MainMenu. This is likely to be "nothing", as no game has been started. But it is
    /// nonetheless important to have a game state, as the GameStateBehaviour system requires that all scenes have states.
    /// </summary>
    /// <remarks> OnNetworkSpawn() won't ever run, because there is no network connection at the main menu screen.
    /// Fortunately we know you are a client, because all players are clients when sitting at the main menu screen.
    /// </remarks>
    public class ClientMainMenuState : GameStateBehaviour
    {
        public override GameState ActiveState { get { return GameState.MainMenu; } }

        [SerializeField] LobbyMenuUI m_LobbyMenuUI;

        [Inject] AuthenticationServiceFacade m_AuthServiceFacade;
        [Inject] ProfileManager m_ProfileManager;

        protected override void Awake()
        {
            base.Awake();

            //m_LobbyButton.interactable = false;
            //m_LobbyUIMediator.Hide();

            if (string.IsNullOrEmpty(Application.cloudProjectId))
            {
                OnSignInFailed();
                return;
            }

            TrySignIn();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);
            builder.RegisterComponent(m_LobbyMenuUI);
        }


        private async void TrySignIn()
        {
            try
            {
                var unityAuthenticationInitOptions = new InitializationOptions();
                var profile = m_ProfileManager.Profile;
                if (profile.Length > 0)
                {
                    unityAuthenticationInitOptions.SetProfile(profile);
                }

                await m_AuthServiceFacade.InitializeAndSignInAsync(unityAuthenticationInitOptions);
                OnAuthSignIn();
                m_ProfileManager.onProfileChanged += OnProfileChanged;
            }
            catch (Exception)
            {
                OnSignInFailed();
            }
        }

        private void OnAuthSignIn()
        {
            //m_LobbyButton.interactable = true;
            //m_SignInSpinner.SetActive(false);

            Debug.Log($"Signed in. Unity Player ID {AuthenticationService.Instance.PlayerId}");

            //m_LocalUser.ID = AuthenticationService.Instance.PlayerId;
            // The local LobbyUser object will be hooked into UI before the LocalLobby is populated during lobby join, so the LocalLobby must know about it already when that happens.
            //m_LocalLobby.AddUser(m_LocalUser);
        }

        private void OnSignInFailed()
        {
            //if (m_LobbyButton)
            //{
            //    m_LobbyButton.interactable = false;
            //}
            //if (m_SignInSpinner)
            //{
            //    m_SignInSpinner.SetActive(false);
            //}
        }

        protected override void OnDestroy()
        {
            m_ProfileManager.onProfileChanged -= OnProfileChanged;
            base.OnDestroy();
        }

        async void OnProfileChanged()
        {
            //m_LobbyButton.interactable = false;
            //m_SignInSpinner.SetActive(true);
            await m_AuthServiceFacade.SwitchProfileAndReSignInAsync(m_ProfileManager.Profile);

            //m_LobbyButton.interactable = true;
            //m_SignInSpinner.SetActive(false);

            Debug.Log($"Signed in. Unity Player ID {AuthenticationService.Instance.PlayerId}");

            // Updating LocalUser and LocalLobby
            //m_LocalLobby.RemoveUser(m_LocalUser);
            //m_LocalUser.ID = AuthenticationService.Instance.PlayerId;
            //m_LocalLobby.AddUser(m_LocalUser);
        }

        public void OnStartClicked()
        {
            //m_LobbyUIMediator.ToggleJoinLobbyUI();
            //m_LobbyUIMediator.Show();
        }
    }
}
