// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemItem.cs
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
public class SystemItem : AItem {

    public new SystemData Data {
        get { return base.Data as SystemData; }
        set { base.Data = value; }
    }

    protected override void SubscribeToDataValueChanges() { }

    public void AssignSettlement(SettlementCreator settlementCreator) {
        D.Assert(gameObject.GetComponentInChildren<SettlementCreator>() == null, "{0} already has a Settlement.".Inject(Data.Name));
        GameObject orbitGoClone = UnityUtility.AddChild(gameObject, RequiredPrefabs.Instance.orbit.gameObject);
        UnityUtility.AttachChildToParent(settlementCreator.gameObject, orbitGoClone);
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
    private void InitializeSettlement(SettlementCreator creator) {
        if (!_isLocalCallFlag) {
            creator.onCompleted -= InitializeSettlement;
        }
        // IMPROVE for now, assign SettlementData to the System's Composition so the SystemHud works
        SettlementItem settlementCmd = creator.gameObject.GetComponentInChildren<SettlementItem>();
        Data.Composition.SettlementData = settlementCmd.Data;
        AddSystemAsLOSChangedRelayTarget(settlementCmd);

        Intel systemPlayerIntel = gameObject.GetSafeInterface<IViewable>().PlayerIntel;
        if (systemPlayerIntel == null) {
            D.Log("{0} has attached and initialized {1}. However, PlayerIntel is not yet set.", Data.Name, settlementCmd.PieceName);
            return;
        }
        settlementCmd.gameObject.GetSafeInterface<ICommandViewable>().PlayerIntel = systemPlayerIntel;
    }

    private void AddSystemAsLOSChangedRelayTarget(SettlementItem settlementCmd) {
        settlementCmd.gameObject.GetSafeMonoBehaviourComponentInChildren<CameraLOSChangedRelay>().AddTarget(_transform);
        settlementCmd.Elements.ForAll(element => element.gameObject.GetSafeMonoBehaviourComponentInChildren<CameraLOSChangedRelay>().AddTarget(_transform));
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

