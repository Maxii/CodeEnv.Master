// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AItemDataForm.cs
// Abstract base class for Forms that are fed content from and can make changes to ItemData.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Abstract base class for Forms that are fed content from and can make changes to ItemData.
/// </summary>
public abstract class AItemDataForm : AInfoChangeForm {

    private AItemData _itemData;
    public AItemData ItemData {
        get { return _itemData; }
        set {
            D.AssertNull(_itemData);  // occurs only once between Resets
            SetProperty<AItemData>(ref _itemData, value, "ItemData");
        }
    }

    public override void PopulateValues() {
        D.AssertNotNull(ItemData);
        AssignValuesToMembers();
    }

    protected override void ResetForReuse_Internal() {
        base.ResetForReuse_Internal();
        _itemData = null;
    }

}

