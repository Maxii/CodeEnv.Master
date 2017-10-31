// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UserPlanetoidInteractibleHudForm.cs
// Form used by the InteractibleHudWindow to display info and allow changes when a user-owned Item is selected.
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
/// Form used by the InteractibleHudWindow to display info and allow changes when a user-owned Item is selected.
/// </summary>
public class UserPlanetoidInteractibleHudForm : AUserItemInteractibleHudForm {

    public override FormID FormID { get { return FormID.UserPlanetoid; } }

    public new PlanetoidData ItemData { get { return base.ItemData as PlanetoidData; } }

    protected override void InitializeNameGuiElement(AGuiElement e) {
        base.InitializeNameGuiElement(e);
        MyEventListener.Get(e.gameObject).onDoubleClick += NameDoubleClickEventHandler;
    }

    protected override void AssignValueToNameGuiElement() {
        base.AssignValueToNameGuiElement();
        _nameLabel.text = ItemData.Name;
    }

    protected override void AssignValueToResourcesGuiElement() {
        base.AssignValueToResourcesGuiElement();
        _resourcesGuiElement.Resources = ItemData.Resources;
    }

    #region Event and Property Change Handlers

    private void NameDoubleClickEventHandler(GameObject go) {
        //D.Log("{0} Name Label double clicked.", DebugName);
        (ItemData.Item as ICameraFocusable).IsFocus = true;
    }

    #endregion

    protected override void CleanupNameGuiElement(AGuiElement e) {
        base.CleanupNameGuiElement(e);
        MyEventListener.Get(e.gameObject).onDoubleClick -= NameDoubleClickEventHandler;
    }

}

