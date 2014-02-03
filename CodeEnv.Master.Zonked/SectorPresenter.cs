// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SectorPresenter.cs
// MVPresenter associated with a SectorView.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// MVPresenter associated with a SectorView.
/// </summary>
public class SectorPresenter : AItemPresenter {

    public new SectorModel Item {
        get { return base.Model as SectorModel; }
        protected set { base.Model = value; }
    }

    public SectorPresenter(IViewable view) : base(view) { }

    protected override AItemModel AcquireModelReference() {
        return UnityUtility.ValidateMonoBehaviourPresence<SectorModel>(_viewGameObject);
    }

    protected override IGuiHudPublisher InitializeHudPublisher() {
        return new GuiHudPublisher<SectorData>(Model.Data);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}


