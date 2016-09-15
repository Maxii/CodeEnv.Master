// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UserPlayerKnowledge.cs
// Holds the current knowledge of the user about items in the universe.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using CodeEnv.Master.GameContent;
    using UnityEngine;

    /// <summary>
    /// Holds the current knowledge of the user about items in the universe.
    /// What is known by the user about each item is available through the item from Reports.
    /// </summary>
    public class UserPlayerKnowledge : PlayerKnowledge {

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerKnowledge" /> class.
        /// <remarks>Used to create the instance when DebugSettings.AllIntelCoverageComprehensive = true.</remarks>
        /// </summary>
        /// <param name="uCenter">The UniverseCenter.</param>
        /// <param name="allStars">All stars.</param>
        /// <param name="allPlanetoids">All planetoids.</param>
        public UserPlayerKnowledge(IUniverseCenter_Ltd uCenter, IEnumerable<IStar_Ltd> allStars, IEnumerable<IPlanetoid_Ltd> allPlanetoids)
            : base(References.GameManager.UserPlayer, uCenter, allStars) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerKnowledge"/> class.
        /// </summary>
        /// <param name="uCenter">The UniverseCenter.</param>
        /// <param name="allStars">All stars.</param>
        public UserPlayerKnowledge(IUniverseCenter_Ltd uCenter, IEnumerable<IStar_Ltd> allStars)
            : base(References.GameManager.UserPlayer, uCenter, allStars) {
        }

        /// <summary>
        /// Returns <c>true</c> if the sector indicated by sectorIndex contains one or more ISectorViewHighlightables, <c>false</c> otherwise.
        /// </summary>
        /// <param name="sectorIndex">Index of the sector.</param>
        /// <param name="highlightablesInSector">The highlightables in sector.</param>
        /// <returns></returns>
        public bool TryGetSectorViewHighlightables(IntVector3 sectorIndex, out IEnumerable<ISectorViewHighlightable> highlightablesInSector) {
            D.Assert(sectorIndex != default(IntVector3), "{0}: SectorIndex of {1} is illegal.", GetType().Name, sectorIndex);
            List<ISectorViewHighlightable> sectorHighlightables = new List<ISectorViewHighlightable>();
            ISystem_Ltd system;
            if (TryGetSystem(sectorIndex, out system)) {
                ISectorViewHighlightable sys = system as ISectorViewHighlightable;
                D.Assert(sys != null);
                sectorHighlightables.Add(sys);
            }
            IEnumerable<IStarbaseCmd_Ltd> starbases;
            if (TryGetStarbases(sectorIndex, out starbases)) {
                IEnumerable<ISectorViewHighlightable> highlightableStarbases = starbases.Cast<ISectorViewHighlightable>();
                D.Assert(!highlightableStarbases.IsNullOrEmpty());
                sectorHighlightables.AddRange(highlightableStarbases);
            }
            IEnumerable<IFleetCmd_Ltd> fleets;
            if (TryGetFleets(sectorIndex, out fleets)) {
                IEnumerable<ISectorViewHighlightable> highlightableFleets = fleets.Cast<ISectorViewHighlightable>();
                D.Assert(!highlightableFleets.IsNullOrEmpty());
                sectorHighlightables.AddRange(highlightableFleets);
            }

            if (sectorHighlightables.Any()) {
                highlightablesInSector = sectorHighlightables;
                return true;
            }
            highlightablesInSector = null;
            return false;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

