// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IDetectableItem.cs
// Interface indicating an Item is detectable by sensors.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using UnityEngine;

    /// <summary>
    /// Interface indicating an Item is detectable by sensors.
    /// </summary>
    public interface IDetectable {

        bool IsOperational { get; }

        string FullName { get; }

        Vector3 Position { get; }

        void OnDetection(ICommandItem cmdItem, DistanceRange sensorRange);

        void OnDetectionLost(ICommandItem cmdItem, DistanceRange sensorRange);
    }
}

