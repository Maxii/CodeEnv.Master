// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: TextForm.cs
// A form that displays simple text content. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// A form that displays simple text content. 
/// </summary>
public class TextForm : AForm {

    private string _text;
    public string Text {
        get { return _text; }
        set {
            D.AssertNull(_text);    // occurs only once between Resets
            SetProperty<string>(ref _text, value, "Text", TextPropSetHandler);
        }
    }

    public override FormID FormID { get { return FormID.TextHud; } }

    private UILabel _label;

    protected override void InitializeValuesAndReferences() {
        _label = gameObject.GetSingleComponentInChildren<UILabel>();
    }

    #region Event and Property Change Handlers

    private void TextPropSetHandler() {
        AssignValuesToMembers();
    }

    #endregion

    protected override void AssignValuesToMembers() {
        //D.Log("{0}.AssignValuesToMembers() called. Text = {1}.", GetType().Name, Text);
        _label.text = Text;
    }

    public override void Reset() {
        //D.Log("{0}.Reset() called.", GetType().Name);
        _text = null;
    }

    protected override void Cleanup() { }

}

