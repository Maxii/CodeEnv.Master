// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarDesign.cs
// A Star Design.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// A Star Design.
    /// </summary>
    public struct StarDesign {

        public string DebugName { get { return GetType().Name; } }

        public string DesignName { get; private set; }

        public StarStat StarStat { get; private set; }

        public StarDesign(string designName, StarStat stat) {
            DesignName = designName;
            StarStat = stat;
        }

        public override string ToString() {
            return DebugName;
        }

    }
}

