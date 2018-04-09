// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AGuiWaitForInitializeMember.cs
// Abstract base class for Gui members that must wait to be initialized before populating their widgets. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Abstract base class for Gui members that must wait to be initialized before populating their widgets. 
/// Also has embedded text tooltip support and the ability to adjust the depth of member widgets.
/// </summary>
public abstract class AGuiWaitForInitializeMember : ATextTooltip {

    /// <summary>
    /// The incremental amount that each widget's depth in this Gui member will be increased.
    /// <remarks>Purpose is to elevate all member widget depths above that of an external background sprite if present,
    /// yet keep the relative depth of each widget compared to another the same.
    /// Background sprites that have a parent UIPanel different than the parent UIPanel of this AGuiMember are not
    /// necessary to account for as UIPanel depth supersedes UIWidget depth.</remarks>
    /// </summary>
    [Tooltip("The incremental amount that each widget's depth in this Gui member will be increased. Use 0 if there are no external background sprites to worry about.")]
    [SerializeField]
    private int _widgetDepthIncrement = Constants.Zero;

    /// <summary>
    /// Returns <c>true</c> if all required Properties have been set in preparation for populating member widget values.
    /// <remarks>Needs to be public as Forms can destroy members that aren't initialized when the member is placed
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

    [System.Diagnostics.Conditional("DEBUG")]
    protected virtual void __ValidateOnAwake() {
        if (_widgetDepthIncrement < Constants.Zero) {
            D.ErrorContext(this, "{0}'s _widgetDepthIncrement should not be negative.", DebugName);
        }
    }

    #endregion

}

