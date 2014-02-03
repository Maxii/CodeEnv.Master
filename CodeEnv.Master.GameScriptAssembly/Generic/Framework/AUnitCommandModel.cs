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

    /// <summary>
    /// Adds the Element to this Command including parenting if needed.
    /// </summary>
    /// <param name="element">The Element to add.</param>
    public void AddElement(UnitElementModelType element) {
        Elements.Add(element);
        Data.AddElement(element.Data);
        Transform parentTransform = _transform.parent;
        if (element.transform.parent != parentTransform) {
            element.transform.parent = parentTransform;   // local position, rotation and scale are auto adjusted to keep ship unchanged in worldspace
        }
        AssessCommandCategory();
        // TODO consider changing HQElement
    }

    public void OnSubordinateElementDeath(UnitElementModelType element) {
        D.Log("{0} acknowledging {1} has been lost.", Data.Name, element.Data.Name);
        RemoveElement(element);

        var temp = onSubordinateElementDeath;
        if (temp != null) {
            temp(element);
        }
    }

    public void RemoveElement(UnitElementModelType element) {
        bool isRemoved = Elements.Remove(element);
        isRemoved = isRemoved && Data.RemoveElement(element.Data);
        D.Assert(isRemoved, "{0} not found.".Inject(element.Data.Name));
        if (Elements.Count > Constants.Zero) {
            if (element == HQElement) {
                // HQ Element has died
                HQElement = SelectHQElement();
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

    protected virtual UnitElementModelType SelectHQElement() {
        return Elements.MaxBy(e => e.Data.Health);
    }

    public abstract void AssessCommandCategory();

    protected override void Cleanup() {
        base.Cleanup();
        Data.Dispose();
    }

    # region StateMachine Callbacks

    public void OnShowCompletion() {
        RelayToCurrentState();
    }

    void OnHit(float damage) {  // allows commands to optionally take and show special hits
        RelayToCurrentState(damage);    // IMPROVE add Action delegate to RelayToCurrentState
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

