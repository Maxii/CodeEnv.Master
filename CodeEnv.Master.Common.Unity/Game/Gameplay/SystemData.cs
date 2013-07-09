// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemData.cs
// All the data associated with a particular system.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

namespace CodeEnv.Master.Common.Unity {

    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    /// <summary>
    /// All the data associated with a particular system.
    /// </summary>
    public class SystemData : AData {

        private static string starNameEndsWith = Constants.Space + CommonTerms.Star;

        public string StarName {
            get {
                return Name + starNameEndsWith;
            }
        }

        public int Capacity { get; set; }

        public OpeYield Resources { get; set; }

        public XYield SpecialResources { get; set; }

        public SettlementSize SettlementSize { get; set; }

        public SystemData(Transform t) : base(t) { }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

