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

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Class for AItems that are Sectors.
/// </summary>
public class SectorItem : AItem, ISectorItem {

    private static string _toStringFormat = "{0}{1}";

    public new SectorData Data {
        get { return base.Data as SectorData; }
        set { base.Data = value; }
    }

    public Index3D SectorIndex { get { return Data.SectorIndex; } }

    private SystemItem _system;
    /// <summary>
    /// The System present in this Sector, if any.
    /// </summary>
    public SystemItem System {
        get { return _system; }
        set {
            D.Assert(_system == null);    // one time only, if at all 
            SetProperty<SystemItem>(ref _system, value, "System", SystemPropChangedHandler);
        }
    }

    private SectorPublisher _publisher;
    public SectorPublisher Publisher {
        get { return _publisher = _publisher ?? new SectorPublisher(Data, this); }
    }

    public override float Radius { get { return TempGameValues.SectorSideLength / 2F; } }   // the radius of the sphere inscribed inside a sector box

    #region Initialization

    protected override void InitializeOnData() {
        _hudManager = new ItemHudManager(Publisher);
        // Note: There is no collider associated with a SectorItem. The collider used for context menu activation is part of the SectorExaminer
    }

    #endregion

    public SectorReport GetUserReport() { return Publisher.GetUserReport(); }

    public SectorReport GetReport(Player player) { return Publisher.GetReport(player); }

    private void SystemPropChangedHandler() {
        Data.SystemData = System.Data;
        // The owner of a sector and all it's celestial objects is determined by the ownership of the System, if any
    }

    #region Cleanup

    protected override void Cleanup() {
        base.Cleanup();
        Data.Dispose();
    }

    #endregion

    public override string ToString() {
        return _toStringFormat.Inject(GetType().Name, SectorIndex);
    }

    #region INavigableTarget Members

    public override float RadiusAroundTargetContainingKnownObstacles { get { return System != null ? System.Radius : Constants.ZeroF; } }
    // TODO what about a Starbase or Nebula?

    public override float GetShipArrivalDistance(float shipCollisionAvoidanceRadius) { return Radius / 2F; }  // 600



    #endregion


}

