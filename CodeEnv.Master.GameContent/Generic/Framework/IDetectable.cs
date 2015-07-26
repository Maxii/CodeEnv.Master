// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IDetectableItem.cs
// Interface indicating an Item is detectable by sensors and weapon targeting systems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using UnityEngine;

    /// <summary>
    /// Interface indicating an Item is detectable by sensors and weapon targeting systems.
    /// </summary>
    public interface IDetectable {

        event Action<IItem> onOwnerChanged;

        Player Owner { get; }

        bool IsOperational { get; }

        string FullName { get; }

        Vector3 Position { get; }

        void OnDetection(IUnitCmdItem cmdItem, RangeDistanceCategory sensorRangeCat);

        void OnDetectionLost(IUnitCmdItem cmdItem, RangeDistanceCategory sensorRangeCat);
    }
}

