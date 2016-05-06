// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UnityUtility.cs
// Static class of utility methods that are specific to Unity and/or Ngui.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System;
    using System.Linq;
    using System.Collections.Generic;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;
    using System.Collections;

    /// <summary>
    /// Static class of utility methods that are specific to Unity and/or Ngui.
    /// </summary>
    public static class UnityUtility {

        public static readonly IEqualityComparer<float> FloatEqualityComparer = new FloatEqualityComparer();

        public static readonly IEqualityComparer<Vector3> Vector3EqualityComparer = new Vector3EqualityComparer();

        /// <summary>
        /// Determines whether the world point provided is currently within the viewport of the main camera.
        /// </summary>
        /// <param name="worldPoint">The world point.</param>
        /// <returns>
        ///   <c>true</c> if the worldPoint is within the viewport of the main camera; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsWithinCameraViewport(Vector3 worldPoint) {
            return IsWithinCameraViewport(worldPoint, Camera.main);
        }

        /// <summary>
        /// Determines whether the world point provided is currently within the viewport of the provided camera.
        /// </summary>
        /// <param name="worldPoint">The world point.</param>
        /// <param name="camera">The camera.</param>
        /// <returns></returns>
        public static bool IsWithinCameraViewport(Vector3 worldPoint, Camera camera) {
            Vector3 viewportPoint = camera.WorldToViewportPoint(worldPoint);
            if (Utility.IsInRange(viewportPoint.x, 0F, 1F) && Utility.IsInRange(viewportPoint.y, 0F, 1F) && viewportPoint.z > 0) {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Validates the component presence on the gameObject and returns the component.
        /// Logs an error if the component isn't found.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="go">The go.</param>
        /// <returns></returns>
        public static T ValidateComponentPresence<T>(GameObject go) where T : Component {
            T component = go.GetComponent<T>();
            if (component == null) {
                System.Diagnostics.StackFrame stackFrame = new System.Diagnostics.StackTrace().GetFrame(1);
                D.Error(ErrorMessages.ComponentNotFound.Inject(typeof(T).Name, stackFrame.GetMethod().Name));
            }
            return component;
        }

        /// <summary>
        /// Gets the rendering bounds of the transform including any children.
        /// </summary>
        /// <param name="t">The Transform to get the bounding box for.</param>
        /// <param name="providedBounds">The provided bounding box that will be adjusted to encapsulate the transform and its children.</param>
        /// <param name="toEncapsulate">Used to determine if the bounds of the provided bounding box should affect the resulting bounding box.
        /// WARNING: this value can be changed by the method, so if iterating with this method, make sure it is reset to the value desired each time the
        /// method is used. </param>
        /// <returns>Returns true if at least one bounding box was calculated.</returns>
        public static bool GetBoundWithChildren(Transform t, ref Bounds providedBounds, ref bool toEncapsulate) {
            var bound = new Bounds();
            var didOne = false;

            // get 'this' bound
            var renderer = t.GetComponent<Renderer>();
            if (renderer != null) {                     //if (t.gameObject.renderer != null) {
                bound = renderer.bounds;                // bound = t.gameObject.renderer.bounds;
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
            foreach (Transform child in t) {
                if (GetBoundWithChildren(child, ref providedBounds, ref toEncapsulate)) {
                    didOne = true;
                }
            }
            return didOne;
        }

        /// <summary>
        /// Gets the bounding box surrounding all of the provided points in Worldspace.
        /// Derived from Vectrosity.VectorManager.
        /// </summary>
        /// <param name="points">The points to encompass in the bounding box.</param>
        /// <returns></returns>
        public static Bounds GetBounds(Vector3[] points) {
            var bounds = new Bounds();
            var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            var end = points.Length;

            for (int i = 0; i < end; i++) {
                if (points[i].x < min.x) {
                    min.x = points[i].x;
                }
                else if (points[i].x > max.x) {
                    max.x = points[i].x;
                }

                if (points[i].y < min.y) {
                    min.y = points[i].y;
                }
                else if (points[i].y > max.y) {
                    max.y = points[i].y;
                }

                if (points[i].z < min.z) {
                    min.z = points[i].z;
                }
                else if (points[i].z > max.z) {
                    max.z = points[i].z;
                }
            }

            bounds.min = min;
            bounds.max = max;
            return bounds;
        }

        /// <summary>
        /// Attaches the child to the parent, automatically aligning position,
        /// rotation, scale and layer to that of the parent, aka local position,
        /// rotation and scale are set to identity values.
        /// </summary>
        /// <param name="child">The child.</param>
        /// <param name="parent">The parent. If null, child becomes a root GameObject.</param>
        public static void AttachChildToParent(GameObject child, GameObject parent) {
            Utility.ValidateNotNull(child);
            Transform childTransform = child.transform;
            childTransform.parent = (parent != null) ? parent.transform : null;
            childTransform.localPosition = Vector3.zero;
            childTransform.localRotation = Quaternion.identity;
            childTransform.localScale = Vector3.one;
            child.layer = (parent != null) ? parent.layer : child.layer;
        }

        /// <summary>
        /// Instantiates an object and adds it to the specified parent with all local
        /// values reset. Removes Clone from name. WARNING: Awake() is called immediately after Instantiation, even before
        /// being attached to a parent.
        /// </summary>
        /// <param name="parent">The object to parent too. If null, the object is instantiated without a parent.</param>
        /// <param name="prefab">The childPrefab to instantiate from.</param>
        /// <returns></returns>
        public static GameObject AddChild(GameObject parent, GameObject childPrefab) {
            GameObject clonedChild = GameObject.Instantiate(childPrefab) as GameObject;
            clonedChild.name = childPrefab.name;
            //D.Log("Instantiated {0} and parented to {1}. {0}.Awake() has already occurred!", childPrefab.name, parent.name);
            AttachChildToParent(clonedChild, parent);
            return clonedChild;
        }

        /// <summary>
        /// Sets the layer of the parent and all its children, grandchildren, etc. to <c>layer</c>.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="layer">The layer.</param>
        public static void SetLayerRecursively(Transform parent, Layers layer) {
            parent.gameObject.layer = (int)layer;
            foreach (Transform child in parent) {
                child.gameObject.layer = (int)layer;
                if (child.childCount > Constants.Zero) {
                    SetLayerRecursively(child, layer);
                }
            }
        }

        /// <summary>
        /// Returns <c>true</c> if the two provided normalized directions are within the allowed deviation in degrees.
        /// </summary>
        /// <param name="dirA">The first direction.</param>
        /// <param name="dirB">The second direction.</param>
        /// <param name="allowedDeviation">The allowed deviation in degrees. Cannot be more precise
        /// than UnityConstants.AngleEqualityPrecision due to Unity floating point precision.</param>
        /// <returns></returns>
        public static bool AreDirectionsWithinTolerance(Vector3 dirA, Vector3 dirB, float allowedDeviation = UnityConstants.AngleEqualityPrecision) {
            dirA.ValidateNormalized();
            dirB.ValidateNormalized();
            D.Warn(allowedDeviation < UnityConstants.AngleEqualityPrecision, "Angle Deviation precision {0} cannot be < {1}.", allowedDeviation, UnityConstants.AngleEqualityPrecision);
            allowedDeviation = Mathf.Clamp(allowedDeviation, UnityConstants.AngleEqualityPrecision, 180F);
            float actualDeviation = Vector3.Angle(dirA, dirB);
            D.Log("Deviation between directions {0} and {1} is {2} degrees.", dirA, dirB, actualDeviation);
            return actualDeviation <= allowedDeviation;
        }

        #region Deprecated WaitFor Coroutines

        // 3.26.16 Deprecated all of these as their use is a bad practice

        /// <summary>
        /// Waits for the designated number of seconds, then executes the provided delegate.
        /// Usage:
        ///     WaitForSecondsToExecute(delayInSeconds, onWaitFinished: () =&gt; {
        ///         Code to execute after the wait;
        ///     });
        /// Warning: This method uses a coroutine Job. Accordingly, after being called it will 
        /// immediately return which means the code you have following it will execute 
        /// before the code assigned to the onWaitFinished delegate.
        /// </summary>
        /// <param name="onWaitFinished">The delegate to execute once the wait is finished.</param>
        [Obsolete]
        public static void WaitForSecondsToExecute(float delayInSeconds, Action onWaitFinished) {
            new Job(WaitForSeconds(delayInSeconds), toStart: true, jobCompleted: (wasKilled) => onWaitFinished());
        }

        [Obsolete]
        private static IEnumerator WaitForSeconds(float delayInSeconds) {
            yield return new WaitForSeconds(delayInSeconds);
        }

        /// <summary>
        /// Waits one FixedUpdate cycle, then executes the provided delegate.
        /// Usage:
        /// WaitOneFixedUpdateToExecute(onWaitFinished: () =&gt; {
        /// Code to execute after the wait;
        /// });
        /// Warning: This method uses a coroutine Job. Accordingly, after being called it will
        /// immediately return which means the code you have following it will execute
        /// before the code assigned to the onWaitFinished delegate.
        /// </summary>
        /// <param name="onWaitFinished">The delegate to execute once the wait is finished.</param>
        /// <returns></returns>
        [Obsolete]
        public static void WaitOneFixedUpdateToExecute(Action onWaitFinished) {
            new Job(WaitOneFixedUpdate(), toStart: true, jobCompleted: delegate {
                onWaitFinished();
            });
        }

        [Obsolete]
        private static IEnumerator WaitOneFixedUpdate() {
            yield return new WaitForFixedUpdate();
        }

        /// <summary>
        /// Waits one frame, then executes the provided delegate.
        /// <remarks>Deprecated as I can't think of a circumstance where it would be wise to use this.</remarks>
        /// Usage:
        ///     WaitOneToExecute(onWaitFinished: () =&gt; {
        ///         Code to execute after the wait;
        ///     });
        /// Warning: This method uses a coroutine Job. Accordingly, after being called it will 
        /// immediately return which means the code you have following it will execute 
        /// before the code assigned to the onWaitFinished delegate.
        /// </summary>
        /// <param name="onWaitFinished">The delegate to execute once the wait is finished.</param>
        [Obsolete]
        public static void WaitOneToExecute(Action onWaitFinished) {
            WaitForFrames(Constants.One, onWaitFinished: delegate {
                onWaitFinished();
            });
        }

        /// <summary>
        /// Waits the designated number of frames, then executes the provided delegate.
        /// <remarks>Deprecated as I can't think of a circumstance where it would be wise to use this.</remarks>
        /// Usage:
        /// WaitForFrames(framesToWait, onWaitFinished: (jobWasKilled) =&gt; {
        /// Code to execute after the wait;
        /// });
        /// Warning: This method uses a coroutine Job. Accordingly, after being called it will
        /// immediately return which means the code you have following it will execute
        /// before the code assigned to the onWaitFinished delegate.
        /// </summary>
        /// <param name="framesToWait">The frames to wait.</param>
        /// <param name="onWaitFinished">The delegate to execute once the wait is finished. The
        /// signature is onWaitFinished(jobWasKilled).</param>
        /// <returns>A reference to the Job so it can be killed before it finishes, if needed.</returns>
        [Obsolete]
        public static Job WaitForFrames(int framesToWait, Action<bool> onWaitFinished) {
            Utility.ValidateNotNegative(framesToWait);
            return new Job(WaitForFrames(framesToWait), toStart: true, jobCompleted: (wasKilled) => {
                onWaitFinished(wasKilled);
            });
        }

        /// <summary>
        /// Waits the designated number of frames. Usage:
        /// new Job(UnityUtility.WaitFrames(1), toStart: true, onJobCompletion: (jobWasKilled) =&gt; {
        /// Code to execute after the wait;
        /// });
        /// WARNING: the code in this location will execute immediately after the Job starts.
        /// </summary>
        /// <param name="framesToWait">The frames to wait.</param>
        /// <returns></returns>
        [Obsolete]
        private static IEnumerator WaitForFrames(int framesToWait) {
            int targetFrameCount = Time.frameCount + framesToWait;
            while (Time.frameCount < targetFrameCount) {
                yield return null;
            }
        }

        /// <summary>
        /// Waits the initial designated number of frames, then executes the provided delegate. Then
        /// continuously waits the repeating number of frames and executes the delegate. This continues
        /// until the returned Job is killed.
        /// <remarks>Deprecated as I can't think of a circumstance where it would be wise to use this.</remarks>
        /// Usage:
        /// WaitForFrames(initialFramesToWait, repeatingFramesToWait, methodToExecute: () =&gt; {
        /// Code to execute;
        /// });
        /// Warning: This method uses a coroutine Job. Accordingly, after being called it will
        /// immediately return which means the code you have following it will execute
        /// before the code assigned to the methodToExecute delegate.
        /// </summary>
        /// <param name="initialFramesToWait">The initial frames to wait.</param>
        /// <param name="repeatingFramesToWait">The repeating frames to wait.</param>
        /// <param name="methodToExecute">The method to execute.</param>
        /// <returns>
        /// A reference to the Job as it must be killed to stop it.
        /// </returns>
        [Obsolete]
        public static Job WaitForFrames(int initialFramesToWait, int repeatingFramesToWait, Action methodToExecute) {
            return new Job(RepeatingWaitForFrames(initialFramesToWait, repeatingFramesToWait, methodToExecute), toStart: true, jobCompleted: null);
        }

        /// <summary>
        /// Executes <c>methodToExecute</c> following a delay of <c>initialFramesToWait</c> frames, then 
        /// continuously delays <c>repeatingFramesToWait</c> frames between executions of <c>methodToExecute</c> until killed.
        /// WARNING: the code in this location will execute immediately after the Job starts.
        /// </summary>
        /// <param name="initialFramesToWait">The initial frames to wait.</param>
        /// <param name="repeatingFramesToWait">The number of frames to wait between method execution.</param>
        /// <param name="methodToExecute">The method to execute.</param>
        /// <returns></returns>
        [Obsolete]
        private static IEnumerator RepeatingWaitForFrames(int initialFramesToWait, int repeatingFramesToWait, Action methodToExecute) {
            int targetFrameCount = Time.frameCount + initialFramesToWait;
            while (Time.frameCount < targetFrameCount) {
                yield return null;
            }
            methodToExecute();

            while (true) {
                targetFrameCount = Time.frameCount + repeatingFramesToWait;
                while (Time.frameCount < targetFrameCount) {
                    yield return null;
                }
                methodToExecute();
            }
        }

        #endregion

        #region Common Animation Coroutines

        /// <summary>
        /// Waits for an animation.
        /// </summary>
        /// <param name="animation">The animation component running the animation.</param>
        /// <param name="name">The name of the animation.</param>
        /// <param name="ratio">The 0..1 duration ratio to wait for.</param>
        /// <returns>
        /// A coroutine that waits for the animation
        /// </returns>
        public static IEnumerator WaitForAnimation(Animation animation, string name, float ratio) {
            AnimationState state = animation[name];
            state.wrapMode = WrapMode.ClampForever;
            state.enabled = true;
            state.speed = state.speed == 0 ? 1 : state.speed;
            var t = state.time;
            while ((t / state.length) + float.Epsilon < ratio) {
                t += Time.deltaTime * state.speed;
                yield return null;
            }
        }

        /// <summary>
        /// Plays the named animation and waits for completion.
        /// </summary>
        /// <param name="animation">The animation component running the animation.</param>
        /// <param name="name">The name of the animation to play.</param>
        /// <returns>
        /// A coroutine that waits for the animation to finish.
        /// </returns>
        public static IEnumerator PlayAnimation(Animation animation, string name) {
            AnimationState state = animation[name];
            state.time = 0;
            state.weight = 1;
            state.speed = 1;
            state.enabled = true;
            var wait = WaitForAnimation(animation, name, 1f);
            while (wait.MoveNext())
                yield return null;
            state.weight = 0;
        }

        /// <summary>
        /// Waits for an object to reach a position.
        /// </summary>
        /// <param name="transform">The object that is moving.</param>
        /// <param name="position">The position to attain.</param>
        /// <param name="accuracy">The accuracy with which the object must reach the position
        /// in world units. Default is 1.</param>
        /// <returns>
        /// A coroutine to wait for the object
        /// </returns>
        public static IEnumerator WaitForPosition(Transform transform, Vector3 position, float accuracy = 1F) {
            var accuracySq = accuracy * accuracy;
            while ((transform.position - position).sqrMagnitude > accuracySq)
                yield return null;
        }

        /// <summary>
        /// Waits for the object to achieve a particular rotation.
        /// </summary>
        /// <param name="transform">The transform that is rotating.</param>
        /// <param name="rotation">The rotation to achieve</param>
        /// <returns>
        /// A coroutine to wait for the object
        /// </returns>
        public static IEnumerator WaitForRotation(Transform transform, Vector3 rotation) {
            return WaitForRotation(transform, rotation, Vector3.one);
        }

        /// <summary>
        /// Waits for the object to achieve a particular rotation
        /// </summary>
        /// <param name="transform">The transform that is rotating.</param>
        /// <param name="rotation">The rotation to achieve</param>
        /// <param name="mask">A Vector mask to indicate which axes are important (can also be fractional)</param>
        /// <param name="accuracy">The value to which the rotation must be within. Default is 1 degree.</param>
        /// <returns>
        /// A coroutine to wait for the object
        /// </returns>
        public static IEnumerator WaitForRotation(Transform transform, Vector3 rotation, Vector3 mask, float accuracy = 1F) {
            var accuracySq = accuracy * accuracy;
            if (accuracySq == 0) { accuracySq = float.Epsilon; }

            rotation.Scale(mask);

            while (true) {
                var currentAngles = transform.rotation.eulerAngles;
                currentAngles.Scale(mask);
                if ((currentAngles - rotation).sqrMagnitude <= accuracySq)
                    yield break;
                yield return null;
            }
        }

        /// <summary>
        /// Moves an object over a period of time
        /// </summary>
        /// <param name="objectToMove">The Transform of the object to move.</param>
        /// <param name="position">The destination position</param>
        /// <param name="time">The number of seconds that the move should take</param>
        /// <returns>
        /// A coroutine that moves the object
        /// </returns>
        public static IEnumerator MoveObject(Transform objectToMove, Vector3 position, float time) {
            return MoveObject(objectToMove, position, time, EasingType.Quadratic);
        }

        /// <summary>
        /// Moves an object over a period of time
        /// </summary>
        /// <param name="objectToMove">The Transform of the object to move.</param>
        /// <param name="position">The destination position</param>
        /// <param name="time">The number of seconds that the move should take</param>
        /// <param name="ease">The easing function to use when moving the object</param>
        /// <returns>
        /// A coroutine that moves the object
        /// </returns>
        public static IEnumerator MoveObject(Transform objectToMove, Vector3 position, float time, EasingType ease) {
            var t = 0f;
            var pos = objectToMove.position;
            while (t < 1f) {
                objectToMove.position = Vector3.Lerp(pos, position, Easing.EaseInOut(t, ease));
                t += Time.deltaTime / time;
                yield return null;
            }
            objectToMove.position = position;
        }

        #endregion

    }
}

