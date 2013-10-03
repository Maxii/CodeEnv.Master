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

namespace CodeEnv.Master.Common {

    using System;
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

        public static T ValidateComponentPresence<T>(GameObject go) where T : Component {
            System.Diagnostics.StackFrame stackFrame = new System.Diagnostics.StackTrace().GetFrame(1);
            T component = go.GetComponent<T>();
            D.Assert(component != null, ErrorMessages.ComponentNotFound.Inject(typeof(T).Name, stackFrame.GetMethod().Name));
            return component;
        }

        public static T ValidateComponentPresence<T>(Transform t) where T : Component {
            System.Diagnostics.StackFrame stackFrame = new System.Diagnostics.StackTrace().GetFrame(1);
            T component = t.GetComponent<T>();
            D.Assert(component != null, ErrorMessages.ComponentNotFound.Inject(typeof(T).Name, stackFrame.GetMethod().Name));
            return component;
        }

        public static T ValidateMonoBehaviourPresence<T>(GameObject go) where T : MonoBehaviour {
            System.Diagnostics.StackFrame stackFrame = new System.Diagnostics.StackTrace().GetFrame(1);
            T monoBehaviour = go.GetSafeMonoBehaviourComponent<T>();
            D.Assert(monoBehaviour != null, ErrorMessages.ComponentNotFound.Inject(typeof(T).Name, stackFrame.GetMethod().Name));
            return monoBehaviour;
        }

        public static T ValidateMonoBehaviourPresence<T>(Transform t) where T : MonoBehaviour {
            System.Diagnostics.StackFrame stackFrame = new System.Diagnostics.StackTrace().GetFrame(1);
            T monoBehaviour = t.gameObject.GetSafeMonoBehaviourComponent<T>();
            D.Assert(monoBehaviour != null, ErrorMessages.ComponentNotFound.Inject(typeof(T).Name, stackFrame.GetMethod().Name));
            return monoBehaviour;
        }

        public static I ValidateInterfacePresence<I>(GameObject go) where I : class {
            System.Diagnostics.StackFrame stackFrame = new System.Diagnostics.StackTrace().GetFrame(1);
            I i = go.GetInterface<I>();
            D.Assert(i != null, ErrorMessages.ComponentNotFound.Inject(typeof(I).Name, stackFrame.GetMethod().Name));
            return i;
        }

        public static I ValidateInterfacePresence<I>(Transform t) where I : class {
            System.Diagnostics.StackFrame stackFrame = new System.Diagnostics.StackTrace().GetFrame(1);
            I i = t.GetInterface<I>();
            D.Assert(i != null, ErrorMessages.ComponentNotFound.Inject(typeof(I).Name, stackFrame.GetMethod().Name));
            return i;
        }

        /// <summary>
        /// Gets the rendering bounds of the transform including any children.
        /// </summary>
        /// <param name="transform">The game object to get the bounding box for.</param>
        /// <param name="providedBounds">The provided bounding box that will be adjusted to encapsulate the transform and its children.</param>
        /// <param name="toEncapsulate">Used to determine if the bounds of the provided bounding box should affect the resulting bounding box.</param>
        /// <returns>Returns true if at least one bounding box was calculated.</returns>
        public static bool GetBoundWithChildren(Transform transform, ref Bounds providedBounds, ref bool toEncapsulate) {
            var bound = new Bounds();
            var didOne = false;

            // get 'this' bound
            if (transform.gameObject.renderer != null) {
                bound = transform.gameObject.renderer.bounds;
                if (toEncapsulate) {
                    providedBounds.Encapsulate(bound.min);
                    providedBounds.Encapsulate(bound.max);
                }
                else {
                    providedBounds.min = bound.min;
                    providedBounds.max = bound.max;
                    toEncapsulate = true;
                }

                didOne = true;
            }

            // union with bound(s) of any/all children
            foreach (Transform child in transform) {
                if (GetBoundWithChildren(child, ref providedBounds, ref toEncapsulate)) {
                    didOne = true;
                }
            }

            return didOne;
        }

    }
}

