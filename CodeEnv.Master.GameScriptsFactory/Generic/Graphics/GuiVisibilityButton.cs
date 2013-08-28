// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiVisibilityButton.cs
// Button that issues commands that control the visibility of Gui elements that contain UIPanels.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using UnityEngine;

/// <summary>
/// Button that issues commands that control the visibility of Gui elements that contain UIPanels.
/// </summary>
public class GuiVisibilityButton : AGuiButtonBase {

    /// <summary>
    /// Event that delivers a GuiVisibilityCommand and the UIPanel exceptions that can accompany it. 
    /// WARNING: This event MUST remain in Scripts as it references the NGUI UIPanel class. 
    /// Placing it in Common.Unity crashes the Unity compiler without telling you why!!!!!
    /// </summary>
    public class GuiVisibilityChangeEvent : AGameEvent {

        public GuiVisibilityCommand GuiVisibilityCmd { get; private set; }
        public UIPanel[] Exceptions { get; private set; }

        public GuiVisibilityChangeEvent(object source, GuiVisibilityCommand guiVisibilityCmd, params UIPanel[] exceptions)
            : base(source) {
            GuiVisibilityCmd = guiVisibilityCmd;
            Exceptions = exceptions;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }

    /// <summary>
    /// Command that determines whether this button hides other
    /// Gui elements or restores them.
    /// </summary>
    public GuiVisibilityCommand guiVisibilityCmd;

    /// <summary>
    /// The list of UIPanels that should be excepted from the GuiVisibilityCommand.
    /// </summary>
    public UIPanel[] guiVisibilityExceptions;

    protected override void OnLeftClick() {
        switch (guiVisibilityCmd) {
            case GuiVisibilityCommand.HideVisibleGuiPanels:
                if (guiVisibilityExceptions.IsNullOrEmpty<UIPanel>()) {
                    D.Warn("{0} has no GuiVisibilityExceptions listed.", gameObject.name);
                }
                break;
            case GuiVisibilityCommand.RestoreInvisibleGuiPanels:
                break;
            case GuiVisibilityCommand.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(guiVisibilityCmd));
        }
        _eventMgr.Raise<GuiVisibilityChangeEvent>(new GuiVisibilityChangeEvent(this, guiVisibilityCmd, guiVisibilityExceptions));
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

