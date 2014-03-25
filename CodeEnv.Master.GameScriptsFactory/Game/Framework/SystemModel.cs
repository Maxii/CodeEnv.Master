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

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// The data-holding class for all Systems in the game.  
/// WARNING: Donot change name to "System", a protected word.
/// </summary>
public class SystemModel : AItemModel, ISystemModel {

    public new SystemData Data {
        get { return base.Data as SystemData; }
        set { base.Data = value; }
    }

    protected override void Awake() {
        base.Awake();
        Subscribe();
    }

    protected override void Initialize() { }

    public void AssignSettlement(SettlementUnitCreator settlementCreator) {
        D.Assert(gameObject.GetComponentInChildren<SettlementUnitCreator>() == null, "{0} already has a Settlement.".Inject(Data.Name));
        GameObject orbitGo = UnityUtility.AddChild(gameObject, RequiredPrefabs.Instance.orbit.gameObject);
        orbitGo.name = "SettlementOrbit";
        UnityUtility.AttachChildToParent(settlementCreator.gameObject, orbitGo);
        // position this settlement piece in the orbit slot already reserved for it
        settlementCreator.transform.localPosition = Data.SettlementOrbitSlot;
        if (settlementCreator.IsCompleted) {
            _isLocalCallFlag = true;
            InitializeSettlement(settlementCreator);
        }
        else {
            settlementCreator.onCompleted += InitializeSettlement;
        }
    }

    private bool _isLocalCallFlag;
    private void InitializeSettlement(SettlementUnitCreator creator) {
        if (!_isLocalCallFlag) {
            creator.onCompleted -= InitializeSettlement;
        }
        SettlementCmdModel settlementCmd = creator.gameObject.GetSafeMonoBehaviourComponentInChildren<SettlementCmdModel>();
        Data.SettlementData = settlementCmd.Data;

        AddSystemAsLOSChangedRelayTarget(settlementCmd);

        IIntel systemPlayerIntel = gameObject.GetSafeInterface<IViewable>().PlayerIntel;
        // UNCLEAR should a new settlement being attached to a System take on the PlayerIntel state of the System??  See SystemPresenter.OnPlayerIntelCoverageChanged()
        settlementCmd.gameObject.GetSafeInterface<ICommandViewable>().PlayerIntel.CurrentCoverage = systemPlayerIntel.CurrentCoverage;
    }

    private void AddSystemAsLOSChangedRelayTarget(SettlementCmdModel settlementCmd) {
        settlementCmd.gameObject.GetSafeMonoBehaviourComponentInChildren<CameraLOSChangedRelay>().AddTarget(_transform);
        settlementCmd.Elements.ForAll(element => element.Transform.GetSafeMonoBehaviourComponentInChildren<CameraLOSChangedRelay>().AddTarget(_transform));
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

