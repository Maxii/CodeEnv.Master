// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IDetectableItem.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using CodeEnv.Master.GameContent;
    using UnityEngine;

    /// <summary>
    /// 
    /// </summary>
    public interface IDetectableItem {

        //void SetIntelCoverage(Player player, IntelCoverage coverage);

        //void OnDetection(Player player, DistanceRange sensorRange);

        void OnDetectionGained(ICommandItem cmdItem, DistanceRange sensorRange);

        void OnDetectionLost(ICommandItem cmdItem, DistanceRange sensorRange);
    }
}

