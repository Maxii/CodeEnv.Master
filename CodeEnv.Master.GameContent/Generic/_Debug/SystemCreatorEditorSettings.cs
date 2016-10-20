// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemCreatorEditorSettings.cs
// Data container for System Creator editor settings used in debugging.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Data container for System Creator editor settings used in debugging.
    /// </summary>
    public class SystemCreatorEditorSettings {

        public StarCategory PresetStarCategory { get; private set; }

        public bool IsCompositionPreset { get; private set; }

        public int NonPresetPlanetQty { get; private set; }

        /// <summary>
        /// Planetoid Category for each planet. 
        /// <remarks>List index indicates which planet.</remarks>
        /// </summary>
        public IList<PlanetoidCategory> PresetPlanetCategories { get; private set; }

        /// <summary>
        /// Planetoid Categories for the moons that go with each planet. 
        /// <remarks>List index indicates which planet.</remarks>
        /// </summary>
        public IList<PlanetoidCategory[]> PresetMoonCategories { get; private set; }

        public SystemDesirability Desirability { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemCreatorEditorSettings" /> class for SystemCreators that have preset values.
        /// </summary>
        /// <param name="presetStarCat">The preset star cat.</param>
        /// <param name="presetPlanetCats">The preset planet cats.</param>
        /// <param name="presetMoonCats">The preset moon cats.</param>
        /// <param name="desirability">The desirability.</param>
        public SystemCreatorEditorSettings(StarCategory presetStarCat, IList<PlanetoidCategory> presetPlanetCats,
            IList<PlanetoidCategory[]> presetMoonCats, SystemDesirability desirability) {
            PresetStarCategory = presetStarCat;
            IsCompositionPreset = true;
            D.Assert(presetPlanetCats.Count <= TempGameValues.TotalOrbitSlotsPerSystem - 1, "{0} > {1}.", presetPlanetCats.Count, TempGameValues.TotalOrbitSlotsPerSystem - 1);
            PresetPlanetCategories = presetPlanetCats;
            PresetMoonCategories = presetMoonCats;
            Desirability = desirability;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemCreatorEditorSettings" /> class for SystemCreators that do not have preset values.
        /// </summary>
        /// <param name="nonPresetPlanetQty">The non preset planet qty.</param>
        /// <param name="desirability">The desirability.</param>
        public SystemCreatorEditorSettings(int nonPresetPlanetQty, SystemDesirability desirability) {
            IsCompositionPreset = false;
            NonPresetPlanetQty = nonPresetPlanetQty;
            Desirability = desirability;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

