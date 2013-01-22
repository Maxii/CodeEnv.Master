﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MonoBehaviourBase.cs
// Abstract Base class for types that are derived from MonoBehaviour.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using CodeEnv.Master.Common;

using System.Collections;
using System.Diagnostics;

[Serializable]
/// <summary>
/// Abstract Base class for types that are derived from MonoBehaviour.
/// NOTE: Unity will never call the 'overrideable' Awake(), Start(), Update(), LateUpdate(), FixedUpdate(), OnGui(), etc. methods when 
/// there is a higher derived class in the chain. Unity only calls the method (if implemented) of the highest derived class.
/// </summary>
public abstract class MonoBehaviourBase : MonoBehaviour {

    #region Invoke
    // Based on an Action Delegate that encapsulates methods with no parameters that return void, aka 'a task'.

    /// <summary>
    /// Invokes the specified method after the specified time delay without using the error-prone method name string.
    /// </summary>
    /// <param name="task">The method to invoke encapsulated as an Action delegate. The method must be parameterless and return void.</param>
    /// <param name="time">The time delay in seconds until the method is invoked.</param>
    public void Invoke(Action task, float time) {
        Invoke(task.Method.Name, time);
    }

    /// <summary>
    /// Repeatedly invokes the specified method after the specified time delay without using the error-prone method name string. Can only be terminated 
    /// with CancelInvoke() or CancelInvoke(Task).
    /// </summary>
    /// <param name="task">The method to invoke encapsulated as a Action delegate. The method must be parameterless and return void.</param>
    /// <param name="time">The time delay in seconds until the method is invoked.</param>
    /// <param name="repeatRate">The repeat rate in seconds.</param>
    public void InvokeRepeating(Action task, float time, float repeatRate) {
        InvokeRepeating(task.Method.Name, time, repeatRate);
    }

    /// <summary>
    /// Invokes the specified method after a random time delay without using the error-prone method name string.
    /// </summary>
    /// <param name="task">The method to invoke encapsulated as a Action delegate. The method must be parameterless and return void.</param>
    /// <param name="minTime">The minimun amount of delay time.</param>
    /// <param name="maxTime">The maximum amount of delay time.</param>
    public void InvokeRandom(Action task, float minTime, float maxTime) {
        float time = UnityEngine.Random.Range(minTime, maxTime);
        Invoke(task.Method.Name, time);
    }

    /// <summary>
    /// Repeatedly invokes the specified method after a random time delay without using the error-prone method name string. Can only be terminated 
    /// with CancelInvoke() or CancelInvoke(Task).
    /// </summary>
    /// <param name="task">The method to invoke encapsulated as a Action delegate. The method must be parameterless and return void.</param>
    /// <param name="minTime">The minimun amount of delay time between invokations.</param>
    /// <param name="maxTime">The maximum amount of delay time between invokations.</param>
    /// <returns></returns>
    public IEnumerator InvokeRandomRepeating(Action task, float minTime, float maxTime) {
        while (true) {
            float time = UnityEngine.Random.Range(minTime, maxTime);
            yield return new WaitForSeconds(time);
            Invoke(task.Method.Name, 0);
        }
    }

    /// <summary>
    /// Cancels any invokation of the specified method.
    /// </summary>
    /// <param name="task">The method to cancel encapsulated as a Action delegate. The method must be parameterless and return void.</param>
    public void CancelInvoke(Action task) {
        CancelInvoke(task.Method.Name);
    }

    /// <summary>
    /// Determines whether the specified method is scheduled to be invoked.
    /// </summary>
    /// <param name="task">The method to invoke encapsulated as a Action delegate. The method must be parameterless and return void.</param>
    /// <returns>
    ///   <c>true</c> if the specified method is scheduled to be invoked; otherwise, <c>false</c>.
    /// </returns>
    public bool IsInvoking(Action task) {
        return IsInvoking(task.Method.Name);
    }
    #endregion

    /// <summary>
    /// Instantiates a clone of the original object at position and rotation.
    /// </summary>
    /// <typeparam name="T">The Type of the original to be cloned.</typeparam>
    /// <param name="original">The original.</param>
    /// <param name="position">The position.</param>
    /// <param name="rotation">The rotation.</param>
    /// <returns>A clone of original named [original.Name](Clone)</returns>
    public T Instantiate<T>(T original, Vector3 position, Quaternion rotation) where T : UnityEngine.Object {
        return (T)UnityEngine.Object.Instantiate(original, position, rotation);
    }

    /// <summary>
    /// Instantiates a clone of the original object at the original objects position and rotation.
    /// </summary>
    /// <typeparam name="T">The Type of the original to be cloned.</typeparam>
    /// <param name="original">The original.</param>
    /// <returns>A clone of original named [original.Name](Clone)</returns>

    public T Instantiate<T>(T original) where T : UnityEngine.Object {
        return (T)UnityEngine.Object.Instantiate(original);
    }

    #region Coroutine
    // Based on Func<IEnumerator> and Func<object, IEnumerator> Delegates that encapsulate methods with zero or one parameter that return IEnumerator, aka 'a task'.

    /// <summary>
    /// Starts a Coroutine executing the method contained in task.
    /// Note: Coroutines run on the object that StartCoroutine() is called on.  So if you are starting a coroutine on a different object it makes a lot of sense to use StartCoroutine 
    /// on that object, not the one you are currently executing.
    /// </summary>
    /// <param name="task">The method to run as a Coroutine encapsulated as a Func&ltIEnumerator&gt delegate. The method can have no parameters and must return IEnumerator.</param>
    /// <param name="value">A single optional value to use as the method's parameter, if any.</param>
    /// <returns>The Coroutine started.</returns>
    public Coroutine StartCoroutine(Func<IEnumerator> task) {
        return StartCoroutine(task.Method.Name);
    }

    /// <summary>
    /// Starts a Coroutine executing the method contained in task.
    /// Note: Coroutines run on the object that StartCoroutine() is called on.  So if you are starting a coroutine on a different object it makes a lot of sense to use StartCoroutine 
    /// on that object, not the one you are currently executing.
    /// </summary>
    /// <param name="task">The method to run as a Coroutine encapsulated as a Func&ltIEnumerator&gt delegate. The method must have one parameter and return IEnumerator.</param>
    /// <param name="value">A single optional value to use as the method's parameter, if any.</param>
    /// <returns>The Coroutine started.</returns>
    public Coroutine StartCoroutine(Func<object, IEnumerator> task, object value) {
        return StartCoroutine(task.Method.Name, value);
    }

    /// <summary>
    /// Stops the specific Coroutine executing the method contained in task.
    /// </summary>
    /// <param name="task">The method currently running as a Coroutine encapsulated as a Func&ltIEnumerator&gt delegate. The method can have no parameters and must return IEnumerator.</param>
    public void StopCoroutine(Func<IEnumerator> task) {
        StopCoroutine(task.Method.Name);
    }
    /// <summary>
    /// Stops the specific Coroutine executing the method contained in task.
    /// </summary>
    /// <param name="task">The method currently running as a Coroutine encapsulated as a Func&ltIEnumerator&gt delegate. The method must have one parameter and return IEnumerator.</param>
    public void StopCoroutine(Func<IEnumerator, object> task) {
        StopCoroutine(task.Method.Name);
    }
    #endregion

    /// <summary>
    /// Like GetComponent&ltT&gt(), this returns the script component that implements Interface I if the game object has one attached, null if it doesn't. 
    /// </summary>
    /// <typeparam name="I">The Interface Type.</typeparam>
    /// <returns>The script component that implements the Interface I.</returns>
    public I GetInterfaceComponent<I>() where I : class {
        return GetComponent(typeof(I)) as I;
    }

    /// <summary>
    /// Returns a list of all active loaded MonoBehaviour scripts that implement Interface I. It will return no inactive scripts.
    ///Please note that this function is very slow.
    /// </summary>
    /// <typeparam name="I">The Type of Interface.</typeparam>
    /// <returns></returns>
    public static List<I> FindObjectsOfInterface<I>() where I : class {
        MonoBehaviour[] monoBehaviours = FindObjectsOfType(typeof(MonoBehaviour)) as MonoBehaviour[];
        List<I> list = new List<I>();


        foreach (MonoBehaviour behaviour in monoBehaviours) {
            I component = behaviour.GetComponent(typeof(I)) as I;
            if (component != null) {
                list.Add(component);
            }
        }
        return list;
    }

    /// <summary>
    /// Returns a list of all active loaded objects of Type T. It will return no assets (meshes, textures, prefabs, ...) or inactive objects.
    ///Please note that this function is very slow.
    /// </summary>
    /// <typeparam name="T">The Type of Object.</typeparam>
    /// <returns></returns>
    public static T[] FindObjectsOfType<T>() where T : UnityEngine.Object {
        T[] objects = FindObjectsOfType(typeof(T)) as T[];
        return objects;
    }

    // An abstract Base has no need for a ToString() method as its derived children have one.
    // ObjectAnalyzer DOES include analysis of base classes in its scope.

}


