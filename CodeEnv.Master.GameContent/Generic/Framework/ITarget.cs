// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ITarget.cs
// Interface for an Item that is a target of another Item.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Interface for an Item that is a target of another Item.
    /// </summary>
    public interface ITarget {

        string Name { get; }

        Vector3 Position { get; }

        bool IsMovable { get; }

        float Radius { get; }

    }
}

