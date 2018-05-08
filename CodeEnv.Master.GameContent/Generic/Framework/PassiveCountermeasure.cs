// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PassiveCountermeasure.cs
// A countermeasure that only has DamageMitigation capabilities.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// A countermeasure that only has DamageMitigation capabilities.
    /// </summary>
    public class PassiveCountermeasure : AEquipment, ICountermeasure {

        private const string NameFormat = "{0}_{1:0.#}";

        public override string Name { get { return NameFormat.Inject(base.Name, DmgMitigation.__Total); } }

        public DamageStrength DmgMitigation { get { return Stat.DmgMitigation; } }

        protected new PassiveCountermeasureStat Stat { get { return base.Stat as PassiveCountermeasureStat; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="PassiveCountermeasure"/> class.
        /// </summary>
        /// <param name="stat">The stat.</param>
        /// <param name="name">The optional unique name for this equipment. If not provided, the name embedded in the stat will be used.</param>
        public PassiveCountermeasure(PassiveCountermeasureStat stat, string name = null) : base(stat, name) { }

        public override bool AreSpecsEqual(AEquipmentStat otherStat) {
            return Stat == otherStat as PassiveCountermeasureStat;
        }
    }
}

