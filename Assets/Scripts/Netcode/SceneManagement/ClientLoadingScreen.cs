using System.Collections;
using UnityEngine;

namespace Netcode.SceneManagement
{
    /// <summary>
    /// This script handles the use of a loading screen with a progress bar and the name of the loaded scene shown. It
    /// must be started and stopped from outside this script. It also allows updating the loading screen when a new
    /// loading operation starts before the loading screen is stopped.
    /// </summary>
    public class ClientLoadingScreen : MonoBehaviour
    {

        [SerializeField]
        CanvasGroup m_CanvasGroup;

        [SerializeField]
        float m_DelayBeforeFadeOut = 0.5f;

        [SerializeField]
        float m_FadeOutDuration = 0.1f;

        bool m_LoadingScreenRunning;

        Coroutine m_FadeOutCoroutine;

        void Awake()
        {
            DontDestroyOnLoad(this);
        }

        void Start()
        {
            SetCanvasVisibility(false);
        }

        public void StopLoadingScreen()
        {
            if (m_LoadingScreenRunning)
            {
                if (m_FadeOutCoroutine != null)
                {
                    StopCoroutine(m_FadeOutCoroutine);
                }
                m_FadeOutCoroutine = StartCoroutine(FadeOutCoroutine());
            }
        }

        public void StartLoadingScreen(string sceneName)
        {
            SetCanvasVisibility(true);
            m_LoadingScreenRunning = true;
        }
        

        void SetCanvasVisibility(bool visible)
        {
            m_CanvasGroup.alpha = visible ? 1 : 0;
            m_CanvasGroup.blocksRaycasts = visible;
        }

        IEnumerator FadeOutCoroutine()
        {
            yield return new WaitForSeconds(m_DelayBeforeFadeOut);
            m_LoadingScreenRunning = false;

            float currentTime = 0;
            while (currentTime < m_FadeOutDuration)
            {
                m_CanvasGroup.alpha = Mathf.Lerp(1, 0, currentTime / m_FadeOutDuration);
                yield return null;
                currentTime += Time.deltaTime;
            }

            SetCanvasVisibility(false);
        }
    }
}
