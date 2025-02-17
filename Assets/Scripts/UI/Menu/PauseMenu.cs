﻿using UnityEngine;

using UnityEngine.EventSystems;

/// <summary>
/// Control all about pause menu
/// </summary>
public class PauseMenu : MonoBehaviour {
	#region Public Attributes
	/// <summary>
	/// The first option to be selected
	/// </summary>
	public GameObject firstSelected;
	#endregion

	#region Private Attributes
	

	/// <summary>
	/// Control if I have to select a default option
	/// </summary>
	private bool _selectOption;
	#endregion

	#region Methods
	public void OnEnable() {
		//we have to select the option in update
		_selectOption = true;
        EventSystem.current.SetSelectedGameObject(null);
    }

	public void Update() {
		//if we have to select the option...
		if (_selectOption) {
			//only once!
			_selectOption = false;

            //the first time don't play effect
            OnSelectInvokeAudio audio = firstSelected.GetComponent<OnSelectInvokeAudio>();
            if (audio != null)
                audio.passPlayAudio = true;

            //select the option
            EventSystem.current.SetSelectedGameObject(firstSelected);
		}
	}
    
	#endregion
}
