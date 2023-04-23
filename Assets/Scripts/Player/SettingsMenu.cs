using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SettingsMenu : MonoBehaviour
{
    [SerializeField] private UIInteraction vignetteBtn;
    [SerializeField] private UIInteraction voiceChatBtn;
    [SerializeField] private GameObject settingsPage;

    private readonly List<Action<float>> vignetteCallbacks = new();
    private readonly List<Action<bool>> voiceChatCallbacks = new();

    private readonly float[] vignetteSettings = {0, 1, 1.5f, 2};
    private readonly string[] vignetteNames = {"Off", "Low", "Medium", "High"};
    private int vignetteSettingsIndex;
    private bool voiceChatActive = true;

    private bool settingsActive;
    

    public static SettingsMenu instance;
    private SteamInputCore.SteamInput steamInput;
    
    void Start()
    {
        DontDestroyOnLoad(gameObject);
        instance = this;

        steamInput = SteamInputCore.GetInput();

        vignetteBtn.AddCallback(ToggleVignetteState);
        voiceChatBtn.AddCallback(ToggleVoiceChatMute);
    }

    private void Update()
    {
        if (steamInput.GetInputDown(SteamInputCore.Hand.Left, SteamInputCore.Button.A) |
            steamInput.GetInputDown(SteamInputCore.Hand.Right, SteamInputCore.Button.A))
        {
            if (settingsActive)
            {
                settingsPage.SetActive(false);
            }
            else
            {
                AppearSettings();
            }
            settingsActive = !settingsActive;
        }
    }

    private void AppearSettings()
    {
        Transform camera = GameObject.FindGameObjectWithTag("MainCamera").transform;
        Vector3 cameraForward = camera.forward;
        cameraForward.y = 0;
        cameraForward.Normalize();

        transform.position = camera.position + cameraForward * 2.5f;
        transform.rotation = Quaternion.LookRotation(cameraForward);
        
        settingsPage.SetActive(true);
    }

    private void ToggleVignetteState(RaycastHit hit, SteamInputCore.Button button)
    {
        vignetteSettingsIndex = (vignetteSettingsIndex + 1) % vignetteSettings.Length;
        vignetteBtn.gameObject.GetComponentInChildren<TMP_Text>().text = "VIGNETTE\n" + vignetteNames[vignetteSettingsIndex];
        foreach (var callback in vignetteCallbacks)
        {
            callback(vignetteSettings[vignetteSettingsIndex]);
        }
    }

    private void ToggleVoiceChatMute(RaycastHit hit, SteamInputCore.Button button)
    {
        voiceChatActive = !voiceChatActive;
        voiceChatBtn.gameObject.GetComponentInChildren<TMP_Text>().text = "VOICE CHAT\n" + (voiceChatActive ? "UNMUTED" : "MUTED");
        foreach (var callback in voiceChatCallbacks)
        {
            callback(voiceChatActive);
        }
    }

    public void AddVignetteCallback(Action<float> a)
    {
        vignetteCallbacks.Add(a);
    }
    
    public void RemoveVignetteCallback(Action<float> a)
    {
        vignetteCallbacks.Remove(a);
    }
    
    public void AddVoiceChatCallback(Action<bool> a)
    {
        voiceChatCallbacks.Add(a);
    }
    
    public void RemoveVoiceChatCallback(Action<bool> a)
    {
        voiceChatCallbacks.Remove(a);
    }
}
