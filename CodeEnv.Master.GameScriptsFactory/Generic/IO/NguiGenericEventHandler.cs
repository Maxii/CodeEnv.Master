// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: NguiGenericEventHandler.cs
// COMMENT - one line to give a brief idea of what this file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;

/// <summary>
/// COMMENT 
/// </summary>
public class NguiGenericEventHandler : NguiEventFallthruHandler {

    protected override void InitializeOnStart() {
        UICamera.genericEventHandler = this.gameObject;
    }

    protected override void WriteMessageToConsole(string msg) {
        D.Error(msg);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

