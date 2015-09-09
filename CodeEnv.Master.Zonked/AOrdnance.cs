// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Microsoft
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AOrdnance.cs
// Abstract base class for Beam, Missile and Projectile Ordnance.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract base class for Beam, Missile and Projectile Ordnance.
/// </summary>
[Obsolete]
public abstract class AOrdnance : AMonoBase, IOrdnance {

    private static int __instanceCount = 1;

    public event Action<IOrdnance> onDeathOneShot;

    public string Name { get; protected set; }

    public WDVCategory ArmamentCategory { get { return _weapon.ArmamDeliveryVehicleCategory

    public IElementAttackableTarget Target { get; private set; }

    private bool _toShowEffects;
    public bool ToShowEffects {
        get { return _toShowEffects; }
        set { SetProperty<bool>(ref _toShowEffects, value, "ToShowEffects", OnToShowEffectsChanged); }
    }

    protected CombatStrength Strength { get { return _weapon.Strength; } }

    protected float _range;
    protected AWeapon _weapon;
    protected GameManager _gameMgr;
    protected GameTime _gameTime;

    private int __instanceID;

    protected override void Awake() {
        base.Awake();
        _gameMgr = GameManager.Instance;
        _gameTime = GameTime.Instance;
        __instanceID = __instanceCount;
        __instanceCount++;
        Name = _transform.name + __instanceID;
    }

    public virtual void Initiate(IElementAttackableTarget target, AWeapon weapon, bool toShowEffects) {
        Target = target;
        _weapon = weapon;

        var tgtBearing = (target.Position - _transform.position).normalized;
        _transform.rotation = Quaternion.LookRotation(tgtBearing); // point ordnance in direction of target so _transform.forward is bearing

        var owner = weapon.RangeMonitor.ParentElement.Owner;
        _range = weapon.RangeCategory.GetWeaponRange(owner);

        ToShowEffects = toShowEffects;
    }

    protected abstract void OnToShowEffectsChanged();

    public void Terminate() {
        PrepareForTermination();
        if (onDeathOneShot != null) {
            onDeathOneShot(this);
            onDeathOneShot = null;
        }
        Destroy(gameObject);
    }

    /// <summary>
    /// Called when Terminate() is called, this is a derived class'
    /// opportunity to do any cleanup (stop audio, etc.) prior to the
    /// gameObject being destroyed.
    /// </summary>
    protected virtual void PrepareForTermination() { }



    #region Nested Classes

    public enum EffectControl {
        None,
        Show,
        Hide,
        Pause,
        Resume
    }

    #endregion


}

