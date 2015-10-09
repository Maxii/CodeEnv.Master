﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AudioOptionMenuAcceptButton.cs
// Accept button for the AudioOptionsMenu. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;

/// <summary>
/// Accept button for the AudioOptionsMenu. 
/// </summary>
public class AudioOptionMenuAcceptButton : AGuiMenuAcceptButton {

    protected override string TooltipContent { get { return "Click to implement Option changes."; } }

    protected override void OnLeftClick() {
        base.OnLeftClick();
        // TODO
    }

    protected override void ValidateStateOnCapture() {
        base.ValidateStateOnCapture();
        // TODO
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}
