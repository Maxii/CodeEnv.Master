// --------------------------------------------------------------------------------------------------------------------
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

        /* Layers 0 - 7 reserved by Unity ***************************************************/

        Default = 0,
        TransparentFX = 1,
        IgnoreRaycast = 2,  // ignores ALL raycasts, including Unity built-in raycasts like detecting collisions

        Water = 4,  // not used
        /// <summary>
        /// Layer used by UI Elements that are fixed on the screen, aka don't popup. Added by Unity 4.5.
        /// </summary>
        UI = 5,

        /*************************************************************************************/

        //Gui2D = 8,    // Removed 8.29.14, replaced by new built-in layer UI introduced in Unity 4.5

        /************************************************************************************************
                    * These 2 layers exist to allow the MainCameraControl to raycast using a mask which avoids
                    * interference from other colliders.
                    *************************************************************************************************/
        UniverseEdge = 9,
        DummyTarget = 10,

        //SectorView = 11,  // Unused

        /// <summary>
        /// Layer used by UI Elements that popup on the screen. Allows the normal UI layer 
        /// to be masked off which keeps it from responding to Ngui events.
        /// Includes partial screen menus, fullScreen menus, dialogs and ContextMenus.
        /// </summary>
        //UIPopup = 12, // No longer used 6.7.15

        /************************************************************************************************
                    * These 4 layers exist to allow the MainCamera to automatically cull the meshes present 
                     * on the layer beyond a predefined distance. Avoids calculating camera distance in update.
                    *************************************************************************************************/
        ShipCull = 14,
        FacilityCull = 15,
        PlanetoidCull = 16,
        StarCull = 17,

        /// <summary>
        /// Layer that allows the System OrbitalPlane Collider to be masked off from Ngui event raycasts. 
        /// Used when checking for occluded colliders behind the plane collider.
        /// </summary>
        SystemOrbitalPlane = 19,

        /// <summary>
        /// Layer that allows the dedicated DeepSpace Background camera to cull all meshes
        /// except the DeepSpace background meshes in the scene.
        /// </summary>
        DeepSpace = 20,

        /// <summary>
        /// Layer that allows Fleets and Ships to raycast without interference from 
        /// other colliders, for the purpose of detecting keepout zones to avoid.
        /// </summary>
        CelestialObjectKeepout = 21,

        /// <summary>
        /// Layer for shields so a shield collision can be tested for by a Beam RayCast. 
        /// All layers including the Shields layer will ignore collisions with the Shields layer.
        /// </summary>
        Shields = 22,        // IgnoreGuiEvents = 22, // Removed 3.19.14, replaced by IgnoreRaycast

        /// <summary>
        /// Layer for Beam Ordnance so it can 
        /// </summary>
        Beams = 23,

        /// <summary>
        /// Layer for Projectile Ordnance so they won't collide with each other when trying to impact a Target on the Default layer. 
        /// </summary>
        Projectiles = 24

        /// <summary>
        /// Layer that allows the dedicated VectorCam (Vectrosity Camera) to cull all meshes
        /// except the Vectrosity2D meshes in the scene.
        /// </summary>
        //Vectrosity2D = 31 // Removed 2.20.15 when upgrading to Vectrosity 4.0

    }
}

