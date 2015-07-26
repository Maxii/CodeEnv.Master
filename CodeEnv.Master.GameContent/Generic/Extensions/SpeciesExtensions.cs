﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SpeciesExtensions.cs
// Extension methods for Species and SpeciesGuiSelection values.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;

    /// <summary>
    /// Extension methods for Species and SpeciesGuiSelection values.
    /// </summary>
    public static class SpeciesExtensions {

        /// <summary>
        /// Converts this SpeciesGuiSelection value to a Species value.
        /// </summary>
        /// <param name="speciesSelection">The species selection.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static Species Convert(this SpeciesGuiSelection speciesSelection) {
            switch (speciesSelection) {
                case SpeciesGuiSelection.Random:
                    return Enums<Species>.GetRandom(excludeDefault: true);
                case SpeciesGuiSelection.Human:
                    return Species.Human;
                case SpeciesGuiSelection.Borg:
                    return Species.Borg;
                case SpeciesGuiSelection.Dominion:
                    return Species.Dominion;
                case SpeciesGuiSelection.Klingon:
                    return Species.Klingon;
                case SpeciesGuiSelection.Ferengi:
                    return Species.Ferengi;
                case SpeciesGuiSelection.Romulan:
                    return Species.Romulan;
                case SpeciesGuiSelection.GodLike:
                    return Species.GodLike;
                case SpeciesGuiSelection.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(speciesSelection));
            }
        }

    }
}

