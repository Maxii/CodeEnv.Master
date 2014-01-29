// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AElement.cs
// Abstract base class for an Element, an object that is under the command of a CommandItem.
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
/// Abstract base class for an Element, an object that is under the command of a CommandItem.
/// </summary>
public abstract class AElement : AMortalItemStateMachine, ITarget {

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

