﻿using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class OptionsInput : MonoBehaviour, SubOptionInterface {
    /// <summary>
    /// Title of the panel
    /// </summary>
    public Text title;
    /// <summary>
    /// The object to be selected
    /// </summary>
    public GameObject firstSelected;
    /// <summary>
    /// Key of string for gamepad
    /// </summary>
    public string keyGamepad;
    /// <summary>
    /// Key of string for keyboard
    /// </summary>
    public string keyKeyboard;
    /// <summary>
    /// Button that changes between gamepad and keyboard
    /// </summary>
    public Button choice;
    /// <summary>
    /// Canvas of gamepad
    /// </summary>
    public CanvasGroup gamepad;
    /// <summary>
    /// Cavnas of keyboard
    /// </summary>
    public CanvasGroup keyboard;
    /// <summary>
    /// Is the gamepad visible, or the other?
    /// </summary>
    private bool gamepadVisible=false;
    /// <summary>
    /// Text internationalization of the button to change it
    /// </summary>
    private TextInternationalization titleButton;

    public void Awake() {
        //get the international text to change key
        titleButton = choice.GetComponentInChildren<TextInternationalization>();
        toggleInput();
    }

    /// <summary>
    /// Get the panel of this option (itself)
    /// </summary>
    /// <returns></returns>
    public GameObject GetPanel() {
        return gameObject;
    }

    /// <summary>
    /// Get the focus to the panel
    /// </summary>
    public bool GetFocus() {
        //the first time don't play effect
        OnSelectInvokeAudio audio = choice.gameObject.GetComponent<OnSelectInvokeAudio>();
        if (audio != null)
            audio.passPlayAudio = true;

        //select the option
        EventSystem.current.SetSelectedGameObject(firstSelected);
        return true;
    }

    public void LoseFocus() {
        if (title != null) {
            if (EventSystem.current.currentSelectedGameObject != null)
                EventSystem.current.SetSelectedGameObject(null);
        }
    }

    /// <summary>
    /// Toggle the input vision
    /// </summary>
    public void toggleInput() {
        if (gamepadVisible) {
            gamepad.alpha = 0;
            keyboard.alpha = 1;
            gamepadVisible = false;
            titleButton.keyText = keyGamepad;

        } else {
            gamepad.alpha = 1;
            keyboard.alpha = 0;
            gamepadVisible = true;
            titleButton.keyText = keyKeyboard;
        }

        titleButton.OnChangeLanguage(LanguageManager.Instance);
    }
}
