// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ICmdSensorRangeMonitor.cs
// Interface allowing access to CmdSensorRangeMonitor MonoBehaviours.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;

    /// <summary>
    /// Interface allowing access to CmdSensorRangeMonitor MonoBehaviours.
    /// </summary>
    public interface ICmdSensorRangeMonitor : ISensorRangeMonitor {

        event EventHandler isOperationalChanged;

        IUnitCmd ParentItem { get; }


    }
}

