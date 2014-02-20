// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AItemModel.cs
// The abstract data-holding base class for all solid and non-solid objects in the game.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// The abstract data-holding base class for all solid and non-solid objects in the game.
/// </summary>
public abstract class AItemModel : AMonoBase, IDestinationTarget {

    private AItemData _data;
    /// <summary>
    /// Gets or sets the data for this item. Clients are responsible for setting in the right sequence as 
    /// one data can be dependant on another data.
    /// </summary>
    public AItemData Data {
        get { return _data; }
        set {
            if (_data == value) { return; }
            _data = value;
            OnDataChanged();
        }
    }

    protected override void Awake() {
        base.Awake();
        enabled = false;
    }

    protected virtual void OnDataChanged() {
        Data.Transform = _transform;
        SubscribeToDataValueChanges();
    }

    /// <summary>
    /// Placeholder for subscribing to changes to values contained in Data. 
    /// Does nothing.
    /// </summary>
    protected virtual void SubscribeToDataValueChanges() { }

    #region IDestinationTarget Members

    public string Name { get { return Data.Name; } }

    public Vector3 Position { get { return Data.Position; } }

    /// <summary>
    /// The radius in units of the conceptual 'globe' that encompasses this Item.
    /// </summary>
    public float Radius { get; set; }

    public virtual bool IsMovable { get { return false; } }

    #endregion
}

