// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AMonoStateMachineSingleton.cs
//  Abstract Base class for Singleton MonoBehaviour State Machines.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Abstract Base class for Singleton MonoBehaviour State Machines.
/// </summary>
/// <typeparam name="T">The final derived AMonoBase Type</typeparam>
/// <typeparam name="E">Th State Type being used, typically an enum type.</typeparam>
public class AMonoStateMachineSingleton<T, E> : AMonoStateMachine<E>, IInstanceIdentity
    where T : AMonoBase
    where E : struct {

    private string _instanceID;

    public override void LogEvent() {
        System.Diagnostics.StackFrame stackFrame = new System.Diagnostics.StackTrace().GetFrame(1);
        D.Log("{0}{1}.{2}() method called.".Inject(GetType().Name, _instanceID, stackFrame.GetMethod().Name));
    }

    #region Singleton Pattern
    // NOTE: Acquiring a reference to T.Instance this way DOES NOT cause Awake() to be called when acquired. Awake() is called on its own schedule.

    protected static T _instance;
    public static T Instance {
        get {
            if (_instance == null && !_isApplicationQuiting) {
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

    /// <summary>
    /// Called when the Application is quiting, followed by OnDisable() and then OnDestroy().
    /// </summary>
    protected override void OnApplicationQuit() {
        base.OnApplicationQuit();
        _instance = null;
    }

    #region IInstanceIdentity Members

    private static int _instanceCounter = 0;
    public int InstanceID { get; private set; }

    private void IncrementInstanceCounter() {
        InstanceID = System.Threading.Interlocked.Increment(ref _instanceCounter);
        D.Log("{0}.InstanceID now set to {1}, static counter now {2}.", typeof(T).Name, InstanceID, _instanceCounter);
    }

    #endregion

}

