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
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract, generic base class for a CommandItem, an object that commands Elements.
/// </summary>
/// <typeparam name="UnitElementModelType">The Type of the derived AUnitElementModel this Command is composed of.</typeparam>
public abstract class AUnitCommandModel<UnitElementModelType> : AMortalItemModelStateMachine, ITarget where UnitElementModelType : AUnitElementModel {

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

    protected override void Start() {
        base.Start();
        Initialize();
    }

    protected abstract void Initialize();

    protected override void SubscribeToDataValueChanges() {
        base.SubscribeToDataValueChanges();
        // TODO 
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
        AssessCommandCategory();
        // TODO consider changing HQElement
    }

    private void OnSubordinateElementDeath(AMortalItemModel mortalItem) {
        D.Log("{0} acknowledging {1} has been lost.", Data.Name, mortalItem.Data.Name);
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
        if (Elements.Count > Constants.Zero) {
            if (element == HQElement) {
                // HQ Element has died
                HQElement = SelectHQElement();
                D.Log("{0} new HQElement = {1}.", Data.Name, HQElement.Data.Name);
            }
            AssessCommandCategory();
        }
        // Command knows when to die
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

    /// <summary>
    /// Checks for damage to this Command when its HQElement takes a hit.
    /// </summary>
    /// <param name="isHQElementAlive">if set to <c>true</c> [is hq element alive].</param>
    /// <returns><c>true</c> if the Command has taken damage.</returns>
    public bool __CheckForDamage(bool isHQElementAlive) {
        bool isHit = (isHQElementAlive) ? RandomExtended<bool>.SplitChance() : true;
        if (isHit) {
            OnHit(UnityEngine.Random.Range(1F, Data.MaxHitPoints + 1F));
        }
        else {
            D.Log("{0} avoided a hit.", Data.Name);
        }
        return isHit;
    }

    protected virtual UnitElementModelType SelectHQElement() {
        return Elements.MaxBy(e => e.Data.Health);
    }

    public abstract void AssessCommandCategory();

    protected override void Cleanup() {
        base.Cleanup();
        Data.Dispose();
    }

    # region StateMachine Support Methods

    private float _hitDamage;
    /// <summary>
    /// Applies the damage to the UnitCommand. Returns true 
    /// if the UnitCommand is still alive.
    /// </summary>
    /// <returns><c>true</c> if health > 0.</returns>
    protected bool ApplyDamage() {
        bool isAlive = true;
        Data.CurrentHitPoints -= _hitDamage;
        if (Data.Health <= Constants.ZeroF) {
            D.Error("{0} should never die as a result of applying damage.", Data.Name);
            isAlive = false;
        }
        _hitDamage = Constants.ZeroF;
        return isAlive;
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

