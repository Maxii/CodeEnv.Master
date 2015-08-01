// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PassiveCountermeasure.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using CodeEnv.Master.GameContent;
    using UnityEngine;

    /// <summary>
    /// 
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

