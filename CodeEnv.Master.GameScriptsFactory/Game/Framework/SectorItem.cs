// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SectorItem.cs
// Item class for Sectors.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
///  Item class for Sectors. 
/// </summary>
public class SectorItem : AItem {

    public new SectorData Data {
        get { return base.Data as SectorData; }
        set { base.Data = value; }
    }

    public Index3D SectorIndex { get { return Data.SectorIndex; } }

    #region Initialization

    protected override void InitializeLocalReferencesAndValues() {
        base.InitializeLocalReferencesAndValues();
        Radius = TempGameValues.SectorSideLength / 2F;  // the radius of the sphere inscribed inside a sector box
        // there is no collider associated with a SectorItem. The collider used for context menu activation is part of the SectorExaminer
    }

    protected override IIntel InitializePlayerIntel() {
        return new ImprovingIntel();
    }

    protected override void SubscribeToPlayerIntelCoverageChanged() {
        _subscribers.Add((PlayerIntel as ImprovingIntel).SubscribeToPropertyChanged<ImprovingIntel, IntelCoverage>(pi => pi.CurrentCoverage, OnPlayerIntelCoverageChanged));
    }

    protected override void InitializeModelMembers() { }

    protected override IGuiHudPublisher InitializeHudPublisher() {
        return new GuiHudPublisher<SectorData>(Data);
    }

    protected override void InitializeViewMembersOnDiscernible() {
        // TODO meshes and animations need to be added to sectors
        // UNCLEAR include a separate CullingLayer for Sector meshes and animations?   
    }

    #endregion

    #region Model Methods
    #endregion

    #region View Methods
    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IDestinationTarget Members

    public override bool IsMobile { get { return false; } }

    #endregion

    #region ICameraTargetable Members

    public override float MinimumCameraViewingDistance { get { throw new System.NotImplementedException("{0}".Inject(GetType().Name)); } }

    #endregion

    #region ICameraFocusable Members

    public override float OptimalCameraViewingDistance { get { throw new System.NotImplementedException("{0}".Inject(GetType().Name)); } }

    #endregion

}

