// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IStarbaseCmd.cs
// Interface for easy access to MonoBehaviours that are StarbaseCmdItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for easy access to MonoBehaviours that are StarbaseCmdItems.
    /// </summary>
    public interface IStarbaseCmd : IUnitBaseCmd {

        StarbaseCmdReport UserReport { get; }

        StarbaseCmdReport GetReport(Player player);

        FacilityReport[] GetElementReports(Player player);

    }
}

