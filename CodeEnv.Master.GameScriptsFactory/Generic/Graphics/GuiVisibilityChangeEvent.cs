// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiVisibilityChangeEvent.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using CodeEnv.Master.Common;

// Note: Must remain in Scripts as it references NGUI class
public class GuiVisibilityChangeEvent : GameEvent {

    public GuiVisibilityCommand GuiVisibilityCmd { get; private set; }
    public UIPanel[] Exceptions { get; private set; }

    public GuiVisibilityChangeEvent(GuiVisibilityCommand guiVisibilityCmd, params UIPanel[] exceptions) {
        GuiVisibilityCmd = guiVisibilityCmd;
        Exceptions = exceptions;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

