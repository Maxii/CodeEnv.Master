﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ItemData.cs
// Basic instantiable class of AItemData.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Basic instantiable class of AItemData.
    /// </summary>
    public class ItemData : AItemData {

        //public ItemData(string name, string optionalParentName = "") : base(name, optionalParentName) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemData" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public ItemData(string name) : base(name) { }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

