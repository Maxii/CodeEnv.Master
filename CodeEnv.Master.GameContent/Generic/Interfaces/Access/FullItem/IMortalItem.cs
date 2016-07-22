﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IMortalItem.cs
// Interface for easy access to MonoBehaviours that are AMortalItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Interface for easy access to MonoBehaviours that are AMortalItems.
    /// </summary>
    public interface IMortalItem : IIntelItem {

        event EventHandler deathOneShot;

        Index3D SectorIndex { get; }

    }
}

