// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IData.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common.Unity {

    using UnityEngine;

    /// <summary>
    /// Interface providing access to the basic properties of the Data class.
    /// </summary>
    public interface IData {

        /// <summary>
        /// Gets or sets the name of the item. 
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Gets or sets the name of the Parent of this item. Optional.
        /// </summary>
        string OptionalParentName { get; set; }

        /// <summary>
        /// Readonly. Gets the position of the gameObject containing this data.
        /// </summary>
        Vector3 Position { get; }

        /// <summary>
        /// Gets or sets the date the human player last had 
        /// intel on this location. Used only when IntelState is
        /// OutOfDate to derive the age of the last intel, 
        /// this property only needs to be updated
        /// when the intel state changes to OutOfDate.
        /// </summary>
        GameDate LastHumanPlayerIntelDate { get; set; }

    }
}

