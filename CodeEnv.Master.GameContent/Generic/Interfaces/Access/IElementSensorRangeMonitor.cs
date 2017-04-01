// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IElementSensorRangeMonitor.cs
// Interface allowing access to ElementSensorRangeMonitor MonoBehaviours.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface allowing access to ElementSensorRangeMonitor MonoBehaviours.
    /// </summary>
    public interface IElementSensorRangeMonitor : ISensorRangeMonitor {

        IUnitElement ParentItem { get; }


    }
}

