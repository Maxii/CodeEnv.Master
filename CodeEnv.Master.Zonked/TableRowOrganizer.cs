// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: TableRowOrganizer.cs
// Debug class that automates the positioning of elements within a table row according
// to the index setting in <c>RowGuiElement</c>.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System.Linq;
using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Debug class that automates the positioning of elements within a table row according
/// to the index setting in <c>RowGuiElement</c>. This class instantiates multiple separators
/// when the row is built so it is slow. It should be replaced by a preset prefab when ready for deployment.
/// </summary>
[System.Obsolete]
public class TableRowOrganizer : AMonoBase {

    private static int _stdRowHeight = 64;
    private static int _stdSeparatorHeight = 50;
    private static int _stdYLocalPosition = -32;

    public UISprite separatorPrefab;

    private int _separatorWidth;
    private UIWidget _rowWidget;
    //private bool _isOrganized;

    //protected override void Awake() {
    //    base.Awake();
    //    //_rowWidget = gameObject.GetSafeMonoBehaviour<UIWidget>();
    //    //Validate();
    //    //_separatorWidth = separatorPrefab.width;
    //    InitializeLocalReferences();
    //    D.Log("{0}.Awake() called.", GetType().Name);
    //}

    private void InitializeLocalReferences() {
        _rowWidget = gameObject.GetSafeMonoBehaviour<UIWidget>();
        Validate();
        _separatorWidth = separatorPrefab.width;
    }

    //protected override void Start() {
    //    base.Start();
    //    if (!_isOrganized) {
    //        D.Warn("{0}.Start() called with isOrganized = false.", GetType().Name); // was set to true in edit mode but was reset on play
    //        PositionRowElements();
    //    }
    //}

    [ContextMenu("Execute")]
    private void Organize() {
        D.Log("{0}.Organize() called.", GetType().Name);
        InitializeLocalReferences();
        PositionRowElements();
    }

    private void PositionRowElements() {
        var rowElements = gameObject.GetSafeMonoBehavioursInImmediateChildrenOnly<RowElementIndexer>();
        var orderedElements = rowElements.OrderBy(re => re.index);
        var lastElement = orderedElements.Last();

        int nextElementXPosition = 0;
        orderedElements.ForAll(e => {
            bool toAddSeparator = e != lastElement;
            nextElementXPosition = PositionElement(e, nextElementXPosition, toAddSeparator);
        });
        _rowWidget.width = nextElementXPosition;
        //_isOrganized = true;
    }
    //public void PositionRowElements() {
    //    var rowElements = gameObject.GetSafeMonoBehavioursInImmediateChildren<RowGuiElement>();
    //    var orderedElements = rowElements.OrderBy(re => re.RowIndex);
    //    var lastElement = orderedElements.Last();

    //    int nextElementXPosition = 0;
    //    orderedElements.ForAll(e => {
    //        bool toAddSeparator = e != lastElement;
    //        nextElementXPosition = PositionElement(e, nextElementXPosition, toAddSeparator);
    //    });
    //    _rowWidget.width = nextElementXPosition;
    //}

    /// <summary>
    /// Positions the element and adds a separator to its right (if indicated).
    /// Returns the starting xPosition for the next element which will be to the immediate
    /// right of the end of the element and/or separator.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="xPosition">The x position.</param>
    /// <param name="addSeparator">if set to <c>true</c> [add separator].</param>
    /// <returns></returns>
    private int PositionElement(RowElementIndexer element, int xPosition, bool addSeparator) {
        //D.Log("{0} setting {1} to localPosition.x = {2}.", GetType().Name, element.GetType().Name, xPosition);
        if (element.index == Constants.MinusOne) {
            D.Warn("{0} found {1} with un-initialized element index. Deactivating...", GetType().Name, element.gameObject.name);
            NGUITools.SetActive(element.gameObject, false);
            return xPosition;
        }

        element.transform.SetLocalPositionX(xPosition);
        var elementWidth = element.gameObject.GetSafeMonoBehaviour<UIWidget>().width;
        var nextElementXPosition = xPosition + elementWidth;
        if (addSeparator) {
            var separatorGo = NGUITools.AddChild(gameObject, separatorPrefab.gameObject);
            separatorGo.transform.localPosition = new Vector3(nextElementXPosition, _stdYLocalPosition);
            nextElementXPosition += _separatorWidth;
        }
        //D.Log("{0} local position now at {1}.", element.GetType().Name, element.transform.localPosition);
        return nextElementXPosition;
    }
    //private int PositionElement(RowGuiElement element, int xPosition, bool addSeparator) {
    //    //D.Log("{0} setting {1} to localPosition.x = {2}.", GetType().Name, element.GetType().Name, xPosition);
    //    element.transform.SetXLocalPosition(xPosition);
    //    var nextElementXPosition = xPosition + element.WidgetWidth;
    //    if (addSeparator) {
    //        var separatorGo = NGUITools.AddChild(gameObject, separatorPrefab.gameObject);
    //        separatorGo.transform.localPosition = new Vector3(nextElementXPosition, _stdYLocalPosition);
    //        nextElementXPosition += _separatorWidth;
    //    }
    //    //D.Log("{0} local position now at {1}.", element.GetType().Name, element.transform.localPosition);
    //    return nextElementXPosition;
    //}

    private void Validate() {
        D.Assert(_rowWidget.height == _stdRowHeight, "{0} height {1} != {2}.".Inject(GetType().Name, _rowWidget.height, _stdRowHeight), gameObject);
        D.Assert(separatorPrefab != null, "{0} separatorPrefab not set.".Inject(GetType().Name), gameObject);
        D.Assert(separatorPrefab.height == _stdSeparatorHeight);
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

