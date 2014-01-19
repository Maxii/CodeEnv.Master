﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Layers.cs
// Simple Enum for Layers that avoids typing out strings or trying to remember the int value.. Use Layers.[Constant].GetName() extension for
// the string name or (int){Layers.[Constant] for the index.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    using System;

    /// <summary>
    /// Simple Enum for Layers that avoids typing out strings or trying to remember the int value. 
    /// Use Layers.[Constant].GetName() extension for the string name or (int){Layers.[Constant] 
    /// for the index. Generate a bit mask to isolate a layer like this: 
    /// <c>collideWithUniverseEdgeLayerOnlyBitMask = 1 << (int)Layers.UniverseEdge;</c>
    /// </summary>
    public enum Layers {

        Default = 0,
        TransparentFX = 1,
        IgnoreRaycast = 2,

        [Obsolete]
        Water = 4,



        Gui2D = 8,
        UniverseEdge = 9,
        DummyTarget = 10,
        SectorView = 11,  // Unused

        Ships = 14,
        BasesSettlements = 15,
        Planetoids = 16,
        Stars = 17,


        DeepSpace = 20,
        CelestialObjectKeepout = 21,

        Vectrosity2D = 31

    }
}

