﻿using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HighlightedOnSelectSubOption : MonoBehaviour, ISelectHandler, IDeselectHandler {
    public Animator asociatedTitle;

    public void OnDeselect(BaseEventData eventData) {
        asociatedTitle.SetBool("Selected", false);
        asociatedTitle.SetBool("Disabled", false);
        asociatedTitle.SetBool("Normal", true);
    }

    public void OnSelect(BaseEventData eventData) {
        asociatedTitle.SetBool("Disabled", false);
        asociatedTitle.SetBool("Normal", false);
        asociatedTitle.SetBool("Selected", true);
    }

    public void Disable() {
        asociatedTitle.SetBool("Selected", false);
        asociatedTitle.SetBool("Normal", false);
        asociatedTitle.SetBool("Disabled", true);
    }
}
