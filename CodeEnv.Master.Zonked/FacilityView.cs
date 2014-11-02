// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FacilityView.cs
// A class for managing the elements of a Facility's UI. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// A class for managing the elements of a Facility's UI. 
/// </summary>
public class FacilityView : AUnitElementView {

    public new FacilityPresenter Presenter {
        get { return base.Presenter as FacilityPresenter; }
        protected set { base.Presenter = value; }
    }

    private Revolver _revolver;

    protected override void Awake() {
        base.Awake();
        Subscribe();
    }

    protected override void InitializePresenter() {
        Presenter = new FacilityPresenter(this);
    }

    protected override void InitializeVisualMembers() {
        base.InitializeVisualMembers();
        _revolver = gameObject.GetComponentInChildren<Revolver>();
        _revolver.enabled = true;
        // TODO Revolver settings and distance controls, Revolvers control their own enabled state based on visibility
    }

    protected override void OnIsDiscernibleChanged() {
        base.OnIsDiscernibleChanged();
        _revolver.enabled = IsDiscernible;  // Revolvers disable when invisible, but I also want to disable if IntelCoverage disappears
    }

    public override void AssessHighlighting() {
        if (!IsDiscernible) {
            Highlight(Highlights.None);
            return;
        }
        if (IsFocus) {
            if (Presenter.IsCommandSelected) {
                Highlight(Highlights.FocusAndGeneral);
                return;
            }
            Highlight(Highlights.Focused);
            return;
        }
        if (Presenter.IsCommandSelected) {
            Highlight(Highlights.General);
            return;
        }
        Highlight(Highlights.None);
    }

    protected override void Highlight(Highlights highlight) {
        switch (highlight) {
            case Highlights.Focused:
                ShowCircle(true, Highlights.Focused);
                ShowCircle(false, Highlights.General);
                break;
            case Highlights.General:
                ShowCircle(false, Highlights.Focused);
                ShowCircle(true, Highlights.General);
                break;
            case Highlights.FocusAndGeneral:
                ShowCircle(true, Highlights.Focused);
                ShowCircle(true, Highlights.General);
                break;
            case Highlights.None:
                ShowCircle(false, Highlights.Focused);
                ShowCircle(false, Highlights.General);
                break;
            case Highlights.Selected:
            case Highlights.SelectedAndFocus:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(highlight));
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

