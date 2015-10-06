// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ElementCategoryExtensions.cs
// Extension methods for Facility and Ship Category.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    /// <summary>
    /// Extension methods for Facility and Ship Category.
    /// </summary>
    public static class ElementCategoryExtensions {

        // TODO MaxMountPoints and Dimensions are temporary until I determine whether I want either value
        // to be independant of the element category. If independant, the values would then be placed
        // in the HullStat.

        public static int __MaxLOSWeapons(this ShipHullCategory cat) {
            int value = 0;
            switch (cat) {
                case ShipHullCategory.Fighter:
                case ShipHullCategory.Scout:
                    value = 1;
                    break;
                case ShipHullCategory.Science:
                case ShipHullCategory.Support:
                case ShipHullCategory.Colonizer:
                case ShipHullCategory.Troop:
                    value = 2;
                    break;
                case ShipHullCategory.Frigate:
                    value = 3;
                    break;
                case ShipHullCategory.Destroyer:
                case ShipHullCategory.Carrier:
                    value = 4;
                    break;
                case ShipHullCategory.Cruiser:
                    value = 7;
                    break;
                case ShipHullCategory.Dreadnaught:
                    value = TempGameValues.MaxLosWeaponsForAnyElement;   // 12
                    break;
                case ShipHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(cat));
            }
            D.Assert(value <= TempGameValues.MaxLosWeaponsForAnyElement);
            return value;
        }

        public static int __MaxMissileWeapons(this ShipHullCategory cat) {
            int value = 0;
            switch (cat) {
                case ShipHullCategory.Fighter:
                case ShipHullCategory.Scout:
                    break;
                case ShipHullCategory.Science:
                case ShipHullCategory.Support:
                case ShipHullCategory.Colonizer:
                case ShipHullCategory.Troop:
                    value = 1;
                    break;
                case ShipHullCategory.Frigate:
                    value = 2;
                    break;
                case ShipHullCategory.Destroyer:
                case ShipHullCategory.Carrier:
                    value = 3;
                    break;
                case ShipHullCategory.Cruiser:
                    value = 4;
                    break;
                case ShipHullCategory.Dreadnaught:
                    value = TempGameValues.MaxMissileWeaponsForAnyElement;   // 6
                    break;
                case ShipHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(cat));
            }
            D.Assert(value <= TempGameValues.MaxMissileWeaponsForAnyElement);
            return value;
        }

        public static Vector3 __HullDimensions(this ShipHullCategory cat) {
            switch (cat) {
                case ShipHullCategory.Frigate:
                    return new Vector3(.03F, .01F, .05F);
                case ShipHullCategory.Destroyer:
                case ShipHullCategory.Support:
                    return new Vector3(.05F, .02F, .10F);
                case ShipHullCategory.Cruiser:
                case ShipHullCategory.Science:
                case ShipHullCategory.Colonizer:
                    return new Vector3(.07F, .04F, .15F);
                case ShipHullCategory.Dreadnaught:
                case ShipHullCategory.Troop:
                    return new Vector3(.10F, .06F, .25F);
                case ShipHullCategory.Carrier:
                    return new Vector3(.12F, .06F, .35F);
                case ShipHullCategory.Fighter:
                case ShipHullCategory.Scout:
                case ShipHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(cat));
            }
        }

        public static int __MaxLOSWeapons(this FacilityHullCategory cat) {
            int value = 0;
            switch (cat) {
                case FacilityHullCategory.Economic:
                case FacilityHullCategory.Factory:
                case FacilityHullCategory.Laboratory:
                case FacilityHullCategory.Barracks:
                case FacilityHullCategory.ColonyHab:
                    value = 2;
                    break;
                case FacilityHullCategory.CentralHub:
                case FacilityHullCategory.Defense:
                    value = TempGameValues.MaxLosWeaponsForAnyElement;   // 12
                    break;
                case FacilityHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(cat));
            }
            D.Assert(value <= TempGameValues.MaxLosWeaponsForAnyElement);
            return value;
        }

        public static int __MaxMissileWeapons(this FacilityHullCategory cat) {
            int value = 0;
            switch (cat) {
                case FacilityHullCategory.Economic:
                case FacilityHullCategory.Factory:
                case FacilityHullCategory.Laboratory:
                case FacilityHullCategory.ColonyHab:
                case FacilityHullCategory.Barracks:
                    value = 2;
                    break;
                case FacilityHullCategory.CentralHub:
                case FacilityHullCategory.Defense:
                    value = TempGameValues.MaxMissileWeaponsForAnyElement;
                    break;
                case FacilityHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(cat));
            }
            D.Assert(value <= TempGameValues.MaxMissileWeaponsForAnyElement);
            return value;
        }

        /// <summary>
        /// The dimensions of the facility (capsule) with this Category. 
        /// x is Radius, y is height. The Axis Direction is assumed to be the Y-Axis, aka 1.
        /// </summary>
        /// <param name="cat">The category.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static Vector2 __HullDimensions(this FacilityHullCategory cat) {
            switch (cat) {
                case FacilityHullCategory.CentralHub:
                case FacilityHullCategory.Defense:
                    return new Vector2(.10F, .40F);
                case FacilityHullCategory.Economic:
                case FacilityHullCategory.Factory:
                case FacilityHullCategory.Laboratory:
                case FacilityHullCategory.ColonyHab:
                case FacilityHullCategory.Barracks:
                    return new Vector2(.05F, .20F);
                case FacilityHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(cat));
            }
        }

    }
}

