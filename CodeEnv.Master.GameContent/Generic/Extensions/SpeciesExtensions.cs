// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SpeciesExtensions.cs
// Extension class providing values acquired externally from Xml for the Species enum.
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
    /// Extension class providing values acquired externally from Xml for the Species enum.
    /// TODO Externalize values in XML.
    /// </summary>
    public static class SpeciesExtensions {

        public static float GetWeaponRangeModifier(this Species species) {
            switch (species) {
                case Species.Human:
                    return 1.0F;
                case Species.BorgLike:
                    return 1.0F;
                case Species.DominionLike:
                    return 1.0F;
                case Species.FrerengiLike:
                    return 1.0F;
                case Species.GodLike:
                    return 1.0F;
                case Species.KlingonLike:
                    return 1.0F;
                case Species.RomulanLike:
                    return 1.0F;
                case Species.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(species));
            }
        }

        public static float GetWeaponReloadTimeModifier(this Species species) {
            // TODO
            return Constants.OneF;
        }
    }
}

