// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: TextForm.cs
// A form that displays text content. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// A form that displays text content. 
/// </summary>
public class TextForm : AForm {

    public override FormID FormID { get { return FormID.TextHud; } }

    private string _text;
    public string Text {
        get { return _text; }
        set {
            D.AssertNull(_text);    // occurs only once between Resets
            SetProperty<string>(ref _text, value, "Text", TextPropSetHandler);
        }
    }

    private UILabel _textLabel;

    protected override void InitializeValuesAndReferences() {
        _textLabel = gameObject.GetSingleComponentInChildren<UILabel>();
    }

    #region Event and Property Change Handlers

    private void TextPropSetHandler() {
        AssignValuesToMembers();
    }

    #endregion

    protected override void AssignValuesToMembers() {
        //D.Log("{0}.AssignValuesToMembers() called. Text = {1}.", DebugName, Text);
        _textLabel.text = Text;
    }

    protected override void ResetForReuse_Internal() {
        _text = null;
    }

    protected override void Cleanup() { }

}

