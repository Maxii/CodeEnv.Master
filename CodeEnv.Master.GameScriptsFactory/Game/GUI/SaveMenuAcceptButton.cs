// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SaveMenuAcceptButton.cs
//  Accept button for the SaveGameMenu.
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
/// Accept button for the SaveGameMenu.
/// </summary>
public class SaveMenuAcceptButton : AGuiMenuAcceptButton {

    private static IEnumerable<KeyCode> _validKeys = new KeyCode[] { KeyCode.Return };

    protected override IEnumerable<KeyCode> ValidKeys { get { return _validKeys; } }

    protected override string TooltipContent { get { return "Click to save game."; } }

    #region Event and Property Change Handlers

    protected override void HandleValidClick() {
        base.HandleValidClick();
        _gameMgr.SaveGame("Game");
    }

    #endregion

    protected override void Cleanup() { }


}

