// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AOwnedItemModel.cs
// Abstract base class for an ItemModel that can have an owner.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Abstract base class for an ItemModel that can have an owner.
/// </summary>
public abstract class AOwnedItemModel : AItemModel, IOwnedTarget {

    public new AOwnedItemData Data {
        get { return base.Data as AOwnedItemData; }
        set { base.Data = value; }
    }

    protected override void SubscribeToDataValueChanges() {
        base.SubscribeToDataValueChanges();
        _subscribers.Add(Data.SubscribeToPropertyChanged<AOwnedItemData, IPlayer>(d => d.Owner, OnOwnerChanged));
    }

    protected virtual void OnOwnerChanged() {
        if (onOwnerChanged != null) {
            onOwnerChanged(this);
        }
    }

    #region IOwnedTarget Members

    public event Action<IOwnedTarget> onOwnerChanged;

    public IPlayer Owner { get { return Data.Owner; } }

    #endregion

}

