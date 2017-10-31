// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FormationChangeGuiElement.cs
// AGuiElement that represents and allows changes to a Unit's Formation.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// AGuiElement that represents and allows changes to a Unit's Formation.
/// </summary>
public class FormationChangeGuiElement : AGuiElement {

    public override GuiElementID ElementID { get { return GuiElementID.FormationChange; } }

    private Reference<Formation> _formationReference;
    public Reference<Formation> FormationReference {
        get { return _formationReference; }
        set {
            D.AssertNull(_formationReference);
            _formationReference = value;
            FormationReferencePropSetHandler();
        }
    }

    private IEnumerable<Formation> _acceptableFormations;
    public IEnumerable<Formation> AcceptableFormations {
        get { return _acceptableFormations; }
        set {
            D.AssertNull(_acceptableFormations);
            _acceptableFormations = value;
            AcceptableFormationsPropSetHandler();
        }
    }

    public override bool IsInitialized { get { return _formationReference != null && _acceptableFormations != null; } }

    private UIPopupList _formationPopupList;
    private UILabel _formationPopupListLabel;

    protected override void InitializeValuesAndReferences() {
        _formationPopupList = gameObject.GetSingleComponentInChildren<UIPopupList>();
        _formationPopupList.keepValue = true;
        EventDelegate.Add(_formationPopupList.onChange, FormationChangedEventHandler);
        _formationPopupListLabel = _formationPopupList.GetComponentInChildren<UILabel>();
    }

    protected override void PopulateMemberWidgetValues() {
        base.PopulateMemberWidgetValues();
        _formationPopupList.items = AcceptableFormations.Select(f => f.GetValueName()).ToList();
        string currentFormationName = FormationReference.Value.GetValueName();
        _formationPopupList.Set(currentFormationName, notify: false);
        _formationPopupListLabel.text = currentFormationName;
    }

    #region Event and Property Change Handlers

    private void FormationReferencePropSetHandler() {
        if (IsInitialized) {
            PopulateMemberWidgetValues();
        }
    }

    private void AcceptableFormationsPropSetHandler() {
        if (IsInitialized) {
            PopulateMemberWidgetValues();
        }
    }

    private void FormationChangedEventHandler() {
        HandleFormationChanged();
    }

    #endregion

    private void HandleFormationChanged() {
        var formation = Enums<Formation>.Parse(_formationPopupList.value);
        if (FormationReference.Value != formation) {
            //D.Log("{0}: UnitFormation changing from {1} to {2}.", DebugName, FormationReference.Value.GetValueName(), formation.GetValueName());
            FormationReference.Value = formation;
        }
    }

    public override void ResetForReuse() {
        _formationReference = null;
        _acceptableFormations = null;
        _formationPopupList.Set(null, notify: false);
        _formationPopupListLabel.text = null;
    }

    #region Cleanup

    protected override void Cleanup() {
        EventDelegate.Remove(_formationPopupList.onChange, FormationChangedEventHandler);
    }

    #endregion

}

