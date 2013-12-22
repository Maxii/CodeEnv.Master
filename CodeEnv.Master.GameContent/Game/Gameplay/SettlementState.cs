// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementState.cs
// Enum defining the states a Settlement can operate in.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Enum defining the states a Settlement can operate in.
    /// </summary>
    public enum SettlementState {

        None,
        Idling,
        ShowDying,
        Dying,
        ProcessOrders,
        Dead

    }
}

