﻿using UnityEngine;
using System.Collections;

public class SceneLevel : MonoBehaviour {
    [HideInInspector]
    public Scene level = null;
    [HideInInspector]
    public float waitBeforeStartLevel = 0f;
    [HideInInspector]
    public MenuMapLevel3D menuMap;
    [HideInInspector]
    public LevelInfo infoLevel;

    public void StartLevel() {
        if (level == null || level.name == "Not" || level.name == "") return;
        // Get the navigator
        MenuNavigator _menuNavigator = GameObject.FindGameObjectWithTag(Tags.Menus)
                                 .GetComponent<MenuNavigator>();

        menuMap.NotifyListenersSelectedLevel(infoLevel);
        // Wait before starting the change
        _menuNavigator.ChangeScene(level.name, waitBeforeStartLevel);
    }
}
