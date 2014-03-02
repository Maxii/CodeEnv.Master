// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUnitCommandModel.cs
//  Abstract, generic base class for a CommandItem, an object that commands Elements.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract, generic base class for a CommandItem, an object that commands Elements.
/// </summary>
/// <typeparam name="UnitElementModelType">The Type of the derived AUnitElementModel this Command is composed of.</typeparam>
public abstract class AUnitCommandModel<UnitElementModelType> : AMortalItemModelStateMachine, ICmdTarget where UnitElementModelType : AUnitElementModel {

    public event Action<UnitElementModelType> onSubordinateElementDeath;

    public string PieceName { get { return Data.OptionalParentName; } }

    public new ACommandData Data {
        get { return base.Data as ACommandData; }
        set { base.Data = value; }
    }

    private UnitElementModelType _hqElement;
    public UnitElementModelType HQElement {
        get { return _hqElement; }
        set { SetProperty<UnitElementModelType>(ref _hqElement, value, "HQElement", OnHQElementChanged, OnHQElementChanging); }
    }

    // can't get rid of generic ElementType since List Properties can't be hidden
    public IList<UnitElementModelType> Elements { get; set; }

    protected override void Awake() {
        base.Awake();
        Elements = new List<UnitElementModelType>();
        // Derived class should call Subscribe() after all used references have been established
    }

    /// <summary>
    /// Adds the Element to this Command including parenting if needed.
    /// </summary>
    /// <param name="element">The Element to add.</param>
    public void AddElement(UnitElementModelType element) {
        element.onItemDeath += OnSubordinateElementDeath;
        Elements.Add(element);
        Data.AddElement(element.Data);
        Transform parentTransform = _transform.parent;
        if (element.transform.parent != parentTransform) {
            element.transform.parent = parentTransform;   // local position, rotation and scale are auto adjusted to keep ship unchanged in worldspace
        }
        // TODO consider changing HQElement
    }

    private void OnSubordinateElementDeath(ITarget mortalItem) {
        D.Log("{0} acknowledging {1} has been lost.", Data.Name, mortalItem.Name);
        UnitElementModelType element = mortalItem as UnitElementModelType;
        RemoveElement(element);

        var temp = onSubordinateElementDeath;
        if (temp != null) {
            temp(element);
        }
    }

    public void RemoveElement(UnitElementModelType element) {
        element.onItemDeath -= OnSubordinateElementDeath;
        bool isRemoved = Elements.Remove(element);
        isRemoved = isRemoved && Data.RemoveElement(element.Data);
        D.Assert(isRemoved, "{0} not found.".Inject(element.Data.Name));
        if (Elements.Count <= Constants.Zero) {
            D.Assert(Data.UnitHealth <= Constants.ZeroF, "{0} UnitHealth error.".Inject(Data.Name));
            KillCommand();
            return;
        }
        if (element == HQElement) {
            // HQ Element has died
            HQElement = SelectHQElement();
            D.Log("{0} new HQElement = {1}.", Data.Name, HQElement.Data.Name);
        }
    }

    protected virtual void OnHQElementChanging(UnitElementModelType newElement) {
        if (HQElement != null) {
            HQElement.IsHQElement = false;
        }
    }

    protected virtual void OnHQElementChanged() {
        HQElement.IsHQElement = true;
        Data.HQElementData = HQElement.Data;
    }

    public override void __SimulateAttacked() {
        Elements.ForAll<UnitElementModelType>(e => e.__SimulateAttacked());
    }

    /// <summary>
    /// Checks for damage to this Command when its HQElement takes a hit.
    /// </summary>
    /// <param name="isHQElementAlive">if set to <c>true</c> [is hq element alive].</param>
    /// <returns><c>true</c> if the Command has taken damage.</returns>
    public bool __CheckForDamage(bool isHQElementAlive) {
        bool isHit = (isHQElementAlive) ? RandomExtended<bool>.SplitChance() : true;
        if (isHit) {
            TakeDamage(UnityEngine.Random.Range(1F, Data.MaxHitPoints + 1F));
        }
        else {
            D.Log("{0} avoided a hit.", Data.Name);
        }
        return isHit;
    }

    protected abstract void KillCommand();

    protected virtual UnitElementModelType SelectHQElement() {
        return Elements.MaxBy(e => e.Data.Health);
    }

    protected override void Cleanup() {
        base.Cleanup();
        Data.Dispose();
    }

    # region StateMachine Support Methods

    protected void Dead_ExitState() {
        LogEvent();
        D.Error("{0}.Dead_ExitState should not occur.", Data.Name);
    }

    #endregion

    # region StateMachine Callbacks

    public override void OnShowCompletion() {
        RelayToCurrentState();
    }

    protected void OnTargetDeath(ITarget deadTarget) {
        //LogEvent();
        RelayToCurrentState(deadTarget);
    }

    void OnDetectedEnemy() {  // TODO connect to sensors when I get them
        RelayToCurrentState();
    }

    #endregion

    // subscriptions contained completely within this gameobject (both subscriber
    // and subscribee) donot have to be cleaned up as all instances are destroyed

    #region ITarget Members

    public override void TakeDamage(float damage) {
        bool isCmdAlive = ApplyDamage(damage);
        D.Assert(isCmdAlive, "{0} should never die as a result of being hit.".Inject(Data.Name));
    }

    public override float MaxWeaponsRange { get { return Data.UnitWeaponsRange; } }

    #endregion

    #region ICmdTarget Members

    public IEnumerable<ITarget> ElementTargets {
        get { return Elements.Cast<ITarget>(); }
    }

    #endregion

}

