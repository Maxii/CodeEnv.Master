// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiGameSpeedReadout.cs
// GameSpeed readout class for the Gui, based on Ngui UILabel.
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

/// <summary>
/// GameSpeed readout class for the Gui, based on Ngui UILabel.
/// </summary>
public class GuiGameSpeedReadout : AGuiLabelReadout {

    protected override string TooltipContent { get { return "Game Speed relative to Normal."; } }

    private IList<IDisposable> _subscriptions;

    protected override void Awake() {
        base.Awake();
        Subscribe();
        RefreshReadout(PlayerPrefsManager.Instance.GameSpeedOnLoad);
    }

    private void Subscribe() {
        _subscriptions = new List<IDisposable>();
        _subscriptions.Add(GameTime.Instance.SubscribeToPropertyChanged<GameTime, GameSpeed>(gt => gt.GameSpeed, GameSpeedPropChangedHandler));
    }

    #region Event and Property Change Handlers

    private void GameSpeedPropChangedHandler() {
        RefreshReadout(GameTime.Instance.GameSpeed);
    }

    #endregion

    private void RefreshReadout(GameSpeed clockSpeed) {
        RefreshReadout(CommonTerms.MultiplySign + clockSpeed.SpeedMultiplier().ToString());
    }

    protected override void Cleanup() {
        Unsubscribe();
    }

    private void Unsubscribe() {
        _subscriptions.ForAll<IDisposable>(s => s.Dispose());
        _subscriptions.Clear();
    }


}

