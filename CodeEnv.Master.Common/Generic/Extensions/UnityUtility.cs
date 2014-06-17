﻿// --------------------------------------------------------------------------------------------------------------------
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
    using System.Linq;
    using System.Collections.Generic;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;
    using System.Collections;

    public static class UnityUtility {

        private static Vector3EqualityComparer _vector3EqualityComparer;
        public static Vector3EqualityComparer Vector3EqualityComparer {
            get {
                if (_vector3EqualityComparer == null) {
                    _vector3EqualityComparer = new Vector3EqualityComparer();
                }
                return _vector3EqualityComparer;
            }
        }

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
        /// <param name="toEncapsulate">Used to determine if the bounds of the provided bounding box should affect the resulting bounding box.
        /// WARNING: this value can be changed by the method, so if iterating with this method, make sure it is reset to the value desired each time the
        /// method is used. </param>
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
                if (points[i].x < min.x) min.x = points[i].x;
                else if (points[i].x > max.x) max.x = points[i].x;
                if (points[i].y < min.y) min.y = points[i].y;
                else if (points[i].y > max.y) max.y = points[i].y;
                if (points[i].z < min.z) min.z = points[i].z;
                else if (points[i].z > max.z) max.z = points[i].z;
            }

            bounds.min = min;
            bounds.max = max;
            return bounds;
        }

        // Moved PositionRandomWithinSphere() to AUnitCommandModel as it isn't really useful outside of that scope

        /// <summary>
        /// Attaches the child to the parent, automatically aligning position,
        /// rotation, scale and layer to that of the parent, aka local position,
        /// rotation and scale are set to identity values.
        /// </summary>
        /// <param name="child">The child.</param>
        /// <param name="parent">The parent.</param>
        public static void AttachChildToParent(GameObject child, GameObject parent) {
            Transform childTransform = child.transform;
            childTransform.parent = parent.transform;
            childTransform.localPosition = Vector3.zero;
            childTransform.localRotation = Quaternion.identity;
            childTransform.localScale = Vector3.one;
            child.layer = parent.layer;
        }

        /// <summary>
        /// Instantiates an object and adds it to the specified parent with all local
        /// values reset. Removes Clone from name. WARNING: Awake() can be called immediately after Instantiation, even before
        /// being attached to a parent.
        /// </summary>
        /// <param name="parent">The object to parent too. If null, the object is instantiated without a parent.</param>
        /// <param name="prefab">The prefab to instantiate from.</param>
        /// <returns></returns>
        static public GameObject AddChild(GameObject parent, GameObject prefab) {
            GameObject clone = GameObject.Instantiate(prefab) as GameObject;
            clone.name = prefab.name;
            if (clone != null && parent != null) {
                //D.Log("Instantiated {0} and parented to {1}. Awake() can preceed this!", prefab.name, parent.name);
                AttachChildToParent(clone, parent);
            }
            return clone;
        }

        /// <summary>
        /// Calculates the location in world space of 8 vertices of a box surrounding a point.
        /// The minimum distance from this 'center' point to any side of the box is distance.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="distance">The minimum distance to the side of the box.</param>
        /// <returns></returns>
        public static IList<Vector3> CalcBoxVerticesAroundPoint(Vector3 point, float distance) {
            IList<Vector3> vertices = new List<Vector3>(8);
            var xPair = new float[2] { point.x - distance, point.x + distance };
            var yPair = new float[2] { point.y - distance, point.y + distance };
            var zPair = new float[2] { point.z - distance, point.z + distance };
            foreach (var x in xPair) {
                foreach (var y in yPair) {
                    foreach (var z in zPair) {
                        Vector3 gridBoxVertex = new Vector3(x, y, z);
                        vertices.Add(gridBoxVertex);
                    }
                }
            }
            return vertices;
        }

        /// <summary>
        /// Calculates the vertices of an inscribed box inside a sphere with 
        /// the provided radius and center point.
        /// </summary>
        /// <param name="center">The center.</param>
        /// <param name="radius">The radius.</param>
        /// <returns></returns>
        public static IList<Vector3> CalcVerticesOfInscribedBoxInsideSphere(Vector3 center, float radius) {
            IList<Vector3> vertices = new List<Vector3>(8);
            IList<Vector3> normalizedVertices = Constants.NormalizedBoxVertices;
            foreach (var normalizedVertex in normalizedVertices) {
                vertices.Add(center + normalizedVertex * radius);
            }
            //D.Log("Center = {0}, Radius = {1}, Vertices = {2}.", center, radius, vertices.Concatenate());
            return vertices;
        }

        /// <summary>
        /// Finds the closest location on the surface of a sphere to the provided point.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="sphereCenter">The sphere center.</param>
        /// <param name="sphereRadius">The sphere radius.</param>
        /// <returns></returns>
        public static Vector3 FindClosestPointOnSphereSurfaceTo(Vector3 point, Vector3 sphereCenter, float sphereRadius) {
            return sphereCenter + (point - sphereCenter).normalized * sphereRadius;
        }

        /// <summary>
        /// Rounds each value in this Vector3 to the float equivalent of the closest integer.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns></returns>
        public static Vector3 RoundPoint(Vector3 point) {
            return RoundPoint(point, Vector3.one);
        }

        /// <summary>
        /// Rounds each value in this Vector3 to the float equivalent of the closest integer
        /// multiple of multi.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="multi">The multi.</param>
        /// <returns></returns>
        public static Vector3 RoundPoint(Vector3 point, Vector3 multi) {
            for (int i = 0; i < 3; i++) {
                point[i] = Utility.RoundMultiple(point[i], multi[i]);
            }
            return point;
        }

        /// <summary>
        /// Derives the average of the supplied vectors.
        /// </summary>
        /// <param name="vectors">The vectors.</param>
        /// <returns></returns>
        public static Vector3 Mean(IEnumerable<Vector3> vectors) {
            int length = vectors.Count();
            if (length == Constants.Zero) {
                return Vector3.zero;
            }
            float x = Constants.ZeroF, y = Constants.ZeroF, z = Constants.ZeroF;
            foreach (var v in vectors) {
                x += v.x;
                y += v.y;
                z += v.z;
            }
            return new Vector3(x / length, y / length, z / length);
        }


        /// <summary>
        /// Waits one frame, then executes the provided delegate.
        /// Usage:
        ///     WaitThenExecute(onWaitFinished: (jobWasKilled) =&gt; {
        ///         Code to execute after the wait;
        ///     });
        /// Warning: This method uses a coroutine Job. Accordingly, after being called it will 
        /// immediately return which means the code you have following it will execute 
        /// before the code assigned to the onWaitFinished delegate.
        /// </summary>
        /// <param name="onWaitFinished">The delegate to execute once the wait is finished. The 
        /// signature is onWaitFinished(jobWasKilled).</param>
        public static void WaitOneToExecute(Action<bool> onWaitFinished) {
            WaitForFrames(Constants.One, onWaitFinished);
        }

        /// <summary>
        /// Waits the designated number of frames, then executes the provided delegate.
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
        /// <returns>A reference to the WaitJob so it can be killed before it finishes, if needed.</returns>
        public static WaitJob WaitForFrames(int framesToWait, Action<bool> onWaitFinished) {
            return new WaitJob(WaitForFrames(framesToWait), toStart: true, onJobComplete: onWaitFinished);
        }

        /// <summary>
        /// Waits the designated number of frames. Usage:
        /// new Job(UnityUtility.WaitFrames(1), toStart: true, onJobCompletion: (jobWasKilled) =&gt; {
        /// Code to execute after the wait;
        /// });
        /// WARNING: the code in this location will execute immediately after the Job starts
        /// </summary>
        /// <param name="framesToWait">The frames to wait.</param>
        /// <returns></returns>
        private static IEnumerator WaitForFrames(int framesToWait) {
            D.Assert(framesToWait > Constants.Zero);
            int targetFrameCount = Time.frameCount + framesToWait;
            while (Time.frameCount < targetFrameCount) {
                yield return null;
            }
        }

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

