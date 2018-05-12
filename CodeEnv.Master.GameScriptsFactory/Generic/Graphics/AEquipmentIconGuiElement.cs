// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AEquipmentIconGuiElement.cs
// Abstract AMultiSizeIconGuiElement that represents an EquipmentStat.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract AMultiSizeIconGuiElement that represents an EquipmentStat.
/// <remarks>Used in screens supporting the design of Units.</remarks>
/// <remarks>7.5.17 Previous implementation had a static _selectedIcon which was used to communicate between
/// EquipmentIconGuiElement and EquipmentStorageIconGuiElement, similar to the way ArenMook did it in his InventorySystem example.</remarks>
/// </summary>
public abstract class AEquipmentIconGuiElement : AMultiSizeIconGuiElement {

    public override GuiElementID ElementID { get { return GuiElementID.EquipmentIcon; } }

    protected override UISprite AcquireIconImageSprite() {
        return _topLevelIconWidget.gameObject.GetSingleComponentInImmediateChildren<UISprite>();
    }

    /// <summary>
    /// Set the cursor to the image of the provided stat, 'simulating' a drag of the stat's icon.
    /// </summary>
    protected void UpdateCursor(AEquipmentStat stat) {
        if (stat != null) {
            MyCustomCursor.Set(stat.ImageAtlasID, stat.ImageFilename, width: 32, height: 29);
        }
        else {
            MyCustomCursor.Clear();
        }
    }


    #region Debug

    #endregion

}

