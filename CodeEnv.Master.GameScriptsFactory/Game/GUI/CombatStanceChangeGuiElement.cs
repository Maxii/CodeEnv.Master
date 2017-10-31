// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CombatStanceChangeGuiElement.cs
// AGuiElement that represents and allows changes to a Ship's CombatStance.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// AGuiElement that represents and allows changes to a Ship's CombatStance.
/// </summary>
public class CombatStanceChangeGuiElement : AGuiElement {

    public override GuiElementID ElementID { get { return GuiElementID.CombatStanceChange; } }

    private Reference<ShipCombatStance> _combatStanceReference;
    public Reference<ShipCombatStance> CombatStanceReference {
        get { return _combatStanceReference; }
        set {
            D.AssertNull(_combatStanceReference);
            _combatStanceReference = value;
            CombatStanceReferencePropSetHandler();
        }
    }

    public override bool IsInitialized { get { return _combatStanceReference != null; } }

    private UIPopupList _stancePopupList;
    private UILabel _stancePopupListLabel;

    protected override void InitializeValuesAndReferences() {
        _stancePopupList = gameObject.GetSingleComponentInChildren<UIPopupList>();
        _stancePopupList.keepValue = true;
        EventDelegate.Add(_stancePopupList.onChange, StanceChangedEventHandler);
        _stancePopupListLabel = _stancePopupList.GetComponentInChildren<UILabel>();
    }

    protected override void PopulateMemberWidgetValues() {
        base.PopulateMemberWidgetValues();
        _stancePopupList.items = Enums<ShipCombatStance>.GetValues(excludeDefault: true).Select(cs => cs.GetValueName()).ToList();
        string currentStanceName = CombatStanceReference.Value.GetValueName();
        _stancePopupList.Set(currentStanceName, notify: false);
        _stancePopupListLabel.text = currentStanceName;
    }

    #region Event and Property Change Handlers

    private void CombatStanceReferencePropSetHandler() {
        if (IsInitialized) {
            PopulateMemberWidgetValues();
        }
    }

    private void StanceChangedEventHandler() {
        HandleStanceChanged();
    }

    #endregion

    private void HandleStanceChanged() {
        var combatStance = Enums<ShipCombatStance>.Parse(_stancePopupList.value);
        if (CombatStanceReference.Value != combatStance) {
            //D.Log("{0}: ShipCombatStance changing from {1} to {2}.", DebugName, CombatStanceReference.Value.GetValueName(), combatStance.GetValueName());
            CombatStanceReference.Value = combatStance;
        }
    }

    public override void ResetForReuse() {
        _combatStanceReference = null;
        _stancePopupList.Set(null, notify: false);
        _stancePopupListLabel.text = null;
    }

    #region Cleanup

    protected override void Cleanup() {
        EventDelegate.Remove(_stancePopupList.onChange, StanceChangedEventHandler);
    }

    #endregion


}

