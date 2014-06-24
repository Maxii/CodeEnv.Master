// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: NewSystemModel.cs
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
public class NewSystemModel : AOwnedItemModel, INewSystemModel, IDestinationTarget {

    public new NewSystemData Data {
        get { return base.Data as NewSystemData; }
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
        GameObject systemGo = gameObject;
        GameObject orbitGo = UnityUtility.AddChild(systemGo, RequiredPrefabs.Instance.orbiter.gameObject);
        orbitGo.name = "SettlementOrbit";
        Transform settlementUnitTransform = settlementCmd.transform.parent;
        UnityUtility.AttachChildToParent(settlementUnitTransform.gameObject, orbitGo);
        // enabling (or not) the orbit around the star is handled by the SettlementCreator once isRunning
        settlementUnitTransform.localPosition = Data.SettlementOrbitSlot.GenerateRandomPositionWithinSlot(); // position this settlement unit in the orbit slot already reserved for it
        // IMPROVE should really be assigning the SettlementOrbitSlot to Settlement.Data.OrbitSlot and let it auto position, just like PlanetoidData.OrbitSlot
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

    #region IDestinationTarget Members

    public Vector3 Position { get { return Data.Position; } }

    public virtual bool IsMobile { get { return false; } }

    public SpaceTopography Topography { get { return Data.Topography; } }

    #endregion
}

