// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IFleetCmd_Ltd.cs
// limited InfoAccess Interface for easy access to MonoBehaviours that are FleetCmdItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR


namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// limited InfoAccess Interface for easy access to MonoBehaviours that are FleetCmdItems.
    /// </summary>
    public interface IFleetCmd_Ltd : IUnitCmd_Ltd {

        Reference<float> ActualSpeedValue_Debug { get; }

        bool IsLocatedIn(IntVector3 sectorID);

    }
}

