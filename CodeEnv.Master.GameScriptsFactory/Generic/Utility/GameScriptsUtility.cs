// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameScriptsUtility.cs
// Utility for MonoBehaviours where script interfaces can't be used.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Utility for MonoBehaviours where script interfaces can't be used.
/// </summary>
public static class GameScriptsUtility {

    /// <summary>
    /// Merges the provided fleets together, returning the fleetCmd that was selected as the best Cmd with elements
    /// from all fleets. If the best Cmd does not already have a hero, assigns the best Hero from the other fleets, if any.
    /// </summary>
    /// <param name="fleets">The fleets to merge.</param>
    /// <param name="optionalFleetName">Optional name of the fleet.</param>
    /// <returns></returns>
    public static FleetCmdItem Merge(IEnumerable<FleetCmdItem> fleets, string optionalFleetName = null) {
        Utility.ValidateNotNullOrEmpty(fleets);
        fleets.ForAll(f => D.Assert(!f.IsDead));

        // make a copy so destroying a fleet in fleets does not cause a 'modified while iterating' exception in caller
        var allCmds = fleets.ToArray();
        FleetCmdItem bestCmd = allCmds.Cast<IFleetCmd>().GetMostEffective() as FleetCmdItem;

        var lesserCmds = allCmds.Except(bestCmd);

        if (!bestCmd.IsHeroPresent) {
            // If bestCmd already has a hero, that is the hero that will lead the merged fleet.
            // Find the cmd with the best hero in the other cmds, if any, as it should join bestCmd first to transfer the hero.
            FleetCmdItem bestHeroLesserCmd = null;
            var lesserCmdsWithHeros = lesserCmds.Where(cmd => cmd.IsHeroPresent);
            if (lesserCmdsWithHeros.Any()) {
                var heros = lesserCmdsWithHeros.Select(f => f.Data.Hero);
                Hero bestHero = heros.GetMostExperienced();
                bestHeroLesserCmd = lesserCmdsWithHeros.Single(cmd => cmd.Data.Hero == bestHero);

            }
            if (bestHeroLesserCmd != null) {
                bestHeroLesserCmd.Join(bestCmd);    // will transfer the hero too 
                lesserCmds = lesserCmds.Except(bestHeroLesserCmd);
            }
        }

        foreach (var lesserCmd in lesserCmds) {
            lesserCmd.Join(bestCmd);    // no heros will transfer as 1) bestCmd already has a hero, or 2) there are no heros
        }
        if (!optionalFleetName.IsNullOrEmpty()) {
            bestCmd.UnitName = optionalFleetName;
        }
        return bestCmd;
    }

}

