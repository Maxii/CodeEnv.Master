// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbaseCommandItem.cs
// Class for AUnitBaseCmdItems that are Starbases.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;

/// <summary>
/// Class for AUnitBaseCmdItems that are Starbases.
/// </summary>
public class StarbaseCmdItem : AUnitBaseCmdItem, ICmdPublisherClient<FacilityReport> {

    public new StarbaseCmdData Data {
        get { return base.Data as StarbaseCmdData; }
        set { base.Data = value; }
    }

    private StarbasePublisher _publisher;
    public StarbasePublisher Publisher {
        get { return _publisher = _publisher ?? new StarbasePublisher(Data, this); }
    }

    #region Initialization

    protected override HudManager InitializeHudManager() {
        var hudManager = new HudManager(Publisher);
        hudManager.AddContentToUpdate(AHudManager.UpdatableLabelContentID.IntelState);
        return hudManager;
    }

    #endregion

    #region Model Methods

    public StarbaseReport GetReport(Player player) { return Publisher.GetReport(player); }

    public FacilityReport[] GetElementReports(Player player) {
        return Elements.Cast<FacilityItem>().Select(e => e.GetReport(player)).ToArray();
    }

    protected override void OnHQElementChanged() {
        base.OnHQElementChanged();
    }

    protected override void OnDeath() {
        base.OnDeath();
        // unlike SettlementCmdItem, no parent orbiter object to disable or destroy
    }

    #endregion

    #region View Methods

    protected override IIcon MakeCmdIconInstance() {
        return StarbaseIconFactory.Instance.MakeInstance(Data);
    }

    #endregion

    #region Mouse Events

    #endregion

    #region Cleanup

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

