// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUnitElementModel.cs
// Abstract base class for an Element, an object that is under the command of a CommandItem.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract base class for an Element, an object that is under the command of a CommandItem.
/// </summary>
public abstract class AUnitElementModel : AMortalItemModelStateMachine, ITarget {

    public event Action onStartShow;
    public event Action onStopShow;

    public virtual bool IsHQElement { get; set; }

    public new AElementData Data {
        get { return base.Data as AElementData; }
        set { base.Data = value; }
    }

    private Rigidbody _rigidbody;

    protected override void Awake() {
        base.Awake();
        _rigidbody = UnityUtility.ValidateComponentPresence<Rigidbody>(gameObject);
        // derived classes should call Subscribe() after they have acquired needed references
    }

    protected override void Start() {
        base.Start();
        Initialize();
    }

    protected abstract void Initialize();

    protected override void OnDataChanged() {
        base.OnDataChanged();
        _rigidbody.mass = Data.Mass;
    }

    public void __SimulateAttacked() {
        if (!DebugSettings.Instance.MakePlayerInvincible) {
            OnHit(UnityEngine.Random.Range(Constants.ZeroF, Data.MaxHitPoints + 1F));
        }
    }

    # region StateMachine Support Methods

    /// <summary>
    /// Applies the damage to the Element. Returns true 
    /// if the Element survived the hit.
    /// </summary>
    /// <returns><c>true</c> if the Element survived.</returns>
    protected bool ApplyDamage(float damage) {
        Data.CurrentHitPoints -= damage;
        return Data.Health > Constants.ZeroF;
    }

    protected void OnStartShow() {
        var temp = onStartShow;
        if (temp != null) {
            onStartShow();
        }
    }

    protected void OnStopShow() {
        var temp = onStopShow;
        if (temp != null) {
            onStopShow();
        }
    }

    protected IEnumerator DelayedDestroy(float delayInSeconds) {
        D.Log("{0}.DelayedDestroy({1}).", Data.Name, delayInSeconds);
        yield return new WaitForSeconds(delayInSeconds);
        D.Log("{0} GameObject being destroyed.", Data.Name);
        Destroy(gameObject);
    }

    protected void Dead_ExitState() {
        LogEvent();
        D.Error("{0}.Dead_ExitState should not occur.", Data.Name);
    }

    #endregion

    # region StateMachine Callbacks

    public void OnShowCompletion() {
        RelayToCurrentState();
    }

    protected abstract void OnHit(float damage);

    void OnDetectedEnemy() {  // TODO connect to sensors when I get them
        RelayToCurrentState();
    }

    #endregion

    // subscriptions contained completely within this gameobject (both subscriber
    // and subscribee) donot have to be cleaned up as all instances are destroyed

    #region ITarget Members

    public string Name {
        get { return Data.Name; }
    }

    public Vector3 Position {
        get { return Data.Position; }
    }

    public virtual bool IsMovable { get { return true; } }

    #endregion

}

