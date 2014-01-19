// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AElement.cs
// Abstract, generic base class for an Element. An Element is an object that is under the command of a CommandItem.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System.Collections;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract, generic base class for an Element. An Element is an object that is under the command of a CommandItem.
/// </summary>
/// <typeparam name="ElementCategoryType">The Type that defines the possible sub-categories of an Element, eg. a ShipItem can be sub-categorized as a Frigate which is defined within the ShipCategory Type.</typeparam>
/// <typeparam name="ElementDataType">The type of Data associated with the ElementType used under this Command.</typeparam>
/// <typeparam name="ElementStateType">The State Type being used by this Element's StateMachine.</typeparam>
public abstract class AElement<ElementCategoryType, ElementDataType, ElementStateType> : AMortalItemStateMachine<ElementStateType>, ITarget
    where ElementCategoryType : struct
    where ElementDataType : AElementData<ElementCategoryType>
    where ElementStateType : struct {

    public bool IsHQElement { get; set; }

    public new ElementDataType Data {
        get { return base.Data as ElementDataType; }
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

