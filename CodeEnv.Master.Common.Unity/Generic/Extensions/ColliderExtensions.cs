// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ColliderExtensions.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

namespace CodeEnv.Master.Common.Unity {

    using UnityEngine;

    public static class ColliderExtensions {

        /// <summary>
        /// Gets the current OnScreen diameter in pixels of this collider. Can be Zero if not
        /// on the screen. WARNING: Donot use to scale an object
        /// </summary>
        /// <param name="col">The collider.</param>
        /// <returns>The diameter of the collider on the screen. Can be zero.</returns>
        public static float OnScreenDiameter(this Collider col) {
            Vector3 colliderPosition = col.transform.position;
            Debug.Log("ColliderPosition = {0}.".Inject(colliderPosition));
            if (!UnityUtility.IsVisibleAt(colliderPosition)) {
                return Constants.ZeroF;
            }
            float colliderDiameter = col.bounds.extents.magnitude;
            Debug.Log("ColliderDiameter = {0}.".Inject(colliderDiameter));
            float distanceFromCamera = Vector3.Distance(colliderPosition, Camera.main.transform.position);
            Debug.Log("DistanceFromCamera = {0}.".Inject(distanceFromCamera));
            float angularSize = (colliderDiameter / distanceFromCamera) * Mathf.Rad2Deg;
            Debug.Log("AngularSize = {0}.".Inject(angularSize));
            float pixelSize = ((angularSize * Screen.height) / Camera.main.fieldOfView);
            Debug.Log("PixelSize = {0}.".Inject(pixelSize));
            return pixelSize;
        }



    }
}

