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
        /// Positions the provided game objects randomly inside a sphere in such a way that the meshes 
        /// are not in contact. 
        /// </summary>
        /// <param name="center">The location of the sphere's center in world space.</param>
        /// <param name="radius">The radius of the sphere in units.</param>
        /// <param name="objects">The objects to position.</param>
        /// <returns><c>true</c> if all objects were successfully positioned.</returns>
        public static bool PositionRandomWithinSphere(Vector3 center, float radius, GameObject[] objects) {
            Vector3[] localLocations = new Vector3[objects.Length];
            int iterateCount = 0;
            IList<Bounds> objectsBounds = new List<Bounds>();

            for (int i = 0; i < objects.Length; i++) {
                bool toEncapsulate = false;
                Vector3 candidateLocalLocation = UnityEngine.Random.insideUnitSphere * radius;
                Bounds goBounds = new Bounds();
                GameObject go = objects[i];
                if (GetBoundWithChildren(go.transform, ref goBounds, ref toEncapsulate)) {
                    goBounds.center = candidateLocalLocation;
                    D.Log("Bounds = {0}.", goBounds.ToString());
                    if (objectsBounds.All(b => !b.Intersects(goBounds))) {
                        objectsBounds.Add(goBounds);
                        localLocations[i] = candidateLocalLocation;
                        iterateCount = 0;
                    }
                    else {
                        i--;
                        iterateCount++;
                        if (iterateCount >= 10) {
                            D.Error("Iterate error.");
                            return false;
                        }
                    }
                }
                else {
                    D.Error("Unable to construct a Bound for {0}.", go.name);
                    return false;
                }
            }
            for (int i = 0; i < objects.Length; i++) {
                objects[i].transform.position = center + localLocations[i];
                //objects[i].transform.localPosition = localLocations[i];
            }
            return true;
        }

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
        /// values reset. WARNING: Awake() can be called immediately after Instantiation, even before
        /// being attached to a parent.
        /// </summary>
        /// <param name="parent">The object to parent too. If null, the object is instantiated without a parent.</param>
        /// <param name="prefab">The prefab to instantiate from.</param>
        /// <returns></returns>
        static public GameObject AddChild(GameObject parent, GameObject prefab) {
            GameObject clone = GameObject.Instantiate(prefab) as GameObject;
            D.Log("Instantiated {0} and parented to {1}. Awake() can preceed this!", prefab.name, parent.name);
            if (clone != null && parent != null) {
                AttachChildToParent(clone, parent);
            }
            return clone;
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

