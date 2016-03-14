// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlanetCtxControl.cs
// Context Menu Control for <see cref="PlanetItem"/>s. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;

/// <summary>
/// Context Menu Control for <see cref="PlanetItem"/>s. 
/// No distinction between AI and User owned. 
/// </summary>
public class PlanetCtxControl : PlanetoidCtxControl {

    private static FleetDirective[] _userRemoteFleetDirectives = new FleetDirective[] { FleetDirective.FullSpeedMove,
                                                                                        FleetDirective.Move,
                                                                                        FleetDirective.Orbit,
                                                                                        FleetDirective.Attack };

    protected override IEnumerable<FleetDirective> UserRemoteFleetDirectives { get { return _userRemoteFleetDirectives; } }

    public PlanetCtxControl(PlanetItem planet) : base(planet) { }

    protected override bool IsUserRemoteFleetMenuItemDisabledFor(FleetDirective directive) {
        switch (directive) {
            case FleetDirective.Attack:
                return !_user.IsEnemyOf(_planetoidMenuOperator.Owner);
            case FleetDirective.Move:
            case FleetDirective.FullSpeedMove:
                return false;
            case FleetDirective.Orbit:
                return !(_planetoidMenuOperator as IShipOrbitable).IsOrbitingAllowedBy(_user);
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

