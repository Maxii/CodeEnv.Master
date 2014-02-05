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

    public bool IsHQElement { get; set; }

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

    private float _hitDamage;
    /// <summary>
    /// Applies the damage to the Element. Returns true 
    /// if the Element survived the hit.
    /// </summary>
    /// <returns><c>true</c> if the Element survived.</returns>
    protected bool ApplyDamage() {
        bool isAlive = true;
        Data.CurrentHitPoints -= _hitDamage;
        if (Data.Health <= Constants.ZeroF) {
            isAlive = false;
        }
        _hitDamage = Constants.ZeroF;
        return isAlive;
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

    #endregion

    # region StateMachine Callbacks

    public void OnShowCompletion() {
        RelayToCurrentState();
    }

    void OnHit(float damage) {
        _hitDamage = damage;
        OnHit();
    }

    void OnHit() {
        RelayToCurrentState();
    }

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

