// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CmdSensorRangeMonitor.cs
// SensorRangeMonitor for MR and LR Sensors located with the UnitCmd.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// SensorRangeMonitor for MR and LR Sensors located with the UnitCmd.
/// </summary>
public class CmdSensorRangeMonitor : ASensorRangeMonitor, ICmdSensorRangeMonitor {

    public event EventHandler isOperationalChanged;

    public new IUnitCmd ParentItem {
        get { return base.ParentItem as IUnitCmd; }
        set { base.ParentItem = value; }
    }

    #region Event and Property Change Handlers

    protected override void HandleIsOperationalChanged() {
        base.HandleIsOperationalChanged();
        OnIsOperationalChanged();
    }

    private void OnIsOperationalChanged() {
        if (isOperationalChanged != null) {
            isOperationalChanged(this, EventArgs.Empty);
        }
    }

    #endregion
}

