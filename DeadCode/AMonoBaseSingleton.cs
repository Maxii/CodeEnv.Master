﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AMonoBaseSingleton.cs
// Abstract Base class for types that are derived from AMonoBehaviourBase that want to implement the Singleton pattern.
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
/// Abstract Base class for types that are derived from AMonoBehaviourBase that want to implement the Singleton pattern. 
/// Includes IInstanceIdentity functionality. Clients wishing IInstanceIdentity functionality have no obligations except to inherit from this. 
/// NOTE: Unity will never call the 'overrideable' Awake(), Start(), Update(), LateUpdate(), FixedUpdate(), OnGui(), etc. methods when 
/// there is a higher derived class in the chain. Unity only calls the method (if implemented) of the highest derived class.
/// </summary>
[Obsolete]
public abstract class AMonoBaseSingleton<T> : AMonoBase, IInstanceCount where T : AMonoBase {

    #region Singleton Pattern
    // NOTE: Acquiring a reference to T.Instance this way DOES NOT cause Awake() to be called when acquired. Awake() is called on its own schedule.

    protected static T _instance;
    public static T Instance {
        get {
            if (_instance == null) {
                if (IsApplicationQuiting) {
                    //D.Warn("Application is quiting while trying to access {0}.Instance.".Inject(typeof(T).Name));
                    return null;
                }
                // value is required for the first time, so look for it                        
                Type thisType = typeof(T);
                _instance = FindObjectOfType(thisType) as T;    // WARNING: Does not find T if another Type is also present on same gameobject
                // eg. T = DebugHud, but DebugHud gameobject also contains UILabel...
                if (_instance == null && !Application.isLoadingLevel) {
                    var stackFrame = new System.Diagnostics.StackTrace().GetFrame(2);
                    string callerIdMessage = "{0}.{1}().".Inject(stackFrame.GetMethod().DeclaringType, stackFrame.GetMethod().Name);
                    D.Error("No instance of {0} found. Is it destroyed/deactivated? Called by {1}.".Inject(thisType.Name, callerIdMessage));

                    // an instance of this singleton doesn't yet exist so create a temporary one
                    //System.Diagnostics.StackFrame stackFrame = new System.Diagnostics.StackTrace().GetFrame(2);
                    //string callerIdMessage = "{0}.{1}().".Inject(stackFrame.GetFileName(), stackFrame.GetMethod().Name);
                    //D.Warn("No instance of {0} found, so a temporary one has been created. Called by {1}.", thisType.Name, callerIdMessage);

                    //GameObject tempGO = new GameObject(thisType.Name, thisType);
                    //_instance = tempGO.GetComponent<T>();
                    //if (_instance == null) {
                    //    D.Error("Problem during the creation of {0}.", thisType.Name);
                    //}
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
        _instanceID = Constants.Underscore + InstanceCount;
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

    public int InstanceCount { get; private set; }

    #endregion

    #region Debug

    private static int _instanceCounter = 0;

    private void IncrementInstanceCounter() {
        InstanceCount = System.Threading.Interlocked.Increment(ref _instanceCounter);
        //D.Log("{0}.InstanceID now set to {1}, static counter now {2}.", typeof(T).Name, InstanceID, _instanceCounter);
    }

    private string _instanceID;

    /// <summary>
    /// Logs the method name called. WARNING:  Coroutines showup as &lt;IEnumerator.MoveNext&gt; rather than the method name
    /// </summary>
    public override void LogEvent() {
        if (DebugSettings.Instance.EnableEventLogging) {
            var stackFrame = new System.Diagnostics.StackFrame(1);
            string name = _transform.name + "(from transform)";
            Debug.Log("{0}.{1}{2}.{3}() called.".Inject(name, GetType().Name, _instanceID, stackFrame.GetMethod().Name));
        }
    }

    #endregion


}


