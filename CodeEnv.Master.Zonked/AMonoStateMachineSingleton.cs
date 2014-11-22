// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AMonoStateMachineSingleton.cs
//  Abstract Base class for Singleton MonoBehaviour State Machines.This version supports
//  subscription to State Changes, but does not support Call() or Return().
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Abstract Base class for Singleton MonoBehaviour State Machines.
/// WARNING: This version supports subscription to State Changes, but does not 
///  support Call() or Return() as not all state changes will be notified if they are 
///  used as they make state changes without going through SetProperty.
/// </summary>
/// <typeparam name="T">The final derived AMonoBase Type</typeparam>
/// <typeparam name="E">Th State Type being used, typically an enum type.</typeparam>
[Obsolete]
public abstract class AMonoStateMachineSingleton<T, E> : AMonoStateMachine_NoCall<E>, IInstanceIdentity
    where T : AMonoBase
    where E : struct {

    private static int _instanceCounter = 0;
    private string _instanceID;

    #region Singleton Pattern
    // NOTE: Acquiring a reference to T.Instance this way DOES NOT cause Awake() to be called when acquired. Awake() is called on its own schedule.

    protected static T _instance;
    public static T Instance {
        get {
            if (_instance == null && !IsApplicationQuiting) {
                //System.Diagnostics.StackFrame stackFrame_ = new System.Diagnostics.StackTrace().GetFrame(1);
                //D.Log("{0}.{1}() method called.".Inject(typeof(T).Name, stackFrame_.GetMethod().Name));

                // value is required for the first time, so look for it                        
                Type thisType = typeof(T);
                _instance = FindObjectOfType(thisType) as T;
                if (_instance == null) {
                    // an instance of this singleton doesn't yet exist so create a temporary one
                    System.Diagnostics.StackFrame stackFrame = new System.Diagnostics.StackTrace().GetFrame(2);
                    string callerIdMessage = " Called by {0}.{1}().".Inject(stackFrame.GetFileName(), stackFrame.GetMethod().Name);
                    D.Warn("No instance of {0} found, so a temporary one has been created. Called by {1}.", thisType.Name, callerIdMessage);

                    GameObject tempGO = new GameObject(thisType.Name, thisType);
                    _instance = tempGO.GetComponent<T>();
                    if (_instance == null) {
                        D.Error("Problem during the creation of {0}.", thisType.Name);
                    }
                }
                //D.Log("{0}.Instance found.", typeof(T).Name);
            }
            return _instance;
        }
    }

    #endregion

    #region MonoBehaviour Event Methods

    protected override void Awake() {
        IncrementInstanceCounter();
        _instanceID = Constants.Underscore + InstanceID;
        //D.Log("{0}._instanceID string now set to {1}, InstanceID value used was {2}.", typeof(T).Name, _instanceID, InstanceID);
        base.Awake();
    }

    #endregion

    private void IncrementInstanceCounter() {
        InstanceID = System.Threading.Interlocked.Increment(ref _instanceCounter);
        //D.Log("{0}.InstanceID now set to {1}, static counter now {2}.", typeof(T).Name, InstanceID, _instanceCounter);
    }

    /// <summary>
    /// Called when the Application is quiting, followed by OnDisable() and then OnDestroy().
    /// </summary>
    protected override void OnApplicationQuit() {
        base.OnApplicationQuit();
        _instance = null;
    }

    #region Debug

    public override void LogEvent() {
        // NOTE:  Coroutines don't show the right method name when logged using stacktrace
        var stackFrame = new System.Diagnostics.StackFrame(1);
        D.Log("{0}{1}.{2}() called.", GetType().Name, _instanceID, stackFrame.GetMethod().Name);
    }

    #endregion

    #region IInstanceIdentity Members

    public int InstanceID { get; private set; }

    #endregion

}

