// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemModel.cs
// The data-holding class for all Systems in the game.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// The data-holding class for all Systems in the game.  
/// WARNING: Donot change name to "System", a protected word.
/// </summary>
public class SystemModel : AItemModel, ISystemModel, ISystemTarget {

    public new SystemData Data {
        get { return base.Data as SystemData; }
        set { base.Data = value; }
    }

    protected override void Awake() {
        base.Awake();
        Subscribe();
    }

    protected override void InitializeRadiiComponents() {
        Radius = TempGameValues.SystemRadius;
        // IMPROVE currently no need to set the radius of the System's orbital plane collider as it simply matches the mesh it is assigned too
    }

    protected override void Initialize() { }

    public void AssignSettlement(SettlementCmdModel settlementCmd) {
        D.Assert(gameObject.GetComponentInChildren<SettlementCmdModel>() == null, "{0} already has a Settlement.".Inject(FullName));
        GameObject orbitGo = UnityUtility.AddChild(gameObject, RequiredPrefabs.Instance.orbiter.gameObject);
        orbitGo.name = "SettlementOrbit";
        Transform settlementUnit = settlementCmd.transform.parent;
        UnityUtility.AttachChildToParent(settlementUnit.gameObject, orbitGo);
        // enabling (or not) the orbit around the star is handled by the SettlementCreator once isRunning
        settlementUnit.localPosition = Data.SettlementOrbitSlot;        // position this settlement unit in the orbit slot already reserved for it
        InitializeSettlement(settlementCmd);
    }

    private void InitializeSettlement(SettlementCmdModel settlementCmd) {
        D.Log("{0} is being deployed to {1}.", settlementCmd.Data.ParentName, FullName);
        Data.SettlementData = settlementCmd.Data;

        AddSystemAsLOSChangedRelayTarget(settlementCmd);

        var systemIntelCoverage = gameObject.GetSafeInterface<IViewable>().PlayerIntel.CurrentCoverage;
        if (systemIntelCoverage == IntelCoverage.None) {
            D.Warn("{0}.IntelCoverage set to None by its assigned System {1}.", settlementCmd.FullName, FullName);
        }
        // UNCLEAR should a new settlement being attached to a System take on the PlayerIntel state of the System??  See SystemPresenter.OnPlayerIntelCoverageChanged()
        settlementCmd.gameObject.GetSafeInterface<ICommandViewable>().PlayerIntel.CurrentCoverage = systemIntelCoverage;
    }

    private void AddSystemAsLOSChangedRelayTarget(SettlementCmdModel settlementCmd) {
        settlementCmd.gameObject.GetSafeMonoBehaviourComponentInChildren<CameraLOSChangedRelay>().AddTarget(_transform);
        settlementCmd.Elements.ForAll(element => element.Transform.GetSafeMonoBehaviourComponentInChildren<CameraLOSChangedRelay>().AddTarget(_transform));
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

