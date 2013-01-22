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

namespace CodeEnv.Master.Common.Unity {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.Resources;
    using UnityEngine;

    [Serializable]
    public class CameraFacing {

        private Transform _billboardTransform;
        private Transform _cameraTransform;

        public bool reverseFacing = false;

        public CameraFacing(Transform billboardTransform, Transform cameraTransform) {
            _billboardTransform = billboardTransform;
            _cameraTransform = cameraTransform;
        }

        public void UpdateFacing() {
            // Rotates the billboard transform provided so its forward aligns with that of the provided camera's transform, ie. the direction the camera is looking.
            // In effect, by adopting the camera's forward direction, the billboard is pointing at the camera's focal plane, not at the camera. 
            // It is the camera's focal plane whose image is projected onto the screen so that is what must be 'looked at'.
            Vector3 targetPos = _billboardTransform.position + _cameraTransform.rotation * (reverseFacing ? Vector3.forward : Vector3.back);
            Vector3 targetOrientation = _cameraTransform.rotation * Vector3.up;
            _billboardTransform.LookAt(targetPos, targetOrientation);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

