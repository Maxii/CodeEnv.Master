// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiVisibilityButton.cs
// Allows visibiliity control of all Gui elements in the scene when this button is clicked.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using UnityEngine;

/// <summary>
/// Allows visibiliity control of all Gui elements in the scene
/// when this button is clicked.
/// </summary>
public class GuiVisibilityButton : GuiButtonBase {

    public GuiVisibilityCommand guiVisibilityCmd;
    public UIPanel[] guiVisibilityExceptions;    // Inspector automatically initializes array size

    protected override void OnButtonClick(GameObject sender) {
        switch (guiVisibilityCmd) {
            case GuiVisibilityCommand.RestoreUIPanelsVisibility:
            case GuiVisibilityCommand.MakeVisibleUIPanelsInvisible:
                //Debug.Log("GuiVisibilty value = {0}.".Inject(guiVisibilityCmd));
                eventMgr.Raise<GuiVisibilityChangeEvent>(new GuiVisibilityChangeEvent(guiVisibilityCmd, guiVisibilityExceptions));
                break;
            case GuiVisibilityCommand.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(guiVisibilityCmd));
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

