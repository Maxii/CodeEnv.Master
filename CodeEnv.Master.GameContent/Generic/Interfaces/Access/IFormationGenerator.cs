// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IFormationGenerator.cs
// Interface for easy access to the singleton FormationGenerator.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Interface for easy access to the singleton FormationGenerator.
    /// </summary>
    public interface IFormationGenerator {

        /// <summary>
        /// Generates the specified formation for a Base.
        /// Returns a list of FormationStationSlotInfo instances containing the slotID and the local space relative
        /// position (offset relative to the position of the HQElement) of each station slot in the formation including the HQElement's station.
        /// </summary>
        /// <param name="formation">The formation.</param>
        /// <param name="cmdTransform">The command transform.</param>
        /// <param name="formationRadius">The resulting unit formation radius.</param>
        /// <returns></returns>
        List<FormationStationSlotInfo> GenerateBaseFormation(Formation formation, Transform cmdTransform, out float formationRadius);

        /// <summary>
        /// Generates the specified formation for a Fleet.
        /// Returns a list of FormationStationSlotInfo instances containing the slotID and the local space relative
        /// position (offset relative to the position of the HQElement) of each station slot in the formation including the HQElement's station.
        /// </summary>
        /// <param name="formation">The formation.</param>
        /// <param name="cmdTransform">The command transform.</param>
        /// <param name="formationRadius">The resulting unit formation radius.</param>
        /// <returns></returns>
        List<FormationStationSlotInfo> GenerateFleetFormation(Formation formation, Transform cmdTransform, out float formationRadius);

        /// <summary>
        /// Generates a formation to hold ships that are resident in a Base's Hanger.
        /// Returns a list of FormationStationSlotInfo instances containing the slotID and the local space relative
        /// position (offset relative to the position of the followTransform) of each station slot in the formation which can,
        /// but does not need to include a slot at the location of the followTransform.
        /// <remarks>Including 'formation' allows future use of other hanger formations.</remarks>
        /// </summary>
        /// <param name="formation">The hanger formation.</param>
        /// <param name="followTransform">The transform this formation is to follow.</param>
        /// <param name="formationRadius">The resulting hanger formation radius.</param>
        /// <returns></returns>
        List<FormationStationSlotInfo> GenerateHangerFormation(Formation formation, Transform followTransform, out float formationRadius);

    }
}

