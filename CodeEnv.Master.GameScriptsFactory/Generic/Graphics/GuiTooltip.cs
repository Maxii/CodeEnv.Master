// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiTooltip.cs
// Standalone and extensible class for all Gui scripts containing Tooltip infrastructure.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Standalone and extensible class for all Gui scripts containing Tooltip infrastructure. Can be
/// instantiated for just Tooltip functionality but requires a Collider.
/// </summary>
public class GuiTooltip : AMonoBehaviourBase {

    public string tooltip = string.Empty;

    private bool _tooltipHasContent;

    protected override void Awake() {
        base.Awake();
        InitializeTooltip();
        if (_tooltipHasContent = Utility.CheckForContent(tooltip)) {
            UnityUtility.ValidateComponentPresence<Collider>(gameObject);
        }
    }

    protected virtual void InitializeTooltip() { }

    void OnTooltip(bool toShow) {
        if (_tooltipHasContent) {
            if (toShow) {
                UITooltip.ShowText(tooltip);
            }
            else {
                UITooltip.ShowText(null);
            }
        }
    }
}

