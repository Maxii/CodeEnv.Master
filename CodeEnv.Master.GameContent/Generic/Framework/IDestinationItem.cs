// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IDestinationItem.cs
// Interface for an Item that is a movement destination.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Interface for an Item that is a movement destination.
    /// </summary>
    public interface IDestinationItem {

        string Name { get; }

        Vector3 Position { get; }

        bool IsMovable { get; }

        float Radius { get; }

    }
}

