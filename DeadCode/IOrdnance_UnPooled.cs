// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IOrdnance_UnPooled.cs
// Interface for Ordnance of all types.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using UnityEngine;

    /// <summary>
    /// Interface for Ordnance of all types.
    /// </summary>
    public interface IOrdnance_UnPooled {

        event EventHandler deathOneShot;

        string Name { get; }

        Vector3 CurrentHeading { get; }

        Player Owner { get; }

        IElementAttackable Target { get; }

    }
}

