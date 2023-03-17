using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;

public class MainMenuOptions : MonoBehaviour
{
    public Button SaveButton;
    public Button QuitButton;
    public Dropdown TTSVoiceDropdown;
    public GameObject ConfirmationMenu;
    public Button ConfirmYesButton;
    public Button ConfirmNoButton;

    private GameObject m_optionsMenuPanel => gameObject;
    EventSystem m_EventSystem;
    private VivoxVoiceManager _vivoxVoiceManager => VivoxVoiceManager.Instance;
    private int _selected_tts_voice_index;
    private string tts_voice_setting = "VivoxTTSVoice";

    private void ttsDropdownValueChangedHandler(Dropdown target)
    {
        Debug.Log("selected: " + target.value);
        _selected_tts_voice_index = target.value;
    }

    void Awake()
    {
        // Setup menu objects on awake
        m_optionsMenuPanel.SetActive(false);
        if (!_vivoxVoiceManager)
        {
            Debug.LogError("Unable to find VivoxVoiceManager object.");
        }
        // Fill the TTS dropdown with all possible options
        AddAllTTSVoices();
    }

    void Start()
    {
        // Fetch the current EventSystem
        m_EventSystem = EventSystem.current;
        // Bind all the ui actions
        TTSVoiceDropdown.onValueChanged.AddListener(delegate {
            ttsDropdownValueChangedHandler(TTSVoiceDropdown);
        });
        SaveButton.onClick.AddListener(SaveAction);
        QuitButton.onClick.AddListener(QuitButtonAction);
        ConfirmYesButton.onClick.AddListener(ConfirmYesButtonAction);
        ConfirmNoButton.onClick.AddListener(ConfirmNoAction);
    }

    void OnDestroy()
    {
        // Unbind all the ui actions
        TTSVoiceDropdown.onValueChanged.RemoveAllListeners();
        SaveButton.onClick.RemoveListener(SaveAction);
        QuitButton.onClick.RemoveListener(QuitButtonAction);
        ConfirmYesButton.onClick.RemoveListener(ConfirmYesButtonAction);
        ConfirmNoButton.onClick.RemoveListener(ConfirmNoAction);
    }


    // Detects if the keyboard key or console button was pressed
    void Update()
    {
        // While on standalone or editor the escape key will open and close the menu
#if UNITY_STANDALONE || UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            MenuInputAction();
        }
#endif
        // While on consoles the start button will open and close the menu
#if UNITY_XBOXONE || UNITY_PS4 || UNITY_SWITCH
        if (Input.GetButtonDown("Options"))
        {
            MenuInputAction();
        }
#endif
    }

    private bool isDirty {
        get
        {
            return TTSVoiceDropdown.options[_selected_tts_voice_index].text != PlayerPrefs.GetString(tts_voice_setting, _vivoxVoiceManager.LoginSession.TTS.CurrentVoice.Name);
        }
    }

    private void AddAllTTSVoices()
    {
        //Get all TTS voices add them to the dropdown
        var all_voices = _vivoxVoiceManager.LoginSession.TTS.AvailableVoices;

        // Clear/remove all option item
        TTSVoiceDropdown.options.Clear();
        TTSVoiceDropdown.options.AddRange(all_voices.Select(
            // Fill the dropdown menu options with all voices
            v => new Dropdown.OptionData() { text = v.Name })
            );
        SelectOptionFromSavedSettings();
    }

    private void SelectOptionFromSavedSettings()
    {
        var voiceToSelect = TTSVoiceDropdown.options.FindIndex((i) => { return i.text.Equals(PlayerPrefs.GetString(tts_voice_setting, _vivoxVoiceManager.LoginSession.TTS.CurrentVoice.Name)); });
        TTSVoiceDropdown.value = voiceToSelect;
        _selected_tts_voice_index = voiceToSelect;
    }

    public void ShowOptionsMenu(bool showMenu = false)
    {
        if (showMenu == false && isDirty)
        {
            ShowConfirmMenu(true);
        }
        else
        {
            ShowConfirmMenu(false);
            m_optionsMenuPanel.SetActive(showMenu);
        }
    }

    private void ShowConfirmMenu(bool showMenu = false)
    {
        ConfirmationMenu.SetActive(showMenu);
    }

    // When Menu Input has fired
    private void MenuInputAction()
    {
        if (m_optionsMenuPanel.activeInHierarchy)
        {
            ShowOptionsMenu(false);
        }
        else
        {
            ShowOptionsMenu(true);
        }
    }

    // Resume button on the InGameMenu
    private void SaveAction()
    {
        PlayerPrefs.SetString(tts_voice_setting, TTSVoiceDropdown.options[_selected_tts_voice_index].text);
        // Set tts voice
        _vivoxVoiceManager.LoginSession.TTS.CurrentVoice = _vivoxVoiceManager.LoginSession.TTS.AvailableVoices.FirstOrDefault(v => v.Name == TTSVoiceDropdown.options[_selected_tts_voice_index].text);
        ShowOptionsMenu(false);

        // Remove focus on ui elements 
        m_EventSystem.SetSelectedGameObject(null);
    }

    // Quit button on the InGameMenu
    private void QuitButtonAction()
    {
        ShowOptionsMenu(false);
    }

    private void ConfirmYesButtonAction()
    {
        //Save settings and close
        SaveAction();
        ShowConfirmMenu(false);
        ShowOptionsMenu(false);
    }

    private void ConfirmNoAction()
    {
        ShowConfirmMenu(false);
        ShowConfirmMenu(false);
        m_optionsMenuPanel.SetActive(false);
        SelectOptionFromSavedSettings();
    }
}