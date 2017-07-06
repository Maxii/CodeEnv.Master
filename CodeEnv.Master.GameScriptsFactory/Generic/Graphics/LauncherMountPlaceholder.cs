// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: LauncherMountPlaceholder.cs
// Placeholder for Launcher Weapon Mounts.  
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
/// Placeholder for Launcher Weapon Mounts.  
/// </summary>
public class LauncherMountPlaceholder : AMountPlaceholder {

    protected override EquipmentCategory EquipmentCategory { get { return EquipmentCategory.LaunchedWeapon; } }

    protected override void Cleanup() { }


}

