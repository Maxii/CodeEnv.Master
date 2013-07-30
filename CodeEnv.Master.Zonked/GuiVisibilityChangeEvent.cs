// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiVisibilityChangeEvent.cs
// Event that delivers a GuiVisibilityCommand and the UIPanel exceptions that can accompany it.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using CodeEnv.Master.Common;

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

