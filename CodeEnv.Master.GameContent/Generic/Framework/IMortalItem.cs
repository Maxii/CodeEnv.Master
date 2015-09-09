﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IMortalItem.cs
// Interface for easy access to all items that can die.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Interface for easy access to all items that can die.
    /// </summary>
    public interface IMortalItem : IIntelItem {

        event Action<IMortalItem> onDeathOneShot;

        Index3D SectorIndex { get; }

    }
}

