// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Item.cs
// The abstract data-holding base class for all solid and non-solid objects in the game.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// The abstract data-holding base class for all solid and non-solid objects in the game.
/// </summary>
public abstract class AItem : AMonoBase {

    private AData _data;
    /// <summary>
    /// Gets or sets the data for this item. Clients are responsible for setting in the right sequence as 
    /// one data can be dependant on another data.
    /// </summary>
    public AData Data {
        get { return _data; }
        set { SetProperty<AData>(ref _data, value, "Data", OnDataChanged, OnDataChanging); }
    }

    /// <summary>
    /// The radius in units of the conceptual 'globe' that encompasses this Item.
    /// </summary>
    public float Radius { get; set; }

    protected override void Awake() {
        base.Awake();
        enabled = false;
    }

    private void OnDataChanging(AData data) {
        data.Transform = _transform; // assign our transform to Data
    }

    protected virtual void OnDataChanged() {
        SubscribeToDataValueChanges();
    }

    /// <summary>
    /// Placeholder for subscribing to changes to values contained in Data. 
    /// All derived classes must implement this, even if it does nothing.
    /// </summary>
    protected abstract void SubscribeToDataValueChanges();

}

