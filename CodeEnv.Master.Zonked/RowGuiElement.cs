// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: RowGuiElement.cs
// Debug class needed in conjunction with <c>RowBuilder</c> to automate the positioning of elements 
// within a table row according to the index setting contained here. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Debug class needed in conjunction with <c>RowBuilder</c> to automate the positioning of elements 
/// within a table row according to the index setting contained here. 
/// As this process is slow, it should be taken out of the heirarchy when ready for deployment.
/// </summary>
[System.Obsolete]
public class RowGuiElement : GuiElement {

    private static int _elementHeight = 50;

    [Tooltip("Index of the element within the row beginning at 0")]
    public int rowIndex = Constants.MinusOne;

    public int RowIndex { get { return rowIndex; } }

    public int WidgetWidth { get { return _widget.width; } }

    private UIWidget _widget;

    protected override void Awake() {
        base.Awake();
        _widget = gameObject.GetSafeMonoBehaviour<UIWidget>();
        Validate();
    }

    private void Validate() {
        D.Warn(rowIndex != Constants.MinusOne, "{0} rowIndex not set.".Inject(GetType().Name));
        //D.Assert(rowIndex != Constants.MinusOne, "{0} rowIndex not set.".Inject(GetType().Name), gameObject);
        D.Assert(_widget.height == _elementHeight, gameObject);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

