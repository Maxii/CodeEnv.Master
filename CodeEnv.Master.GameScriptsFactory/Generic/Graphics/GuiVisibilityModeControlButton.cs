// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiVisibilityModeControlButton.cs
// Changes the GuiVisibilityMode to that designated on LClk.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using UnityEngine;

/// <summary>
/// Changes the GuiVisibilityMode to that designated on LClk.
/// </summary>
public class GuiVisibilityModeControlButton : AGuiButtonBase {

    /// <summary>
    /// Gui element visibility mode to implement when this button is LClk.
    /// </summary>
    public GuiVisibilityMode visibilityModeOnClick;

    /// <summary>
    /// The list of UIPanels that should be excepted from the <c>visibilityModeOnClick</c>.
    /// </summary>
    public List<UIPanel> exceptions;

    protected override void OnLeftClick() {
        switch (visibilityModeOnClick) {
            case GuiVisibilityMode.Hidden:
                if (exceptions.Count == Constants.Zero || exceptions[0] == null) {
                    D.Warn("{0}.{1} has no Exceptions listed. \nAs a minimum, it should list the panel being shown.", gameObject.name, GetType().Name);
                    // NOTE: Even without the showing panel being listed, it will still be reactivated immediately after being deactivated
                    // if MyNguiPlayAnimation.ifDisabledOnPlay = EnableThenPlay. 
                }
                break;
            case GuiVisibilityMode.Visible:
                break;
            case GuiVisibilityMode.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(visibilityModeOnClick));
        }
        GuiManager.Instance.ChangeVisibilityOfUIElements(visibilityModeOnClick, exceptions);
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

