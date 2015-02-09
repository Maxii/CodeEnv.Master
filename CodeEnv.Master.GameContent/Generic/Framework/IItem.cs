// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IItem.cs
//  Interface for all items.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    ///  Interface for all items.
    /// </summary>
    public interface IItem {

        /// <summary>
        /// The name to use for debugging. Includes parent name.
        /// </summary>
        string FullName { get; }

        float Radius { get; }

        Player Owner { get; }

        Vector3 Position { get; }

    }
}

