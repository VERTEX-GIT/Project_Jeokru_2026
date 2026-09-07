using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class SettingsMenuController : MonoBehaviour
{
    private const string FullscreenPrefKey =
        "Settings.Fullscreen";

    private const string ResolutionWidthPrefKey =
        "Settings.ResolutionWidth";

    private const string ResolutionHeightPrefKey =
        "Settings.ResolutionHeight";

    private const string MasterVolumePrefKey =
        "Settings.MasterVolume";

    private const string MasterVolumeParameter =
        "MasterVolume";

    [Header("Panel")]
    [SerializeField]
    private CanvasGroup panelCanvasGroup;

    [Header("Display")]
    [SerializeField]
    private Toggle fullscreenToggle;

    [SerializeField]
    private TMP_Dropdown resolutionDropdown;

    [Header("Audio")]
    [SerializeField]
    private Slider masterVolumeSlider;

    [SerializeField]
    private AudioMixer audioMixer;

    private readonly List<Resolution>
        availableResolutions = new();

    private float savedMasterVolume = 1f;

    private void Awake()
    {
        BuildResolutionOptions();
        LoadSettings();
        HideImmediate();
    }

    private void OnEnable()
    {
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged
                .AddListener(
                    HandleMasterVolumeChanged);
        }
    }

    private void OnDisable()
    {
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged
                .RemoveListener(
                    HandleMasterVolumeChanged);
        }
    }

    public void Open()
    {
        LoadSettingsToUI();
        SetPanelVisible(true);
    }

    public void Apply()
    {
        ApplyDisplaySettings();
        SaveSettings();

        savedMasterVolume =
            masterVolumeSlider != null
                ? masterVolumeSlider.value
                : savedMasterVolume;

        SetPanelVisible(false);
    }

    public void Close()
    {
        RestoreSavedVolume();
        LoadSettingsToUI();
        SetPanelVisible(false);
    }

    private void BuildResolutionOptions()
    {
        if (resolutionDropdown == null)
        {
            return;
        }

        availableResolutions.Clear();

        List<string> options =
            new();

        Resolution[] resolutions =
            Screen.resolutions;

        HashSet<string> addedResolutions =
            new();

        foreach (Resolution resolution
                 in resolutions)
        {
            string key =
                $"{resolution.width}x{resolution.height}";

            if (!addedResolutions.Add(key))
            {
                continue;
            }

            availableResolutions.Add(
                resolution);

            options.Add(
                $"{resolution.width} × {resolution.height}");
        }

        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(options);
    }

    private void LoadSettings()
    {
        int fullscreenValue =
            PlayerPrefs.GetInt(
                FullscreenPrefKey,
                Screen.fullScreen ? 1 : 0);

        int width =
            PlayerPrefs.GetInt(
                ResolutionWidthPrefKey,
                Screen.currentResolution.width);

        int height =
            PlayerPrefs.GetInt(
                ResolutionHeightPrefKey,
                Screen.currentResolution.height);

        savedMasterVolume =
            PlayerPrefs.GetFloat(
                MasterVolumePrefKey,
                1f);

        Screen.SetResolution(
            width,
            height,
            fullscreenValue == 1);

        ApplyMasterVolume(
            savedMasterVolume);

        LoadSettingsToUI();
    }

    private void LoadSettingsToUI()
    {
        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn =
                PlayerPrefs.GetInt(
                    FullscreenPrefKey,
                    Screen.fullScreen ? 1 : 0)
                == 1;
        }

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.SetValueWithoutNotify(
                PlayerPrefs.GetFloat(
                    MasterVolumePrefKey,
                    savedMasterVolume));
        }

        SelectSavedResolution();
    }

    private void SelectSavedResolution()
    {
        if (resolutionDropdown == null ||
            availableResolutions.Count == 0)
        {
            return;
        }

        int savedWidth =
            PlayerPrefs.GetInt(
                ResolutionWidthPrefKey,
                Screen.width);

        int savedHeight =
            PlayerPrefs.GetInt(
                ResolutionHeightPrefKey,
                Screen.height);

        int selectedIndex = 0;

        for (int i = 0;
             i < availableResolutions.Count;
             i++)
        {
            Resolution resolution =
                availableResolutions[i];

            if (resolution.width == savedWidth &&
                resolution.height == savedHeight)
            {
                selectedIndex = i;
                break;
            }
        }

        resolutionDropdown
            .SetValueWithoutNotify(
                selectedIndex);

        resolutionDropdown
            .RefreshShownValue();
    }

    private void ApplyDisplaySettings()
    {
        if (resolutionDropdown == null ||
            availableResolutions.Count == 0)
        {
            return;
        }

        int index =
            Mathf.Clamp(
                resolutionDropdown.value,
                0,
                availableResolutions.Count - 1);

        Resolution resolution =
            availableResolutions[index];

        bool fullscreen =
            fullscreenToggle != null &&
            fullscreenToggle.isOn;

        Screen.SetResolution(
            resolution.width,
            resolution.height,
            fullscreen);
    }

    private void SaveSettings()
    {
        bool fullscreen =
            fullscreenToggle != null &&
            fullscreenToggle.isOn;

        PlayerPrefs.SetInt(
            FullscreenPrefKey,
            fullscreen ? 1 : 0);

        if (resolutionDropdown != null &&
            availableResolutions.Count > 0)
        {
            int index =
                Mathf.Clamp(
                    resolutionDropdown.value,
                    0,
                    availableResolutions.Count - 1);

            Resolution resolution =
                availableResolutions[index];

            PlayerPrefs.SetInt(
                ResolutionWidthPrefKey,
                resolution.width);

            PlayerPrefs.SetInt(
                ResolutionHeightPrefKey,
                resolution.height);
        }

        if (masterVolumeSlider != null)
        {
            PlayerPrefs.SetFloat(
                MasterVolumePrefKey,
                masterVolumeSlider.value);
        }

        PlayerPrefs.Save();
    }

    private void HandleMasterVolumeChanged(
        float value)
    {
        ApplyMasterVolume(value);
    }

    private void ApplyMasterVolume(
        float normalizedVolume)
    {
        if (audioMixer == null)
        {
            return;
        }

        float clampedVolume =
            Mathf.Clamp(
                normalizedVolume,
                0.0001f,
                1f);

        float decibels =
            Mathf.Log10(
                clampedVolume) * 20f;

        audioMixer.SetFloat(
            MasterVolumeParameter,
            decibels);
    }

    private void RestoreSavedVolume()
    {
        ApplyMasterVolume(
            savedMasterVolume);
    }

    private void SetPanelVisible(
        bool visible)
    {
        if (panelCanvasGroup == null)
        {
            return;
        }

        panelCanvasGroup.alpha =
            visible ? 1f : 0f;

        panelCanvasGroup.interactable =
            visible;

        panelCanvasGroup.blocksRaycasts =
            visible;
    }

    private void HideImmediate()
    {
        SetPanelVisible(false);
    }
}