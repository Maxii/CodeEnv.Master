// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IUnitRepairCapable.cs
// Interface for Items that can provide accelerated repair of Units including the CmdModule.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.GameContent;

/// <summary>
/// Interface for Items that can provide accelerated repair of Units including the CmdModule.
/// <remarks>Bases only. If accelerated repair at a base is not available, the CmdModule can repair in place at a very slow rate.</remarks>
/// </summary>
////[System.Obsolete]
public interface IUnitRepairCapable : IRepairCapable, IFleetNavigableDestination, IElementNavigableDestination {

    /// <summary>
    /// Gets the repair capacity available for this Unit's CmdModule in hitPts per day.
    /// </summary>
    /// <param name="unitCmd">The unit command module.</param>
    /// <param name="hqElement">The HQElement.</param>
    /// <param name="cmdOwner">The command owner.</param>
    /// <returns></returns>
    float GetAvailableRepairCapacityFor(IUnitCmd_Ltd unitCmd, IUnitElement_Ltd hqElement, Player cmdOwner);


}

