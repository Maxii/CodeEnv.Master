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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

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
        set { SetProperty<string>(ref _text, value, "Text", OnTextChanged); }
    }

    public override FormID FormID { get { return FormID.TextHud; } }

    private UILabel _label;

    protected override void InitializeValuesAndReferences() {
        _label = gameObject.GetSingleComponentInChildren<UILabel>();
    }

    private void OnTextChanged() {
        AssignValuesToMembers();
    }

    protected override void AssignValuesToMembers() {
        _label.text = Text;
    }

    public override void Reset() {
        Text = null;
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

