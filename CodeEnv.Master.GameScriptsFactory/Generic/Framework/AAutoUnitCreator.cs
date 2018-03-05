﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AAutoUnitCreator.cs
// Abstract base class for Unit Creators whose configuration is determined automatically in NewGameUnitConfigurator.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Abstract base class for Unit Creators whose configuration is determined automatically in NewGameUnitConfigurator.
/// </summary>
public abstract class AAutoUnitCreator : AUnitCreator {

    private UnitCreatorConfiguration _configuration;
    public UnitCreatorConfiguration Configuration {
        get { return _configuration; }
        set {
            D.AssertNull(_configuration);   // currently one time only
            SetProperty<UnitCreatorConfiguration>(ref _configuration, value, "Configuration", ConfigurationPropSetHandler);
        }
    }

    public sealed override GameDate DeployDate { get { return Configuration.DeployDate; } }

    protected sealed override Player Owner { get { return Configuration.Owner; } }

    protected PlayerDesigns _ownerDesigns;

    /// <summary>
    /// Builds and positions the Unit in preparation for deployment and operations.
    /// <remarks>Must be called before AuthorizeDeployment.</remarks>
    /// </summary>
    public sealed override void PrepareUnitForDeployment() {
        D.AssertNotNull(Configuration);    // would only be called with a Configuration
        D.Log(ShowDebugLog, "{0} is building and positioning {1}. Targeted DeployDate = {2}.", DebugName, UnitName, DeployDate);
        InitializeRootUnitName();
        MakeUnit();
    }

    private void MakeUnit() {
        LogEvent();
        MakeElements();
        MakeCommand();
        AddElementsToCommand();
        AssignHQElement();
        PositionUnit();
    }

    protected abstract void MakeElements();

    protected abstract void MakeCommand();

    protected abstract void AddElementsToCommand();

    /// <summary>
    /// Assigns the HQ element to the command. The assignment itself regenerates the formation,
    /// resulting in each element assuming the proper position.
    /// Note: This method must not be called before AddElementsToCommand().
    /// </summary>
    protected abstract void AssignHQElement();

    protected virtual void PositionUnit() { }

    #region Event and Property Change Handlers

    private void ConfigurationPropSetHandler() {
        HandleConfigurationPropSet();
    }

    #endregion

    private void HandleConfigurationPropSet() {
        _ownerDesigns = _gameMgr.GetAIManagerFor(Owner).Designs;
    }

}

