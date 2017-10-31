// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AGuiElement.cs
// Abstract member of the GUI that is uniquely identifiable by its GuiElementID. Also has embedded text tooltip support. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Abstract member of the GUI that is uniquely identifiable by its GuiElementID. Also has embedded text tooltip support. 
/// <remarks>AGuiElements are the lowest level scripts I've designed to modularize
/// the GUI. They typically have one or more Ngui UIWidget siblings and/or children associated with them.</remarks>
/// </summary>
public abstract class AGuiElement : ATextTooltip {

    protected const string Unknown = Constants.QuestionMark;

    public abstract GuiElementID ElementID { get; }

    /// <summary>
    /// The incremental amount that each widget's depth in this AGuiElement will be increased.
    /// <remarks>Purpose is to elevate all member widget depths above that of an external background sprite if present,
    /// yet keep the relative depth of each widget compared to another the same.
    /// Background sprites that have a parent UIPanel different than the parent UIPanel of this AGuiElement are not
    /// necessary to account for as UIPanel depth supersedes UIWidget depth.</remarks>
    /// </summary>
    [Tooltip("The incremental amount that each widget's depth in this AGuiElement will be increased. Use 0 if there are no external background sprites to worry about.")]
    [SerializeField]
    private int _widgetDepthIncrement = Constants.Zero;

    /// <summary>
    /// Returns <c>true</c> if all required Properties have been set in preparation for populating member widget values.
    /// <remarks>Needs to be public as Forms can destroy elements that aren't initialized when the element is placed
    /// as a child of the form in the editor for debug purposes.</remarks>
    /// </summary>
    public abstract bool IsInitialized { get; }

    protected override void Awake() {
        base.Awake();
        __ValidateOnAwake();
        InitializeValuesAndReferences();
        AdjustWidgetDepths(gameObject);
    }

    protected abstract void InitializeValuesAndReferences();

    protected virtual void PopulateMemberWidgetValues() {
        D.Assert(IsInitialized);
    }

    /// <summary>
    /// Increases all widget depths by _widgetDepthIncrement.
    /// </summary>
    /// <param name="topLevelGo">The top level GameObject holding widgets.</param>
    protected void AdjustWidgetDepths(GameObject topLevelGo) {
        var memberWidgets = topLevelGo.GetComponentsInChildren<UIWidget>();
        foreach (var widget in memberWidgets) {
            widget.depth += _widgetDepthIncrement;
        }
    }

    public abstract void ResetForReuse();

    #region Debug

    protected virtual void __ValidateOnAwake() {
        if (_widgetDepthIncrement < Constants.Zero) {
            D.ErrorContext(this, "{0}'s _widgetDepthIncrement should not be negative.", DebugName);
        }

        // 7.5.17 Removed requirement for a UIWidget so I can use GuiElement as an identifier for GuiWindows
        if (ElementID == default(GuiElementID)) {
            D.WarnContext(this, "{0}.{1} not set.", DebugName, typeof(GuiElementID).Name);
        }
    }

    #endregion

}

