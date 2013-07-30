// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiMenuActivate.cs
// COMMENT - one line to give a brief idea of what this file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.Common.Unity;

/// <summary>
/// COMMENT 
/// </summary>
public class GuiMenuActivate : AMonoBehaviourBase {

    UIButton[] childButtons;

    void Start() {
        childButtons = gameObject.GetSafeMonoBehaviourComponentsInChildren<UIButton>(includeInactive: true);
        foreach (UIButton button in childButtons) {
            UIEventListener.Get(button.gameObject).onClick += OnChildButtonClick;
        }
    }

    private void OnChildButtonClick(GameObject go) {
        foreach (UIButton b in childButtons) {
            b.collider.enabled = false;

        }
    }

    private void OnPanelGone() {
        foreach (UIButton b in childButtons) {
            //NGUITools.SetActive(b.gameObject, true);
            b.collider.enabled = true;
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

