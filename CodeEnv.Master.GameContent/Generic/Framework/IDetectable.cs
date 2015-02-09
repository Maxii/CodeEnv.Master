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

    /// <summary>
    /// Interface indicating an Item is detectable by sensors.
    /// </summary>
    public interface IDetectable {

        void OnDetectionGained(ICommandItem cmdItem, DistanceRange sensorRange);

        void OnDetectionLost(ICommandItem cmdItem, DistanceRange sensorRange);
    }
}

