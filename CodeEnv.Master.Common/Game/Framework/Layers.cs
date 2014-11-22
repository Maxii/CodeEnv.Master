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
    /// Enum for Layers that avoids typing out strings or trying to remember the int value. 
    /// Use Layers.[Constant].GetName() extension for the string name or (int){Layers.[Constant] 
    /// for the index. Generate a bit mask to isolate a layer like this: 
    /// <c>int universeEdgeLayerOnlyMask = LayerMaskExtensions.CreateInclusiveMask(Layers.UniverseEdge);</c>
    /// </summary>
    public enum Layers {

        // Layers 0 - 7 reserved by Unity ***************************************************

        Default = 0,
        TransparentFX = 1,
        IgnoreRaycast = 2,  // ignores ALL raycasts, including Unity built-in raycasts like detecting collisions

        Water = 4,
        /// <summary>
        /// Layer used by UI Elements that are fixed on the screen, aka don't popup. Added by Unity 4.5.
        /// </summary>
        UI = 5,

        // *************************************************************************************

        //Gui2D = 8,    // Removed 8.29.14, replaced by new built-in layer UI introduced in Unity 4.5
        UniverseEdge = 9,
        DummyTarget = 10,
        SectorView = 11,  // Unused

        /// <summary>
        /// Layer used by UI Elements that popup on the screen. Includes partial screen menus, fullScreen menus, dialogs and ContextMenus.
        /// Allows disablement of normal UI layer input when showing.
        /// </summary>
        UIPopup = 12,

        Ship = 14,
        Facility = 15,
        Planetoid = 16,
        Star = 17,

        /// <summary>
        /// Layer unique to a SystemView whose collider matches the OrbitalPlane. Used to change the
        /// UICamera.eventReceiverMask to test for occluded objects behind the collider.
        /// </summary>
        SystemOrbitalPlane = 19,

        DeepSpace = 20,
        CelestialObjectKeepout = 21,
        // IgnoreGuiEvents = 22, // Removed 3.19.14, replaced by IgnoreRaycast

        Vectrosity2D = 31

    }
}

