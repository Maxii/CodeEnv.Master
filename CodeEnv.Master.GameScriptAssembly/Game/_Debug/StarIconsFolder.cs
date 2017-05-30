﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarIconsFolder.cs
// Easy access to StarIcons folder in Scene. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;

/// <summary>
/// Easy access to StarIcons folder in Scene. 
/// </summary>
public class StarIconsFolder : AFolderAccess<StarIconsFolder> {

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        ValidateLayer();
    }

    private void ValidateLayer() {
        D.AssertEqual(Layers.TransparentFX, (Layers)gameObject.layer);
    }

    #region Cleanup

    protected override void Cleanup() { }

    #endregion



}

