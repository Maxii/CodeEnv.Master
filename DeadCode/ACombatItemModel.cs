// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ACombatItemModel.cs
// Abstract base class for Items that can engage in combat and cause weapons to fire.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.GameContent;

/// <summary>
/// Abstract base class for Items that can engage in combat and cause weapons to fire.
/// </summary>
public abstract class ACombatItemModel : AMortalItemModelStateMachine, ICombatTarget {

    public new ACombatItemData Data {
        get { return base.Data as ACombatItemData; }
        set { base.Data = value; }
    }

    #region ICombatTarget Members

    public float MaxWeaponsRange { get { return Data.MaxWeaponsRange; } }

    #endregion
}

