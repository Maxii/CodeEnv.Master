// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IWeaponRangeMonitor.cs
// Interface for access to a WeaponRangeMonitor.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;

    /// <summary>
    /// Interface for access to a WeaponRangeMonitor.
    /// </summary>
    public interface IWeaponRangeMonitor : IRangedEquipmentMonitor {

        IUnitElement ParentItem { set; get; }

    }
}

