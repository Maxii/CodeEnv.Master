// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiPauseButton.cs
// Custom Gui button control for the main User Pause Button.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Custom Gui button control for the main Player Pause/Resume Button.
/// </summary>
public class GuiPauseButton : AGuiButton {

    private static IEnumerable<KeyCode> _validKeys = new KeyCode[] { KeyCode.Pause };

    protected override IEnumerable<KeyCode> ValidKeys { get { return _validKeys; } }

    protected override string TooltipContent {  // called by AGuiTooltip before _pauseButtonLabel reference is established
        get { return _pauseButtonLabel != null ? "{0} the game.".Inject(_pauseButtonLabel.text) : "I'm not empty."; }
    }

    private UILabel _pauseButtonLabel;
    private IList<IDisposable> _subscriptions;

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        _pauseButtonLabel = _button.GetComponentInChildren<UILabel>();
        UpdateButtonLabel();
        Subscribe();
    }

    private void Subscribe() {
        _subscriptions = new List<IDisposable>();
        _subscriptions.Add(_gameMgr.SubscribeToPropertyChanged<GameManager, bool>(gs => gs.IsPaused, IsPausedPropChangedHandler));
    }

    protected override void HandleValidClick() {
        bool toPause = !_gameMgr.IsPaused;
        _gameMgr.RequestPauseStateChange(toPause, toOverride: true);
    }

    #region Event and Property Change Handlers

    private void IsPausedPropChangedHandler() {
        UpdateButtonLabel();
    }

    #endregion

    private void UpdateButtonLabel() {
        string labelContent = _gameMgr.IsPaused ? UIMessages.ResumeButtonLabel : UIMessages.PauseButtonLabel;
        _pauseButtonLabel.text = labelContent;
    }

    protected override void Cleanup() {
        Unsubscribe();
    }

    private void Unsubscribe() {
        _subscriptions.ForAll<IDisposable>(s => s.Dispose());
        _subscriptions.Clear();
    }

}

