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
        //public struct SystemCreatorEditorSettings : IEquatable<SystemCreatorEditorSettings> {

        //#region Comparison Operators Override

        //// see C# 4.0 In a Nutshell, page 254

        //public static bool operator ==(SystemCreatorEditorSettings left, SystemCreatorEditorSettings right) {
        //    return left.Equals(right);
        //}

        //public static bool operator !=(SystemCreatorEditorSettings left, SystemCreatorEditorSettings right) {
        //    return !left.Equals(right);
        //}

        //#endregion

        public string SystemName { get; private set; }

        public StarCategory PresetStarCategory { get; private set; }

        public bool IsCompositionPreset { get; private set; }

        public int NonPresetPlanetQty { get; private set; }

        public IList<PlanetoidCategory> PresetPlanetCategories { get; private set; }

        public int NonPresetMoonQty { get; private set; }

        public IList<PlanetoidCategory[]> PresetMoonCategories { get; private set; }
        //public IList<PlanetoidCategory> PresetMoonCategories { get; private set; }

        public int CMsPerPlanetoid { get; private set; }

        public bool IsTrackingLabelEnabled { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemCreatorEditorSettings" /> struct for SystemCreators that have preset values.
        /// </summary>
        /// <param name="systemName">Name of the system.</param>
        /// <param name="presetStarCat">The preset star cat.</param>
        /// <param name="presetPlanetCats">The preset planet cats.</param>
        /// <param name="presetMoonCats">The preset moon cats.</param>
        /// <param name="cmsPerPlanetoid">The CMS per planetoid.</param>
        /// <param name="enableTrackingLabel">if set to <c>true</c> [enable tracking label].</param>
        public SystemCreatorEditorSettings(string systemName, StarCategory presetStarCat, IList<PlanetoidCategory> presetPlanetCats, IList<PlanetoidCategory[]> presetMoonCats, int cmsPerPlanetoid, bool enableTrackingLabel)
            //: this() {
            {
            SystemName = systemName;
            PresetStarCategory = presetStarCat;
            IsCompositionPreset = true;
            PresetPlanetCategories = presetPlanetCats;
            PresetMoonCategories = presetMoonCats;
            CMsPerPlanetoid = cmsPerPlanetoid;
            IsTrackingLabelEnabled = enableTrackingLabel;
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="SystemCreatorEditorSettings"/> struct for SystemCreators that do not have preset values.
        /// </summary>
        /// <param name="nonPresetPlanetQty">The non preset planet qty.</param>
        /// <param name="nonPresetMoonQty">The non preset moon qty.</param>
        /// <param name="cmsPerPlanetoid">The CMS per planetoid.</param>
        /// <param name="enableTrackingLabel">if set to <c>true</c> [enable tracking label].</param>
        public SystemCreatorEditorSettings(string systemName, int nonPresetPlanetQty, int nonPresetMoonQty, int cmsPerPlanetoid, bool enableTrackingLabel)
            //: this() {
            {
            SystemName = systemName;
            IsCompositionPreset = false;
            NonPresetPlanetQty = nonPresetPlanetQty;
            NonPresetMoonQty = nonPresetMoonQty;
            CMsPerPlanetoid = cmsPerPlanetoid;
            IsTrackingLabelEnabled = enableTrackingLabel;
        }

        //#region Object.Equals and GetHashCode Override

        //public override bool Equals(object obj) {
        //    if (!(obj is SystemCreatorEditorSettings)) { return false; }
        //    return Equals((SystemCreatorEditorSettings)obj);
        //}

        ///// <summary>
        ///// Returns a hash code for this instance.
        ///// See "Page 254, C# 4.0 in a Nutshell."
        ///// </summary>
        ///// <returns>
        ///// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        ///// </returns>
        //public override int GetHashCode() {
        //    unchecked { // http://dobrzanski.net/2010/09/13/csharp-gethashcode-cause-overflowexception/
        //        int hash = 17;  // 17 = some prime number
        //        hash = hash * 31 + SystemName.GetHashCode(); // 31 = another prime number
        //        hash = hash * 31 + IsCompositionPreset.GetHashCode();
        //        hash = hash * 31 + NonPresetPlanetQty.GetHashCode();
        //        hash = hash * 31 + PresetPlanetCategories.GetHashCode();
        //        hash = hash * 31 + NonPresetMoonQty.GetHashCode();
        //        hash = hash * 31 + PresetMoonCategories.GetHashCode();
        //        hash = hash * 31 + CMsPerPlanetoid.GetHashCode();
        //        hash = hash * 31 + IsTrackingLabelEnabled.GetHashCode();
        //        return hash;
        //    }
        //}

        //#endregion

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        //#region IEquatable<SystemCreatorEditorSettings> Members

        //public bool Equals(SystemCreatorEditorSettings other) {
        //    return SystemName == other.SystemName && IsCompositionPreset == other.IsCompositionPreset && NonPresetPlanetQty == other.NonPresetPlanetQty
        //        && PresetPlanetCategories == other.PresetPlanetCategories && NonPresetMoonQty == other.NonPresetMoonQty
        //        && PresetMoonCategories == other.PresetMoonCategories && CMsPerPlanetoid == other.CMsPerPlanetoid
        //        && IsTrackingLabelEnabled == other.IsTrackingLabelEnabled;
        //}

        //#endregion

    }
}

