// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarBaseState.cs
// Enum defining the states a StarBase can operate in.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Enum defining the states a StarBase can operate in.
    /// </summary>
    public enum StarBaseState {

        None,
        Idling,
        ShowDying,
        Dying,
        ProcessOrders,
        Dead

    }
}

