using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FMODLanguageToggle : MonoBehaviour
{
    [SerializeField] private FMODLineProvider lineProvider;
    [SerializeField] private Button toggleButton;
    [SerializeField] private TextMeshProUGUI buttonText;

    private void Awake()
    {
        if (lineProvider == null)
        {
            lineProvider = FindFirstObjectByType<FMODLineProvider>();
        }

        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(ToggleVoiceLanguage);
        }
        else
        {
            Debug.LogWarning("⚠️ FMODLanguageToggle: Toggle Button is not assigned.");
        }

        UpdateButtonText();
    }

    private void OnEnable()
    {
        // Optional: react if voice language changes elsewhere
        if (lineProvider != null)
        {
            lineProvider.OnVoiceLanguageChanged += OnVoiceLanguageChanged;
        }
    }

    private void OnDisable()
    {
        if (lineProvider != null)
        {
            lineProvider.OnVoiceLanguageChanged -= OnVoiceLanguageChanged;
        }
    }

    private void ToggleVoiceLanguage()
    {
        if (lineProvider == null)
        {
            Debug.LogWarning("⚠️ FMODLanguageToggle: No FMODLineProvider found.");
            return;
        }

        lineProvider.ToggleVoiceLanguage();
        UpdateButtonText();
    }

    private void OnVoiceLanguageChanged(FMODLineProvider.VoiceLanguage newLanguage)
    {
        UpdateButtonText();
    }

    private void UpdateButtonText()
    {
        if (buttonText == null || lineProvider == null)
            return;

        buttonText.text =
            lineProvider.CurrentVoiceLanguage == FMODLineProvider.VoiceLanguage.EN
                ? "EN"
                : "ALT";
    }
}
