// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IRangedEquipmentMonitor.cs
// Interface allowing access to RangedEquipmentMonitors.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;

    /// <summary>
    ///  Interface allowing access to RangedEquipmentMonitors.
    /// </summary>
    public interface IRangedEquipmentMonitor {

        string Name { get; }

        RangeDistanceCategory RangeCategory { get; }

        Player Owner { get; }

        void Reset();

    }
}

