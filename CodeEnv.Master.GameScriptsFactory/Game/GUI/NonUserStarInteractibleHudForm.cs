// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: NonUserStarInteractibleHudForm.cs
// Form used by the InteractibleHudWindow to display info when a NonUser-owned Item is selected.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Form used by the InteractibleHudWindow to display info when a NonUser-owned Item is selected.
/// </summary>
public class NonUserStarInteractibleHudForm : ANonUserItemInteractibleHudForm {

    public override FormID FormID { get { return FormID.NonUserStar; } }

    public new StarReport Report { get { return base.Report as StarReport; } }

    protected override void InitializeNameGuiElement(AGuiElement e) {
        base.InitializeNameGuiElement(e);
        MyEventListener.Get(e.gameObject).onDoubleClick += NameDoubleClickEventHandler;
    }

    protected override void AssignValueToNameGuiElement() {
        base.AssignValueToNameGuiElement();
        _nameLabel.text = Report.Name != null ? Report.Name : Unknown;
    }

    protected override void AssignValueToResourcesGuiElement() {
        base.AssignValueToResourcesGuiElement();
        _resourcesGuiElement.Resources = Report.Resources;
    }

    #region Event and Property Change Handlers

    private void NameDoubleClickEventHandler(GameObject go) {
        //D.Log("{0} Name Label double clicked.", DebugName);
        (Report.Item as ICameraFocusable).IsFocus = true;
    }

    #endregion

    protected override void CleanupNameGuiElement(AGuiElement e) {
        base.CleanupNameGuiElement(e);
        MyEventListener.Get(e.gameObject).onDoubleClick -= NameDoubleClickEventHandler;
    }
}

