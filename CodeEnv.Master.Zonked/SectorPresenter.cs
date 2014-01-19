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
public class SectorPresenter : APresenter {

    public new SectorItem Item {
        get { return base.Item as SectorItem; }
        protected set { base.Item = value; }
    }

    public SectorPresenter(IViewable view) : base(view) { }

    protected override AItem AcquireItemReference() {
        return UnityUtility.ValidateMonoBehaviourPresence<SectorItem>(_viewGameObject);
    }

    protected override IGuiHudPublisher InitializeHudPublisher() {
        return new GuiHudPublisher<SectorData>(Item.Data);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}


