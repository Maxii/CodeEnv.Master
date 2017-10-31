// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: NameChangeGuiElement.cs
// AGuiElement that represents and allows changes to an Item's name.
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
/// AGuiElement that represents and allows changes to an Item's name.
/// </summary>
public class NameChangeGuiElement : AGuiElement, IComparable<NameChangeGuiElement> {

    public sealed override GuiElementID ElementID { get { return GuiElementID.NameChange; } }

    private Reference<string> _nameReference;
    public Reference<string> NameReference {
        get { return _nameReference; }
        set {
            D.AssertNull(_nameReference);
            _nameReference = value;
            NameReferencePropSetHandler();
        }
    }

    public sealed override bool IsInitialized { get { return _nameReference != null; } }

    protected override string TooltipContent { get { return "Click to change Item name"; } }

    private UIInput _nameInput;
    private UILabel _nameInputLabel;

    protected override void InitializeValuesAndReferences() {
        _nameInput = gameObject.GetSingleComponentInChildren<UIInput>();
        EventDelegate.Add(_nameInput.onSubmit, NameInputSubmittedEventHandler);
        _nameInputLabel = _nameInput.GetComponentInChildren<UILabel>();
    }

    protected sealed override void PopulateMemberWidgetValues() {
        base.PopulateMemberWidgetValues();
        _nameInput.value = NameReference.Value;
        _nameInputLabel.text = _nameInput.value;
        //D.Log("{0}: Input field has been assigned {1}.", DebugName, _nameInput.value);
    }

    #region Event and Property Change Handlers

    private void NameReferencePropSetHandler() {
        if (IsInitialized) {
            PopulateMemberWidgetValues();
        }
    }

    private void NameInputSubmittedEventHandler() {
        HandleNameInputSubmitted();
    }

    #endregion

    private void HandleNameInputSubmitted() {
        if (NameReference.Value != _nameInput.value) {
            NameReference.Value = _nameInput.value;
        }
        else {
            D.Warn("{0}: Name {1} submitted without being changed.", DebugName, _nameInput.value);
        }
        _nameInputLabel.text = _nameInput.value;
        _nameInput.RemoveFocus();
    }

    public sealed override void ResetForReuse() {
        _nameReference = null;
        _nameInput.Set(null, notify: false);
        _nameInputLabel.text = null;
    }

    #region Cleanup

    protected sealed override void Cleanup() {
        EventDelegate.Remove(_nameInput.onSubmit, NameInputSubmittedEventHandler);
    }

    #endregion

    #region IComparable<NameChangeGuiElement> Members

    public int CompareTo(NameChangeGuiElement other) {
        return NameReference.Value.CompareTo(other.NameReference.Value);
    }

    #endregion

}

