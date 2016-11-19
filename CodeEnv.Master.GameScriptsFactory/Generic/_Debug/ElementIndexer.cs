// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ElementIndexer.cs
// Debug class used with <c>RowOrganizer</c> or <c>FormationGridOrganizer</c> to automate the positioning of elements 
// within a row, column, layer, etc according to the index setting contained here. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Debug class used with <c>RowOrganizer</c> or <c>FormationGridOrganizer</c> to automate the positioning of elements 
/// within a row, column, layer, etc according to the index setting contained here. 
/// It is safe to leave on an element as it is only used by XXXOrganizers which can only be activated in edit mode.
/// </summary>
public class ElementIndexer : AMonoBase {

    private const string ToStringFormat = "{0} Index: {1}";

    [Tooltip("Index of the element within the row/column/layer beginning on the left/bottom/rear at 0")]
    public int index = -1;

    protected override void Cleanup() { }

    public override string ToString() {
        return ToStringFormat.Inject(GetType().Name, index);
    }

}

