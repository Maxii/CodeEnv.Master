// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ACommandItem.cs
// Abstract, generic base class for a CommandItem. A CommandItem is an object that commands Elements.
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
/// Abstract, generic base class for a CommandItem. A CommandItem is an object that commands Elements.
/// </summary>
/// <typeparam name="ElementType">The Type of Element used under this Command.</typeparam>
/// <typeparam name="ElementCategoryType">The Type that defines the possible sub-categories of an Element, eg. a ShipItem can be sub-categorized as a Frigate which is defined within the ShipCategory Type.</typeparam>
/// <typeparam name="ElementDataType">The type of Data associated with the ElementType used under this Command.</typeparam>
/// <typeparam name="ElementStateType">The State Type being used by the ElementType's StateMachine.</typeparam>
/// <typeparam name="CommandDataType">The Type of Data associated with this Command.</typeparam>
/// <typeparam name="CommandCompositionType">The Type of the Composition component of this Command.</typeparam>
/// <typeparam name="CommandStateType">The State Type being used by this CommandItem's StateMachine.</typeparam>
public abstract class ACommandItem<ElementType, ElementCategoryType, ElementDataType, ElementStateType, CommandDataType, CommandCompositionType, CommandStateType> : AMortalItemStateMachine<CommandStateType>, ITarget
    where ElementType : AElement<ElementCategoryType, ElementDataType, ElementStateType>
    where ElementCategoryType : struct
    where ElementDataType : AElementData<ElementCategoryType>
    where ElementStateType : struct
    where CommandDataType : ACommandData<ElementCategoryType, ElementDataType, CommandCompositionType>
    where CommandCompositionType : AComposition<ElementCategoryType, ElementDataType>
    where CommandStateType : struct {

    public event Action<ElementType> onElementDestroyed;

    public string PieceName { get { return Data.OptionalParentName; } }

    public new CommandDataType Data {
        get { return base.Data as CommandDataType; }
        set { base.Data = value; }
    }

    private ElementType _hqElement;
    public ElementType HQElement {
        get { return _hqElement; }
        set { SetProperty<ElementType>(ref _hqElement, value, "HQElement", OnHQElementChanged, OnHQElementChanging); }
    }

    public IList<ElementType> Elements { get; private set; }

    protected override void Awake() {
        base.Awake();
        Elements = new List<ElementType>();
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
    public void AddElement(ElementType element) {
        Elements.Add(element);
        Data.AddElement(element.Data);
        Transform parentTransform = _transform.parent;
        if (element.transform.parent != parentTransform) {
            element.transform.parent = parentTransform;   // local position, rotation and scale are auto adjusted to keep ship unchanged in worldspace
        }
        // TODO consider changing flagship
    }

    public void ReportElementLost(ElementType element) {
        D.Log("{0} acknowledging {1} has been lost.", Data.Name, element.Data.Name);
        RemoveElement(element);

        var temp = onElementDestroyed;
        if (temp != null) {
            temp(element);
        }
    }

    public void RemoveElement(ElementType element) {
        bool isRemoved = Elements.Remove(element);
        isRemoved = isRemoved && Data.RemoveElement(element.Data);
        D.Assert(isRemoved, "{0} not found.".Inject(element.Data.Name));
        if (Elements.Count > Constants.Zero) {
            if (element == HQElement) {
                // HQ Element has died
                HQElement = SelectHQElement();
            }
            return;
        }
        // Fleet knows when to die
    }

    protected virtual void OnHQElementChanging(ElementType newElement) {
        if (HQElement != null) {
            HQElement.IsHQElement = false;
        }
    }

    protected virtual void OnHQElementChanged() {
        HQElement.IsHQElement = true;
        Data.HQElementData = HQElement.Data;
    }

    protected virtual ElementType SelectHQElement() {
        return Elements.MaxBy(e => e.Data.Health);
    }

    protected override void Cleanup() {
        base.Cleanup();
        Data.Dispose();
    }

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

