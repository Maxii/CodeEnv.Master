// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IZoomToFurthest.cs
// Tells the camera when zooming in a direction containing multiple game objects along
// a ray, to zoom to the furthest object on the ray with this interface, if no other objects 
// not holding this interface are present. If any other objects without this interface are
// present along the ray, the closest one will be the zoom target.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    /// <summary>
    /// Tells the camera when zooming in a direction containing multiple game objects along
    /// a ray, to zoom to the furthest object on the ray with this interface, if no other objects 
    /// not holding this interface are present. If any other objects without this interface are
    /// present along the ray, the closest one will be the zoom target.
    /// </summary>
    public interface IZoomToFurthest {

    }
}

