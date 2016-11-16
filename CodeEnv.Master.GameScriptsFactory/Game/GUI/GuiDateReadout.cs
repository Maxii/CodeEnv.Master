// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiDateReadout.cs
// Date readout class for the Gui, based on Ngui UILabel.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

#define ENABLE_PROFILER

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Date readout class for the Gui, based on Ngui UILabel.
/// </summary>
public class GuiDateReadout : AGuiLabelReadout {

    protected override string TooltipContent { get { return "The current date in the game."; } }

    private GameTime _gameTime;

    protected override void Awake() {
        base.Awake();
        _gameTime = GameTime.Instance;
        Subscribe();
    }

    private void Subscribe() {
        //D.Log("{0} subscribing to {1}.calenderDateChanged.", GetType().Name, typeof(GameTime).Name);
        _gameTime.calenderDateChanged += CalenderDateChangedEventHandler;
    }


    #region Event and Property Change Handlers

    private void CalenderDateChangedEventHandler(object sender, EventArgs e) {
        string formattedCalenderDateText = _gameTime.CurrentDate.CalenderFormattedDate; // 11.15.16 no extra allocation
        RefreshReadout(formattedCalenderDateText);
    }

    #endregion

    protected override void Cleanup() {
        Unsubscribe();
    }

    private void Unsubscribe() {
        _gameTime.calenderDateChanged -= CalenderDateChangedEventHandler;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

