// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Data.cs
// Basic instantiable class for AData.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Basic instantiable class for AData.
    /// </summary>
    public class Data : AData {

        /// <summary>
        /// Initializes a new instance of the <see cref="Data"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="optionalParentName">Name of the optional parent.</param>
        public Data(string name, string optionalParentName = "") : base(name, optionalParentName) { }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

