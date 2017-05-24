// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AWeaponStat.cs
// Immutable, abstract base stat containing externally acquirable values for Weapons.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable, abstract base stat containing externally acquirable values for Weapons.
    /// </summary>
    public abstract class AWeaponStat : ARangedEquipmentStat {

        private const string DebugNameFormat = "{0}(DeliveryVehicleStrength[{1}], DamagePotential[{2}], Range[{3}]).";

        public override string DebugName {
            get {
                if (_debugName == null) {
                    _debugName = DebugNameFormat.Inject(base.DebugName, DeliveryVehicleStrength.DebugName, DamagePotential.DebugName,
                        RangeCategory.GetValueName());
                }
                return _debugName;
            }
        }

        public WDVCategory DeliveryVehicleCategory { get { return DeliveryVehicleStrength.Category; } }

        public WDVStrength DeliveryVehicleStrength { get; private set; }

        public DamageStrength DamagePotential { get; private set; }

        public float ReloadPeriod { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WeaponStat" /> struct.
        /// </summary>
        /// <param name="name">The name of the weapon.</param>
        /// <param name="imageAtlasID">The image atlas identifier.</param>
        /// <param name="imageFilename">The image filename.</param>
        /// <param name="description">The description.</param>
        /// <param name="size">The physical size of the weapon.</param>
        /// <param name="mass">The mass.</param>
        /// <param name="pwrRqmt">The power required to operate the weapon.</param>
        /// <param name="expense">The expense.</param>
        /// <param name="rangeCat">The range category of the weapon.</param>
        /// <param name="deliveryVehicleStrength">The delivery strength.</param>
        /// <param name="reloadPeriod">The time it takes to reload the weapon in hours.</param>
        /// <param name="damagePotential">The damage potential.</param>
        /// <param name="isDamageable">if set to <c>true</c> [is damageable].</param>
        public AWeaponStat(string name, AtlasID imageAtlasID, string imageFilename, string description, float size, float mass, float pwrRqmt,
            float expense, RangeCategory rangeCat, WDVStrength deliveryVehicleStrength, float reloadPeriod,
            DamageStrength damagePotential, bool isDamageable)
            : base(name, imageAtlasID, imageFilename, description, size, mass, pwrRqmt, expense, rangeCat, isDamageable) {
            DeliveryVehicleStrength = deliveryVehicleStrength;
            ReloadPeriod = reloadPeriod;
            DamagePotential = damagePotential;
        }


    }
}

