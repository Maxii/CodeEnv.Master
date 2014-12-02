﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AMonoBehaviourBaseSingletonInstanceIdentity.cs
// Singleton. Generic MonoBehaviour Singleton with IInstanceIdentity functionality incorporated.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;

/// <summary>
/// Singleton. Generic MonoBehaviour Singleton with IInstanceIdentity functionality incorporated. Clients wishing
/// IInstanceIdentity functionality have no obligations except to inherit from this. 
/// </summary>
[Obsolete]
public abstract class AMonoBehaviourBaseSingletonInstanceIdentity<T> : AMonoBaseSingleton<T>, IInstanceCount where T : AMonoBase {

    private string _instanceID;

    protected override void LogEvent(string eventName) {
        D.Log("{0}{1}.{2}().", this.GetType().Name, _instanceID, eventName);
        //D.Log("{0}{1}.{2}(). GameObject.active = {3}.", this.GetType().Name, _instanceID, eventName, gameObject.activeInHierarchy);
    }

    #region MonoBehaviour Event Methods

    protected override void Awake() {
        IncrementInstanceCounter();
        _instanceID = Constants.Underscore + InstanceCount;
        //D.Log("{0}._instanceID string now set to {1}, InstanceID value used was {2}.", typeof(T).Name, _instanceID, InstanceID);
        base.Awake();
    }

    #endregion

    #region IInstanceIdentity Members

    private static int _instanceCounter = 0;
    public int InstanceCount { get; private set; }

    private void IncrementInstanceCounter() {
        InstanceCount = System.Threading.Interlocked.Increment(ref _instanceCounter);
        D.Log("{0}.InstanceID now set to {1}, static counter now {2}.", typeof(T).Name, InstanceCount, _instanceCounter);
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

