// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Countermeasure.cs
// A MortalItem's defensive Countermeasure.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// A MortalItem's defensive Countermeasure.
    /// </summary>
    [Obsolete]
    public class Countermeasure : AEquipment {

        private static string _editorNameFormat = "{0}_{1:0.#}";

        public override string Name {
            get {
#if UNITY_EDITOR
                return _editorNameFormat.Inject(base.Name, Strength.Combined);
#else
                return base.Name;
#endif
            }
        }

        public WDVStrength __DeliveryInterceptability { get { return new WDVStrength(WDVCategory.Beam, 2.5F); } }

        public DamageStrength __DamageMitigation { get { return new DamageStrength(thermal: 1F, atomic: 0F, kinetic: 2F); } }


        public CombatStrength Strength { get { return Stat.Strength; } }

        protected new CountermeasureStat Stat { get { return base.Stat as CountermeasureStat; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="Countermeasure" /> class.
        /// </summary>
        /// <param name="stat">The stat.</param>
        public Countermeasure(CountermeasureStat stat)
            : base(stat) {
        }

        public override string ToString() { return Stat.ToString(); }

    }
}

