// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UnityUtilities.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common.Unity {

    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;


    public static class UnityUtility {

        /// <summary>
        /// Determines whether the world point provided is currently visible from the main camera.
        /// </summary>
        /// <param name="worldPoint">The world point.</param>
        /// <returns>
        ///   <c>true</c> if [is visible at] [the specified world point]; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsVisibleAt(Vector3 worldPoint) {
            Vector3 viewportPoint = Camera.main.WorldToViewportPoint(worldPoint);
            if (Utility.IsInRange(viewportPoint.x, 0F, 1F) && Utility.IsInRange(viewportPoint.y, 0F, 1F) && viewportPoint.z > 0) {
                return true;
            }
            return false;
        }

        public static void ValidateComponentPresence<T>(GameObject go) where T : Component {
            System.Diagnostics.StackFrame stackFrame = new System.Diagnostics.StackTrace().GetFrame(1);
            D.Assert(go.GetComponent<T>() != null, ErrorMessages.ComponentNotFound.Inject(typeof(T), stackFrame.GetMethod().Name));
        }

        public static void ValidateMonoBehaviourPresence<T>(GameObject go) where T : MonoBehaviour {
            System.Diagnostics.StackFrame stackFrame = new System.Diagnostics.StackTrace().GetFrame(1);
            D.Assert(go.GetSafeMonoBehaviourComponent<T>() != null, ErrorMessages.ComponentNotFound.Inject(typeof(T), stackFrame.GetMethod().Name));
        }

    }
}

