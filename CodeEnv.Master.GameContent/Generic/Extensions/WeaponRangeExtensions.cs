// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: WeaponRangeExtensions.cs
// Extension class providing values acquired externally from Xml for the WeaponRange enum.
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
    /// Extension class providing values acquired externally from Xml for the WeaponRange enum.
    /// TODO Externalize values in XML.
    /// </summary>
    public static class WeaponRangeExtensions {

        public static float GetValue(this WeaponRange range, Species species) {
            var speciesModifier = species.GetWeaponRangeModifier();
            switch (range) {
                case WeaponRange.Short:
                    return 4F * speciesModifier;
                case WeaponRange.Medium:
                    return 7F * speciesModifier;
                case WeaponRange.Long:
                    return 10F * speciesModifier;
                case WeaponRange.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(range));
            }
        }

    }
}

