// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiLabelReadoutBase.cs
// Base class for Dynamic Gui Labels (used as readouts) built with NGUI.
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
/// Base class for Dynamic Gui Labels (used as readouts) built with NGUI.
/// </summary>
public abstract class GuiLabelReadoutBase : MonoBehaviourBase {

    public string tooltip = string.Empty;
    protected UILabel readoutLabel;

    void Start() {
        Initialize();
    }

    protected virtual void Initialize() {
        readoutLabel = gameObject.GetSafeMonoBehaviourComponent<UILabel>();
    }

    void OnTooltip(bool toShow) {
        if (Utility.CheckForContent(tooltip)) {
            if (toShow) {
                UITooltip.ShowText(tooltip);
            }
            else {
                UITooltip.ShowText(null);
            }
        }
    }
}

