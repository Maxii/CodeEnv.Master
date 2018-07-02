// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: TextDialogForm.cs
// COMMENT - one line to give a brief idea of what this file does.
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
/// APopupDialogForm for text. Also includes an optional Title and Icon.
/// <remarks>MenuControls include an optional Done button or Accept/Cancel buttons. 
/// Will throw an error if no buttons are supposed to show.</remarks>
/// <remarks>IMPROVE Allow use of supplied UISprite in place of default existing version.
/// Would allow for a supplied sprite to come with additional functionality like Tooltip support, etc.</remarks>
/// <remaris>IMPROVE Allow for changing names of buttons - Accept/Yes, Cancel/No, Done/OK.</remaris>
/// </summary>
public class TextDialogForm : APopupDialogForm {

    [SerializeField]
    private UIButton _cancelButton = null;

    [SerializeField]
    private UIButton _doneButton = null;

    [SerializeField]
    private UIButton _acceptButton = null;

    [SerializeField]
    private UILabel _titleLabel = null;

    [SerializeField]
    private UISprite _icon = null;

    [SerializeField]
    private UILabel _textLabel = null;

    public override FormID FormID { get { return FormID.TextDialog; } }

    protected override void InitializeValuesAndReferences() { }

    protected override void AssignValuesToMembers() {
        base.AssignValuesToMembers();

        _titleLabel.text = Settings.Title;
        _icon.atlas = Settings.IconAtlasID.GetAtlas();
        _icon.spriteName = Settings.IconFilename;
        _textLabel.text = Settings.Text;
    }

    protected override void InitializeMenuControls() {
        // activate and subscribe to menu controls
        if (Settings.ShowDoneButton) {
            _doneButton.gameObject.SetActive(true);
            if (Settings.DoneButtonDelegate != null) {
                EventDelegate.Set(_doneButton.onClick, Settings.DoneButtonDelegate);
            }
        }
        else {
            if (Settings.ShowCancelButton) {
                _cancelButton.gameObject.SetActive(true);
                if (Settings.CancelButtonDelegate != null) {
                    EventDelegate.Set(_cancelButton.onClick, Settings.CancelButtonDelegate);
                }
            }
            if (Settings.ShowAcceptButton) {
                _acceptButton.gameObject.SetActive(true);
                if (Settings.AcceptButtonDelegate != null) {
                    EventDelegate.Set(_acceptButton.onClick, Settings.AcceptButtonDelegate);
                }
            }
        }
    }

    protected override void UnsubscribeFromMenuControls() {
        if (Settings.CancelButtonDelegate != null) {
            EventDelegate.Remove(_cancelButton.onClick, Settings.CancelButtonDelegate);
        }
        if (Settings.DoneButtonDelegate != null) {
            EventDelegate.Remove(_doneButton.onClick, Settings.DoneButtonDelegate);
        }
        if (Settings.AcceptButtonDelegate != null) {
            EventDelegate.Remove(_acceptButton.onClick, Settings.AcceptButtonDelegate);
        }
    }

    protected override void DeactivateAllMenuControls() {
        NGUITools.SetActive(_cancelButton.gameObject, false);
        NGUITools.SetActive(_doneButton.gameObject, false);
        NGUITools.SetActive(_acceptButton.gameObject, false);
    }

    protected override void ResetForReuse_Internal() {
        base.ResetForReuse_Internal();

        _titleLabel.text = string.Empty;
        _icon.atlas = AtlasID.None.GetAtlas();
        _icon.spriteName = string.Empty;
        _textLabel.text = string.Empty;
    }

    protected override void Cleanup() {
        base.Cleanup();
        // nothing to cleanup beside menu control subscriptions handled by base.Cleanup
    }

    #region Debug

    protected override void __ValidateOnAwake() {
        base.__ValidateOnAwake();

        D.AssertNotNull(_cancelButton);
        D.AssertNotNull(_doneButton);
        D.AssertNotNull(_acceptButton);

        D.AssertNotNull(_titleLabel);
        D.AssertNotNull(_icon);
        D.AssertNotNull(_textLabel);
    }

    protected override void __Validate(DialogSettings settings) {
        if (settings.ShowDoneButton) {
            D.Assert(!settings.ShowAcceptButton);
            D.Assert(!settings.ShowCancelButton);
        }
        else {
            D.Assert(settings.ShowAcceptButton);
            D.AssertNotNull(settings.AcceptButtonDelegate);
            D.Assert(settings.ShowCancelButton);
        }
        Utility.ValidateForContent(settings.Text);
        D.AssertEqual(settings.Player, TempGameValues.NoPlayer);
    }

    #endregion

}

