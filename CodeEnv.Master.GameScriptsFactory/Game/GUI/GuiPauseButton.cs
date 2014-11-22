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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

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
public class GuiPauseButton : AGuiButtonBase {

    protected override string TooltipContent {  // called by AGuiTooltip before _pauseButtonLabel reference is established
        get { return _pauseButtonLabel != null ? "{0} the game.".Inject(_pauseButtonLabel.text) : "I'm not empty."; }
    }

    private UILabel _pauseButtonLabel;
    private IList<IDisposable> _subscribers;

    protected override void Awake() {
        base.Awake();
        _pauseButtonLabel = _button.GetComponentInChildren<UILabel>();
        UpdateButtonLabel();
        Subscribe();
    }

    private void Subscribe() {
        _subscribers = new List<IDisposable>();
        _subscribers.Add(_gameMgr.SubscribeToPropertyChanged<GameManager, bool>(gs => gs.IsPaused, OnIsPausedChanged));
    }

    private void OnIsPausedChanged() {
        UpdateButtonLabel();
    }

    protected override void OnLeftClick() {
        bool toPause = !_gameMgr.IsPaused;
        _gameMgr.RequestPauseStateChange(toPause, toOverride: true);
    }

    private void UpdateButtonLabel() {
        string labelContent = _gameMgr.IsPaused ? UIMessages.ResumeButtonLabel : UIMessages.PauseButtonLabel;
        _pauseButtonLabel.text = labelContent;
    }

    protected override void Cleanup() {
        Unsubscribe();
    }

    private void Unsubscribe() {
        _subscribers.ForAll<IDisposable>(s => s.Dispose());
        _subscribers.Clear();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

