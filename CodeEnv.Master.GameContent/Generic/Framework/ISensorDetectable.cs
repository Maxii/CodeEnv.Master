// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ISensorDetectableItem.cs
// Interface for Items that are detectable by sensors.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using UnityEngine;

    /// <summary>
    /// Interface for Items that are detectable by sensors.
    /// </summary>
    public interface ISensorDetectable : IDetectable {

        event EventHandler ownerChanged;

        void HandleDetectionBy(IUnitCmdItem cmdItem, RangeCategory sensorRangeCat);

        void HandleDetectionLostBy(IUnitCmdItem cmdItem, RangeCategory sensorRangeCat);
    }
}

