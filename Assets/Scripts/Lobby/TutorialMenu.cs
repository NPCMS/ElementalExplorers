using UnityEngine;

public class TutorialMenu : MonoBehaviour
{
    [Header("TutorialScreens")]
    [SerializeField] private GameObject grappleFrame;
    [SerializeField] private GameObject aFrame;
    [SerializeField] private GameObject joystickFrame;
    [SerializeField] private GameObject bFrame;

    [Header("Next buttons")]
    [SerializeField] private UIInteraction grappleFrameNext;
    [SerializeField] private UIInteraction aFrameNext;
    [SerializeField] private UIInteraction joystickFrameNext;
    [SerializeField] private UIInteraction bFrameNext;
    
    [Header("Prev buttons")]
    [SerializeField] private UIInteraction aFramePrev;
    [SerializeField] private UIInteraction joystickFramePrev;
    [SerializeField] private UIInteraction bFramePrev;

   
    void Awake()
    {
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
            bFrame.SetActive(false);
            grappleFrame.SetActive(true);
            gameObject.SetActive(false);
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
