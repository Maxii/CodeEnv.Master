// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CameraFacing.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR


namespace CodeEnv.Master.Common.Unity {

    using System;
    using CodeEnv.Master.Common;
    using UnityEngine;

    [Obsolete]
    public class CameraFacing {

        private Transform billboardTransform;
        private Transform cameraTransform;

        public bool reverseFacing = false;

        public CameraFacing(Transform billboard, Transform camera) {
            billboardTransform = billboard;
            cameraTransform = camera;
        }

        public void UpdateFacing() {
            // Rotates the billboard t provided so its forward aligns with that of the provided camera's t, ie. the direction the camera is looking.
            // In effect, by adopting the camera's forward direction, the billboard is pointing at the camera's focal plane, not at the camera. 
            // It is the camera's focal plane whose image is projected onto the screen so that is what must be 'looked at'.
            Vector3 targetPos = billboardTransform.position + cameraTransform.rotation * (reverseFacing ? Vector3.forward : Vector3.back);
            Vector3 targetOrientation = cameraTransform.rotation * Vector3.up;
            billboardTransform.LookAt(targetPos, targetOrientation);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

