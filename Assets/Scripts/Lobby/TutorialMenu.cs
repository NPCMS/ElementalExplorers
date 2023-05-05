using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class TutorialMenu : MonoBehaviour
{
    [SerializeField] private GameObject mainMenu;
    [Header("TutorialScreens")]
    [SerializeField] private GameObject startFrame;
    [SerializeField] private GameObject grappleFrame;
    [SerializeField] private GameObject aFrame;
    [SerializeField] private GameObject joystickFrame;
    [SerializeField] private GameObject bFrame;

    [Header("Next buttons")]
    [SerializeField] private UIInteraction startFrameNext;
    [SerializeField] private UIInteraction grappleFrameNext;
    [SerializeField] private UIInteraction aFrameNext;
    [SerializeField] private UIInteraction joystickFrameNext;
    [SerializeField] private UIInteraction bFrameNext;
    
    [Header("Prev buttons")]
    [SerializeField] private UIInteraction grappleFramePrev;
    [SerializeField] private UIInteraction aFramePrev;
    [SerializeField] private UIInteraction joystickFramePrev;
    [SerializeField] private UIInteraction bFramePrev;

   
    void Awake()
    {
        startFrameNext.AddCallback((RaycastHit hit, SteamInputCore.Button button) =>
        {
            grappleFrame.SetActive(true);
            startFrame.SetActive(false);
        });
        
        grappleFrameNext.AddCallback((RaycastHit hit, SteamInputCore.Button button) =>
        {
            aFrame.SetActive(true);
            grappleFrame.SetActive(false);
        });
        
        aFrameNext.AddCallback((RaycastHit hit, SteamInputCore.Button button) =>
        {
            joystickFrame.SetActive(true);
            aFrame.SetActive(false);
        });
        
        joystickFrameNext.AddCallback((RaycastHit hit, SteamInputCore.Button button) =>
        {
            bFrame.SetActive(true);
            joystickFrame.SetActive(false);
        });
        
        bFrameNext.AddCallback((RaycastHit hit, SteamInputCore.Button button) =>
        {
            mainMenu.SetActive(true);
            bFrame.SetActive(false);
        });
        
        grappleFramePrev.AddCallback((RaycastHit hit, SteamInputCore.Button button) =>
        {
            startFrame.SetActive(true);
            grappleFrame.SetActive(false);

        });
        
        aFramePrev.AddCallback((RaycastHit hit, SteamInputCore.Button button) =>
        {
            grappleFrame.SetActive(true);
            aFrame.SetActive(false);
        });
        
        joystickFramePrev.AddCallback((RaycastHit hit, SteamInputCore.Button button) =>
        {
            aFrame.SetActive(true);
            joystickFrame.SetActive(false);
        });
        
        bFramePrev.AddCallback((RaycastHit hit, SteamInputCore.Button button) =>
        {
            joystickFrame.SetActive(true);
            bFrame.SetActive(false);
        });
        
        
    }
}
