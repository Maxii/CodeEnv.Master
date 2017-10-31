// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AHudWindow.cs
// Singleton. Abstract Gui Window for showing customized Forms that act as HUDs at various locations on the screen. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;

/// <summary>
/// Singleton. Abstract Gui Window for showing customized Forms that act as HUDs at various locations on the screen. 
/// AHudWindows have the ability to envelop their background around the Form they are displaying.
/// 10.5.17 Current derived classes include dynamically positioned TooltipHudWindow, HoveredHudWindow 
/// and the fixed InteractibleHudWindow and UnitHudWindow.
/// </summary>
public abstract class AHudWindow<T> : AFormWindow<T> where T : AHudWindow<T> {

    protected UIWidget _backgroundWidget;

    private MyEnvelopContent _backgroundEnvelopContent;

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        _backgroundEnvelopContent = gameObject.GetSingleComponentInChildren<MyEnvelopContent>();
        // TODO set envelopContent padding programmatically once background is permanently picked?
        _backgroundWidget = _backgroundEnvelopContent.gameObject.GetSafeComponent<UIWidget>();
    }

    #region Event and Property Change Handlers

    #endregion

    private void EncompassFormWithBackground(AForm form) {
        _backgroundEnvelopContent.targetRoot = form.transform;
        _backgroundEnvelopContent.Execute();
    }

    /// <summary>
    /// Shows the form in the window after it has been prepared.
    /// </summary>
    /// <param name="form">The form.</param>
    protected override void ShowForm(AForm form) {
        EncompassFormWithBackground(form);
        PositionWindow();
        base.ShowForm(form);
    }

    protected abstract void PositionWindow();

}

