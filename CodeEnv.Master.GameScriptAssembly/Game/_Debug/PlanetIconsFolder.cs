// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlanetIconsFolder.cs
// Easy access to PlanetIcons folder in Scene. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;

/// <summary>
/// Easy access to PlanetIcons folder in Scene. 
/// </summary>
public class PlanetIconsFolder : AFolderAccess<PlanetIconsFolder> {

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        ValidateLayer();
    }

    private void ValidateLayer() {
        D.AssertEqual(Layers.Cull_1000, (Layers)gameObject.layer);
    }

    #region Cleanup

    protected override void Cleanup() { }

    #endregion


}

