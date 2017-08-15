// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AEquipmentIcon.cs
// Abstract Gui 'icon' composed of an image and a name that provides methods supporting AEquipmentStats.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract Gui 'icon' composed of an image and a name that provides methods supporting AEquipmentStats.
/// <remarks>Used in Gui elements supporting the design of Units.</remarks>
/// <remarks>7.5.17 Previous implementation had a static _selectedIcon which was used to communicate between
/// EquipmentIcon and EquipmentStorageIcon, similar to the way ArenMook did it in his InventorySystem example.</remarks>
/// </summary>
public abstract class AEquipmentIcon : AGuiIcon {

    protected override void HandleIconSizeSet() {
        base.HandleIconSizeSet();
        ResizeWidgetAndAnchorIcon();
    }

    private void ResizeWidgetAndAnchorIcon() {
        IntVector2 iconDimensions = GetIconDimensions(Size);
        UIWidget topLevelWidget = GetComponent<UIWidget>();
        topLevelWidget.SetDimensions(iconDimensions.x, iconDimensions.y);
        AnchorTo(topLevelWidget);
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

    protected override void __Validate() {
        base.__Validate();
        UnityUtility.ValidateComponentPresence<BoxCollider>(gameObject);
    }

    #endregion

}

