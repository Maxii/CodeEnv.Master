// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiMenuAcceptButtonBase.cs
// Base class for GuiMenuAccept buttons that accumulate changes from its sibling
// attached menu items before Raising an event with a Settings object attached.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Base class for GuiMenuAccept buttons that accumulate changes from its sibling
/// attached menu items before Raising an event with a Settings object attached.
/// </summary>
public abstract class GuiMenuAcceptButtonBase : GuiButtonBase {

    // Can be empty
    protected UICheckbox[] checkboxes;
    protected UIPopupList[] popupLists;
    protected UISlider[] sliders;

    protected override void Initialize() {
        base.Initialize();
        GameObject buttonParent = gameObject.transform.parent.gameObject;

        // acquire all the checkboxes here
        checkboxes = buttonParent.GetSafeMonoBehaviourComponentsInChildren<UICheckbox>(includeInactive: true);
        if (Utility.CheckForContent<UICheckbox>(checkboxes)) {
            checkboxes.ForAll<UICheckbox>(checkbox => checkbox.onStateChange += OnCheckboxStateChange);
        }

        popupLists = buttonParent.GetSafeMonoBehaviourComponentsInChildren<UIPopupList>(includeInactive: true);
        if (Utility.CheckForContent<UIPopupList>(popupLists)) {
            popupLists.ForAll<UIPopupList>(popupList => popupList.onSelectionChange += OnPopupListSelectionChange);
        }

        sliders = buttonParent.GetSafeMonoBehaviourComponentsInChildren<UISlider>(includeInactive: true);
        if (Utility.CheckForContent<UISlider>(sliders)) {
            sliders.ForAll<UISlider>(slider => slider.onValueChange += OnSliderValueChange);
        }
    }

    protected abstract void OnCheckboxStateChange(bool state);

    protected abstract void OnPopupListSelectionChange(string item);

    protected abstract void OnSliderValueChange(float value);

    // IDisposable Note: No reason to remove Ngui event listeners OnDestroy() as the EventListener or
    // Delegate to be removed is attached to a GameObject that is also being destroyed. In addition,
    // execution is problematic as the gameObject may have already been destroyed.

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

