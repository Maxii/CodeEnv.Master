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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace


/// <summary>
/// Context Menu Control for <see cref="PlanetItem"/>s. 
/// No distinction between AI and User owned. 
/// <remarks>OPTIMIZE Currently here in anticipation of different context order capability between planets and moons.</remarks>
/// </summary>
public class PlanetCtxControl : PlanetoidCtxControl {

    public PlanetCtxControl(PlanetItem planet) : base(planet) { }


}

