// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: RowOrganizer.cs
// Debug class for use in edit mode (via ContextMenu.Execute) that automates the positioning of elements 
// within a table or header row according to the index setting in <c>RowElementIndexer</c>. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Debug class for use in edit mode (via ContextMenu.Execute) that automates the positioning of elements 
/// within a table or header row according to the index setting in <c>RowElementIndexer</c>. 
/// This class destroys and/or instantiates multiple separators when the row is built so it is slow. 
/// It is safe to leave on a row as it can only be activated from the ContextMenu.
/// </summary>
public class RowOrganizer : AMonoBase {

    public UISprite separatorPrefab;

    private float _rowMemberLocalPositionY;
    private int _elementHeight;
    private int _separatorWidth;
    private UIWidget _rowWidget;

    [ContextMenu("Execute")]
    private void Organize() {
        D.Log("{0}.Organize() called.", GetType().Name);
        InitializeLocalReferences();
        PositionRowElements();
    }

    private void InitializeLocalReferences() {
        D.Assert(separatorPrefab != null, "{0}.{1} separatorPrefab not set.".Inject(gameObject.name, GetType().Name), gameObject);
        _separatorWidth = separatorPrefab.width;
        _rowWidget = gameObject.GetSafeMonoBehaviour<UIWidget>();
        _rowMemberLocalPositionY = -_rowWidget.height / 2;
    }

    private void PositionRowElements() {
        DestroyExistingSeparators();

        var rowElements = gameObject.GetSafeMonoBehavioursInImmediateChildrenOnly<RowElementIndexer>();
        _elementHeight = ValidateElements(rowElements);

        var orderedElements = rowElements.OrderBy(re => re.index);
        var lastElement = orderedElements.Last();

        int nextElementXPosition = 0;
        orderedElements.ForAll(e => {
            bool toAddSeparator = e != lastElement;
            nextElementXPosition = PositionElement(e, nextElementXPosition, toAddSeparator);
        });
        _rowWidget.width = nextElementXPosition;
    }

    /// <summary>
    /// Validates the elements and returns the height of all elements.
    /// </summary>
    /// <param name="rowElements">The row elements.</param>
    /// <returns></returns>
    private int ValidateElements(RowElementIndexer[] rowElements) {
        int height = Constants.Zero;
        var indexesFound = new List<int>(rowElements.Length);
        rowElements.ForAll(e => {

            int index = e.index;
            D.Assert(index != Constants.MinusOne, "{0}.{1} not set.".Inject(e.gameObject.name, typeof(RowElementIndexer).Name));
            D.Warn(indexesFound.Contains(index), "Duplicate {0} index {1} found. Order will not be deterministic.", typeof(RowElementIndexer).Name, index);
            indexesFound.Add(index);

            if (height == Constants.Zero) {
                height = e.gameObject.GetSafeMonoBehaviour<UIWidget>().height;
            }
            else {
                D.Assert(e.gameObject.GetSafeMonoBehaviour<UIWidget>().height == height);
            }
        });
        return height;
    }

    /// <summary>
    /// Positions the element and adds a separator to its right (if indicated).
    /// Returns the starting localPosition.x for the next element which will be to the immediate
    /// right of the end of the element and/or separator.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="localPositionX">The value to set the element's localPosition.x.</param>
    /// <param name="addSeparator">if set to <c>true</c> [add separator].</param>
    /// <returns></returns>
    private int PositionElement(RowElementIndexer element, int localPositionX, bool addSeparator) {
        //D.Log("{0} setting {1} to localPosition.x = {2}.", GetType().Name, element.GetType().Name, localPositionX);
        element.transform.localPosition = new Vector3(localPositionX, _rowMemberLocalPositionY);
        element.transform.SetSiblingIndex(element.index);
        var elementWidth = element.gameObject.GetSafeMonoBehaviour<UIWidget>().width;
        var nextLocalPositionX = localPositionX + elementWidth;
        if (addSeparator) {
            MakeAndPositionSeparator(nextLocalPositionX);
            nextLocalPositionX += _separatorWidth;
        }
        //D.Log("{0} local position now at {1}.", element.GetType().Name, element.transform.localPosition);
        return nextLocalPositionX;
    }

    private void MakeAndPositionSeparator(int localPositionX) {
        var separatorGo = NGUITools.AddChild(gameObject, separatorPrefab.gameObject);
        separatorGo.GetComponent<UIWidget>().height = _elementHeight;
        separatorGo.transform.localPosition = new Vector3(localPositionX, _rowMemberLocalPositionY);
        separatorGo.transform.SetAsLastSibling();
    }

    private void DestroyExistingSeparators() {
        var rowWidgets = gameObject.GetSafeMonoBehavioursInImmediateChildrenOnly<UIWidget>();
        var separatorWidgets = rowWidgets.Where(w => w.gameObject.GetComponent<RowElementIndexer>() == null);
        if (separatorWidgets.Any()) {
            separatorWidgets.ForAll(sep => DestroyImmediate(sep.gameObject));
        }
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

