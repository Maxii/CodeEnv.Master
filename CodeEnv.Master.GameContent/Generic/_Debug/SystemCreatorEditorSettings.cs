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

        //public int CMsPerPlanetoid { get; private set; }
        public SystemDesirability Desirability { get; private set; }

        public bool IsTrackingLabelEnabled { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemCreatorEditorSettings" /> class for SystemCreators that have preset values.
        /// </summary>
        /// <param name="presetStarCat">The preset star cat.</param>
        /// <param name="presetPlanetCats">The preset planet cats.</param>
        /// <param name="presetMoonCats">The preset moon cats.</param>
        /// <param name="cmsPerPlanetoid">The CMS per planetoid.</param>
        /// <param name="enableTrackingLabel">if set to <c>true</c> [enable tracking label].</param>
        public SystemCreatorEditorSettings(StarCategory presetStarCat, IList<PlanetoidCategory> presetPlanetCats,
            IList<PlanetoidCategory[]> presetMoonCats, /*int cmsPerPlanetoid,*/ SystemDesirability desirability, bool enableTrackingLabel) {
            PresetStarCategory = presetStarCat;
            IsCompositionPreset = true;
            D.Assert(presetPlanetCats.Count <= TempGameValues.TotalOrbitSlotsPerSystem - 1, "{0} > {1}.", presetPlanetCats.Count, TempGameValues.TotalOrbitSlotsPerSystem - 1);
            PresetPlanetCategories = presetPlanetCats;
            PresetMoonCategories = presetMoonCats;
            //CMsPerPlanetoid = cmsPerPlanetoid;
            Desirability = desirability;
            IsTrackingLabelEnabled = enableTrackingLabel;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemCreatorEditorSettings"/> class for SystemCreators that do not have preset values.
        /// </summary>
        /// <param name="nonPresetPlanetQty">The non preset planet qty.</param>
        /// <param name="nonPresetMoonQty">The non preset moon qty.</param>
        /// <param name="cmsPerPlanetoid">The CMS per planetoid.</param>
        /// <param name="enableTrackingLabel">if set to <c>true</c> [enable tracking label].</param>
        public SystemCreatorEditorSettings(int nonPresetPlanetQty, /*int cmsPerPlanetoid,*/ SystemDesirability desirability, bool enableTrackingLabel) {
            IsCompositionPreset = false;
            NonPresetPlanetQty = nonPresetPlanetQty;
            //CMsPerPlanetoid = cmsPerPlanetoid;
            Desirability = desirability;
            IsTrackingLabelEnabled = enableTrackingLabel;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

