// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SelectionHud.cs
// Gui WIndow displaying fixed position custom HUDs for ISelectable Items.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Gui WIndow displaying fixed position custom HUDs for ISelectable Items.
/// The current version is located on the bottom left of the screen and only appears
/// when an ISelectable Item has been selected.
/// </summary>
public class SelectionHud : AHud<SelectionHud> {

    /// <summary>
    /// The local-space corners of this popup. Order is bottom-left, top-left, top-right, bottom-right.
    /// Used by ItemHud to reposition itself to avoid interfering with this fixed Hud.
    /// </summary>
    public Vector3[] LocalCorners { get { return _backgroundWidget.localCorners; } }

    protected override void InitializeOnInstance() {
        base.InitializeOnInstance();
        References.SelectionHud = Instance;
    }

    protected override void PositionPopup() { }

    protected override void Cleanup() {
        base.Cleanup();
        References.SelectionHud = null;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

