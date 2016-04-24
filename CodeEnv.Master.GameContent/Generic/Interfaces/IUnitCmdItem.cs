// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IUnitCmdItem.cs
// Interface for easy access to items that are unit commands.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;

    /// <summary>
    /// Interface for easy access to items that are unit commands.
    /// </summary>
    public interface IUnitCmdItem : IMortalItem {

        bool IsSelected { get; set; }

        IconInfo IconInfo { get; }

        bool __CheckForDamage(bool isHQElementAlive, DamageStrength elementDamageSustained, float elementDamageSeverity);

        void HandleSubordinateElementDeath(IUnitElementItem deadSubordinateElement);

    }
}

