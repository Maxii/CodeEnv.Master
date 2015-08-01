// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FacilityStat2.cs
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
    public struct FacilityStat2 {

        public string Name { get; private set; }
        public float HullMass { get { return _hullStat.Mass; } }
        public float HullMaxHitPoints { get { return _hullStat.MaxHitPoints; } }
        public FacilityCategory HullCategory { get { return _hullStat.Category; } }
        public float Science { get { return _hullStat.Science; } }
        public float Culture { get { return _hullStat.Culture; } }
        public float Income { get { return _hullStat.Income; } }
        public float Expense { get { return _hullStat.Expense; } }

        private FacilityHullStat _hullStat;

        public FacilityStat2(string name, FacilityHullStat hullStat) : this() {
            Name = name;
            _hullStat = hullStat;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

