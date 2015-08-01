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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// A countermeasure that only has DamageMitigation capabilities.
    /// </summary>
    public class PassiveCountermeasure : AEquipment, ICountermeasure {

        private static string _editorNameFormat = "{0}_{1:0.#}";

        public override string Name {
            get {
#if UNITY_EDITOR
                return _editorNameFormat.Inject(base.Name, DamageMitigation.Total);
#else
                return base.Name;
#endif
            }
        }

        public DamageStrength DamageMitigation { get { return Stat.DamageMitigation; } }

        protected new PassiveCountermeasureStat Stat { get { return base.Stat as PassiveCountermeasureStat; } }

        public PassiveCountermeasure(PassiveCountermeasureStat stat) : base(stat) { }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

