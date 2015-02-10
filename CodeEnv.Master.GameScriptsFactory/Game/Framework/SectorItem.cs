// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SectorItem.cs
// Class for AItems that are Sectors.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Class for AItems that are Sectors.
/// </summary>
public class SectorItem : AItem {

    public new SectorItemData Data {
        get { return base.Data as SectorItemData; }
        set { base.Data = value; }
    }

    public Index3D SectorIndex { get { return Data.SectorIndex; } }

    private SectorPublisher _publisher;
    public SectorPublisher Publisher {
        get { return _publisher = _publisher ?? new SectorPublisher(Data); }
    }

    #region Initialization

    /// <summary>
    /// Called from Awake, initializes local references and values including Radius-related components.
    /// </summary>
    protected override void InitializeLocalReferencesAndValues() {
        base.InitializeLocalReferencesAndValues();
        Radius = TempGameValues.SectorSideLength / 2F;  // the radius of the sphere inscribed inside a sector box
        // there is no collider associated with a SectorItem. The collider used for context menu activation is part of the SectorExaminer
    }

    protected override void InitializeModelMembers() { }

    protected override HudManager InitializeHudManager() {
        var hudManager = new HudManager(Publisher);
        return hudManager;
    }

    #endregion

    #region Model Methods

    public SectorReport GetReport(Player player) { return Publisher.GetReport(player); }

    #endregion

    #region View Methods

    #endregion

    #region Cleanup

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

