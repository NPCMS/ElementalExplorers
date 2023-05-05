using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class TutorialMenu : MonoBehaviour
{

    [SerializeField] private List<GameObject> tutorialScreens;
    [SerializeField] private List<GameObject> nextButtons;
    [SerializeField] private List<GameObject> prevButtons;
    void Awake()
    {
        for (int i = 0; i < nextButtons.Count-1; i++)
        {
            if (nextButtons[i].TryGetComponent<UIInteraction>(out UIInteraction nextInteraction))
            {
                nextInteraction.AddCallback((RaycastHit hit, SteamInputCore.Button button) =>
                {
                    tutorialScreens[i+1].SetActive(true);
                    tutorialScreens[i].SetActive(false);
                });
            }
            else
                Debug.LogError("no interaction script");
        }

        for (int i = 0; i < prevButtons.Count; i++)
        {
            if (prevButtons[i].TryGetComponent<UIInteraction>(out UIInteraction prevInteraction))
            {
                prevInteraction.AddCallback((RaycastHit hit, SteamInputCore.Button button) =>
                {
                    tutorialScreens[i-1].SetActive(true);
                    tutorialScreens[i].SetActive(false);
                });
            }
            else
                Debug.LogError("no interaction script");
        }

        if (nextButtons.Last().TryGetComponent<UIInteraction>(out UIInteraction interaction))
        {
            tutorialScreens.Last().SetActive(false);
        }
    }
}
