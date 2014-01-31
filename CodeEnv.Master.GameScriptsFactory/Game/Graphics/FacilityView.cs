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

using CodeEnv.Master.Common;

/// <summary>
/// A class for managing the elements of a Facility's UI. 
/// </summary>
public class FacilityView : AElementView {

    public new FacilityPresenter Presenter {
        get { return base.Presenter as FacilityPresenter; }
        protected set { base.Presenter = value; }
    }

    protected override void Awake() {
        base.Awake();
        Subscribe();
    }

    protected override void InitializePresenter() {
        Presenter = new FacilityPresenter(this);
    }

    protected override void OnAltLeftClick() {
        base.OnAltLeftClick();
        Presenter.__SimulateAttacked();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

