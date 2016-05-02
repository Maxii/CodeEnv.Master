// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Layers.cs
// Simple Enum for Layers that avoids typing out strings or trying to remember the int value.. Use Layers.[Constant].GetValueName() extension for
// the string name or (int){Layers.[Constant] for the index.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    using System;

    /// <summary>
    /// Enum for Layers that avoids typing out strings or trying to remember the int value. 
    /// Use Layers.[Constant].GetValueName() extension for the string name or (int){Layers.[Constant] 
    /// for the index. Generate a bit mask to isolate a layer like this: 
    /// <c>int universeEdgeLayerOnlyMask = LayerMaskExtensions.CreateInclusiveMask(Layers.UniverseEdge);</c>
    /// </summary>
    public enum Layers {

        /* Layers 0 - 7 reserved by Unity ***************************************************/

        Default = 0,
        TransparentFX = 1,
        IgnoreRaycast = 2,  // ignores ALL raycasts, including Unity built-in raycasts like detecting collisions

        Water = 4,  // not used
        /// <summary>
        /// Layer used by UI Elements that are fixed on the screen, aka don't popup. Added by Unity 4.5.
        /// </summary>
        UI = 5,

        /*Layers 0 - 7 reserved by Unity ****************************************************/

        /*************************************************************************************
        * Gui2D = 8,    // Removed 8.29.14, replaced by new built-in layer UI introduced in Unity 4.5
        * UniverseEdge = 9, // Eliminated 4.29.16
        * DummyTarget = 10, // Eliminated 4.29.16
        * SectorView = 11,  // Unused
        *************************************************************************************************/

        /// <summary>
        /// Layer used by UI Elements that popup on the screen. Allows the normal UI layer 
        /// to be masked off which keeps it from responding to Ngui events.
        /// Includes partial screen menus, fullScreen menus, dialogs and ContextMenus.
        /// </summary>
        //UIPopup = 12, // No longer used 6.7.15

        /************************************************************************************************
         * These layers exist to allow the MainCamera to automatically cull the meshes present 
         * on the layer beyond a predefined distance. Avoids calculating camera distance in update.
         *************************************************************************************************/

        /// <summary>
        /// Layer allowing the MainCamera to cull objects more than &lt;&lt;1 unit away.
        /// Typically used by fighter and ornance meshes.
        /// </summary>
        Cull_Tiny = 8,

        /// <summary>
        /// Layer allowing the MainCamera to cull objects more than 1 unit away.
        /// Typically used by meshes associated with the Frigate class of ships.
        /// </summary>
        Cull_1 = 9,

        /// <summary>
        /// Layer allowing the MainCamera to cull objects more than 2 units away.
        /// Typically used by meshes associated with the Destroyer class of ships.
        /// </summary>
        Cull_2 = 10,

        /// <summary>
        /// Layer allowing the MainCamera to cull objects more than 3 units away.
        /// Typically used by meshes associated with the Cruiser class of ships.
        /// </summary>
        Cull_3 = 11,

        /// <summary>
        /// Layer allowing the MainCamera to cull objects more than 4 units away.
        /// Typically used by meshes associated with the larger classes of ships.
        /// </summary>
        Cull_4 = 12,

        /// <summary>
        /// Layer allowing the MainCamera to cull objects more than 15 units away.
        /// Typically used by meshes associated with the larger classes of facilities.
        /// </summary>
        Cull_15 = 13,

        /// <summary>
        /// Layer allowing the MainCamera to cull objects more than 200 units away.
        /// Typically used by meshes associated with moons and small planets.
        /// </summary>
        Cull_200 = 14,

        /// <summary>
        /// Layer allowing the MainCamera to cull objects more than 400 units away.
        /// Typically used by meshes associated with large planets.
        /// </summary>
        Cull_400 = 15,

        /// <summary>
        /// Layer allowing the MainCamera to cull objects more than 1000 units away.
        /// Typically used by Planet Icons.
        /// </summary>
        Cull_1000 = 16,

        /// <summary>
        /// Layer allowing the MainCamera to cull objects more than 3000 units away.
        /// Typically used by meshes associated with Stars and the SystemOrbitalPlane.
        /// </summary>
        Cull_3000 = 17,

        /// <summary>
        /// Layer allowing the MainCamera to cull objects more than 8 units away.
        /// Typically used by meshes associated with the smaller classes of facilities.
        /// </summary>
        Cull_8 = 18,

        /// <summary>
        /// Layer that allows the System OrbitalPlane Collider to be masked off from Ngui event raycasts. 
        /// Used when checking for occluded colliders behind the plane collider.
        /// </summary>
        SystemOrbitalPlane = 19,

        /// <summary>
        /// Layer for Zones around non-ship Items (Obstacles) where ships are banned. 
        /// Separated as a layer to allow Fleets and Ships to raycast without interference from 
        /// other colliders, for the purpose of detecting these obstacles to avoid.
        /// </summary>
        AvoidableObstacleZone = 21,    // CelestialObjectKeepout = 21, // Name changed 11.20.15

        /// <summary>
        /// Layer for shields so a shield collision can be tested for by a Beam RayCast. 
        /// All layers including the Shields layer will ignore collisions with the Shields layer.
        /// </summary>
        Shields = 22,        // IgnoreGuiEvents = 22, // Removed 3.19.14, replaced by IgnoreRaycast

        //Beams = 23,   // Removed 10.20.15 as beams don't have colliders

        /// <summary>
        /// Layer for Projectile Ordnance so they won't collide with each other when trying to impact a Target on the Default layer. 
        /// </summary>
        Projectiles = 24,

        /// <summary>
        /// Layer that allows the dedicated VectorCam (Vectrosity Camera) to cull all meshes
        /// except the Vectrosity2D meshes in the scene.
        /// </summary>
        //Vectrosity2D = 31 // Removed 2.20.15 when upgrading to Vectrosity 4.0

        /// <summary>
        /// Layer for Zones around Ships used for collision detection. Must be separate from the AvoidableObstacleZone 
        /// layer to avoid interfering with ship and fleet raycasts. CollisionDetectionZone layer objects are allowed to 
        /// collide with each other and AvoidableObstacleZone layer objects to detect impending collisions.
        /// </summary>
        CollisionDetectionZone = 26,

        /// <summary>
        /// Layer that allows the dedicated DeepSpace Background camera to cull all meshes
        /// except the DeepSpace background meshes in the scene.
        /// </summary>
        DeepSpace = 31,

    }
}

