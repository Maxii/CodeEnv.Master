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
        SettlementItem settlementCmd = settlementCreator.gameObject.GetComponentInChildren<SettlementItem>();
        if (settlementCmd != null) {
            // the creator has already deployed the settlement so finish initialization
            InitializeSettlement(settlementCmd);
        }
        else {
            D.Warn("{0} assigned to {1} but not yet completed.", settlementCreator.PieceName, Data.Name);
        }
    }

    public void InitializeSettlement(SettlementItem settlementCmd) {
        Intel systemPlayerIntel = gameObject.GetSafeInterface<IViewable>().PlayerIntel;
        if (systemPlayerIntel != null) {
            settlementCmd.gameObject.GetSafeInterface<ICommandViewable>().PlayerIntel = systemPlayerIntel;
            D.Log("{0} has attached and initialized {1}.", Data.Name, settlementCmd.PieceName);
        }
        else {
            D.Warn("{0} PlayerIntel not yet set.", Data.Name);
        }
        // IMPROVE for now, assign SettlementData to the System's Composition so the SystemHud works
        Data.Composition.SettlementData = settlementCmd.Data;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

