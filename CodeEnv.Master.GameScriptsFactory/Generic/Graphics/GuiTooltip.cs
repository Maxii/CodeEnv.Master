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

#define DEBUG_LEVEL_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Standalone and extensible class for all Gui scripts containing Tooltip infrastructure. Can be
/// instantiated for just Tooltip functionality but requires a Collider.
/// </summary>
[RequireComponent(typeof(Collider))]
public class GuiTooltip : MonoBehaviourBase {

    public string tooltip = string.Empty;

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

