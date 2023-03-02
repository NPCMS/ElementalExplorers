using System;
using System.Collections;
using Netcode.ConnectionManagement;
using Netcode.Infrastructure;
using Netcode.Infrastructure.PubSub;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;

namespace Netcode.Utils
{

    /// <summary>
    /// An entry point to the application, where we bind all the common dependencies to the root DI scope.
    /// </summary>
    public class ApplicationController : LifetimeScope
    {
        [SerializeField] ConnectionManager m_ConnectionManager;
        [SerializeField] NetworkManager m_NetworkManager;
        IDisposable m_Subscriptions;

        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);
            builder.RegisterComponent(m_ConnectionManager);
            builder.RegisterComponent(m_NetworkManager);

            //the following singletons represent the local representations of the lobby that we're in and the user that we are
            //they can persist longer than the lifetime of the UI in MainMenu where we set up the lobby that we create or join


            //these message channels are essential and persist for the lifetime of the lobby and relay services
            // Registering as instance to prevent code stripping on iOS
            builder.RegisterInstance(new MessageChannel<UnityServiceErrorMessage>()).AsImplementedInterfaces();
            builder.RegisterInstance(new MessageChannel<ConnectStatus>()).AsImplementedInterfaces();
            
            builder.Register<ProfileManager>(Lifetime.Singleton);


            //these message channels are essential and persist for the lifetime of the lobby and relay services
            //they are networked so that the clients can subscribe to those messages that are published by the server
            builder.RegisterComponent(new NetworkedMessageChannel<ConnectionEventMessage>()).AsImplementedInterfaces();

            //this message channel is essential and persists for the lifetime of the lobby and relay services
            builder.RegisterInstance(new MessageChannel<ReconnectMessage>()).AsImplementedInterfaces();

            //all the lobby service stuff, bound here so that it persists through scene loads
            builder.Register<AuthenticationServiceFacade>(Lifetime.Singleton); //a manager entity that allows us to do anonymous authentication with unity services

        }

        private void Start()
        {
            var subHandles = new DisposableGroup();
            m_Subscriptions = subHandles;

            Application.wantsToQuit += OnWantToQuit;
            DontDestroyOnLoad(gameObject);
            Application.targetFrameRate = 120;
            SceneManager.LoadScene("MenuScene");
        }

        protected override void OnDestroy()
        {
            m_Subscriptions?.Dispose();
            base.OnDestroy();
        }

        /// <summary>
        ///     In builds, if we are in a lobby and try to send a Leave request on application quit, it won't go through if we're quitting on the same frame.
        ///     So, we need to delay just briefly to let the request happen (though we don't need to wait for the result).
        /// </summary>
        private IEnumerator LeaveBeforeQuit()
        {
            yield return null;
            Application.Quit();
        }

        private bool OnWantToQuit()
        {
            StartCoroutine(LeaveBeforeQuit());
            return true;
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
