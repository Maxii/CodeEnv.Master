// --------------------------------------------------------------------------------------------------------------------
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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System.Collections.Generic;
using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Accept button for the AudioOptionsMenu. 
/// </summary>
public class AudioOptionMenuAcceptButton : AGuiMenuAcceptButton {

    private static IEnumerable<KeyCode> _validKeys = new KeyCode[] { KeyCode.Return };

    protected override IEnumerable<KeyCode> ValidKeys { get { return _validKeys; } }

    protected override string TooltipContent { get { return "Click to implement Option changes."; } }

    #region Event and Property Change Handlers

    protected override void HandleValidClick() {
        base.HandleValidClick();
        // TODO
    }

    #endregion

    protected override void __ValidateCapturedState() {
        base.__ValidateCapturedState();
        //TODO
    }

    protected override void Cleanup() { }


}

