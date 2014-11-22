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

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Date readout class for the Gui, based on Ngui UILabel.
/// </summary>
public class GuiDateReadout : AGuiLabelReadoutBase {

    protected override string TooltipContent {
        get { return "The current date in the game."; }
    }

    private IList<IDisposable> _subscribers;

    protected override void Awake() {
        base.Awake();
        Subscribe();
    }

    private void Subscribe() {
        _subscribers = new List<IDisposable>();
        //D.Log("{0} subscribing to {1}.onDateChanged.", GetType().Name, typeof(GameTime).Name);
        GameTime.Instance.onDateChanged += OnDateChanged;
    }

    private void OnDateChanged(GameDate newDate) {
        RefreshReadout(newDate.ToString());
    }

    protected override void Cleanup() {
        Unsubscribe();
    }

    private void Unsubscribe() {
        _subscribers.ForAll<IDisposable>(s => s.Dispose());
        _subscribers.Clear();
        GameTime.Instance.onDateChanged -= OnDateChanged;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

