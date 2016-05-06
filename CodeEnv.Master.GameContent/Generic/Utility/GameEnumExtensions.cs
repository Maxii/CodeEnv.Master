// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameEnumExtensions.cs
// Extensions for Enums specific to the game.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    /// <summary>
    /// Extensions for Enums specific to the game. Most values
    /// are acquired from external XML files.
    /// </summary>
    public static class GameEnumExtensions {

        #region Universe Size and Default Player Count

        private static UniverseSizeXmlPropertyReader _universeSizeXmlReader = UniverseSizeXmlPropertyReader.Instance;

        /// <summary>
        /// Converts this UniverseSizeGuiSelection value to a UniverseSize value.
        /// </summary>
        /// <param name="universeSizeSelection">The universe size GUI selection.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static UniverseSize Convert(this UniverseSizeGuiSelection universeSizeSelection) {
            switch (universeSizeSelection) {
                case UniverseSizeGuiSelection.Random:
                    return Enums<UniverseSize>.GetRandom(excludeDefault: true);
                case UniverseSizeGuiSelection.Tiny:
                    return UniverseSize.Tiny;
                case UniverseSizeGuiSelection.Small:
                    return UniverseSize.Small;
                case UniverseSizeGuiSelection.Normal:
                    return UniverseSize.Normal;
                case UniverseSizeGuiSelection.Large:
                    return UniverseSize.Large;
                case UniverseSizeGuiSelection.Enormous:
                    return UniverseSize.Enormous;
                case UniverseSizeGuiSelection.Gigantic:
                    return UniverseSize.Gigantic;
                case UniverseSizeGuiSelection.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(universeSizeSelection));
            }
        }

        public static float Radius(this UniverseSize universeSize) {
            switch (universeSize) {
                case UniverseSize.Tiny:
                    return _universeSizeXmlReader.TinyRadius;
                case UniverseSize.Small:
                    return _universeSizeXmlReader.SmallRadius;
                case UniverseSize.Normal:
                    return _universeSizeXmlReader.NormalRadius;
                case UniverseSize.Large:
                    return _universeSizeXmlReader.LargeRadius;
                case UniverseSize.Enormous:
                    return _universeSizeXmlReader.EnormousRadius;
                case UniverseSize.Gigantic:
                    return _universeSizeXmlReader.GiganticRadius;
                case UniverseSize.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(universeSize));
            }
        }

        public static int DefaultPlayerCount(this UniverseSize universeSize) {
            int defaultPlayerCount;
            switch (universeSize) {
                case UniverseSize.Tiny:
                    defaultPlayerCount = _universeSizeXmlReader.TinyDefaultPlayerCount;
                    break;
                case UniverseSize.Small:
                    defaultPlayerCount = _universeSizeXmlReader.SmallDefaultPlayerCount;
                    break;
                case UniverseSize.Normal:
                    defaultPlayerCount = _universeSizeXmlReader.NormalDefaultPlayerCount;
                    break;
                case UniverseSize.Large:
                    defaultPlayerCount = _universeSizeXmlReader.LargeDefaultPlayerCount;
                    break;
                case UniverseSize.Enormous:
                    defaultPlayerCount = _universeSizeXmlReader.EnormousDefaultPlayerCount;
                    break;
                case UniverseSize.Gigantic:
                    defaultPlayerCount = _universeSizeXmlReader.GiganticDefaultPlayerCount;
                    break;
                case UniverseSize.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(universeSize));
            }
            return defaultPlayerCount;
        }

        #endregion

        #region ResourceID

        private static ResourceIDXmlPropertyReader _resourceIDXmlReader = ResourceIDXmlPropertyReader.Instance;

        public static string GetImageFilename(this ResourceID resourceID) {
            switch (resourceID) {
                case ResourceID.Organics:
                    return _resourceIDXmlReader.OrganicsImageFilename;
                case ResourceID.Particulates:
                    return _resourceIDXmlReader.ParticulatesImageFilename;
                case ResourceID.Energy:
                    return _resourceIDXmlReader.EnergyImageFilename;
                case ResourceID.Titanium:
                    return _resourceIDXmlReader.TitaniumImageFilename;
                case ResourceID.Duranium:
                    return _resourceIDXmlReader.DuraniumImageFilename;
                case ResourceID.Unobtanium:
                    return _resourceIDXmlReader.UnobtaniumImageFilename;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(resourceID));
            }
        }

        public static AtlasID GetImageAtlasID(this ResourceID resourceID) {
            switch (resourceID) {
                case ResourceID.Organics:
                    return Enums<AtlasID>.Parse(_resourceIDXmlReader.OrganicsImageAtlasID);
                case ResourceID.Particulates:
                    return Enums<AtlasID>.Parse(_resourceIDXmlReader.ParticulatesImageAtlasID);
                case ResourceID.Energy:
                    return Enums<AtlasID>.Parse(_resourceIDXmlReader.EnergyImageAtlasID);
                case ResourceID.Titanium:
                    return Enums<AtlasID>.Parse(_resourceIDXmlReader.TitaniumImageAtlasID);
                case ResourceID.Duranium:
                    return Enums<AtlasID>.Parse(_resourceIDXmlReader.DuraniumImageAtlasID);
                case ResourceID.Unobtanium:
                    return Enums<AtlasID>.Parse(_resourceIDXmlReader.UnobtaniumImageAtlasID);
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(resourceID));
            }
        }

        public static string GetIconFilename(this ResourceID resourceID) {
            switch (resourceID) {
                case ResourceID.Organics:
                    return _resourceIDXmlReader.OrganicsIconFilename;
                case ResourceID.Particulates:
                    return _resourceIDXmlReader.ParticulatesIconFilename;
                case ResourceID.Energy:
                    return _resourceIDXmlReader.EnergyIconFilename;
                case ResourceID.Titanium:
                    return _resourceIDXmlReader.TitaniumIconFilename;
                case ResourceID.Duranium:
                    return _resourceIDXmlReader.DuraniumIconFilename;
                case ResourceID.Unobtanium:
                    return _resourceIDXmlReader.UnobtaniumIconFilename;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(resourceID));
            }
        }

        public static AtlasID GetIconAtlasID(this ResourceID resourceID) {
            switch (resourceID) {
                case ResourceID.Organics:
                    return Enums<AtlasID>.Parse(_resourceIDXmlReader.OrganicsIconAtlasID);
                case ResourceID.Particulates:
                    return Enums<AtlasID>.Parse(_resourceIDXmlReader.ParticulatesIconAtlasID);
                case ResourceID.Energy:
                    return Enums<AtlasID>.Parse(_resourceIDXmlReader.EnergyIconAtlasID);
                case ResourceID.Titanium:
                    return Enums<AtlasID>.Parse(_resourceIDXmlReader.TitaniumIconAtlasID);
                case ResourceID.Duranium:
                    return Enums<AtlasID>.Parse(_resourceIDXmlReader.DuraniumIconAtlasID);
                case ResourceID.Unobtanium:
                    return Enums<AtlasID>.Parse(_resourceIDXmlReader.UnobtaniumIconAtlasID);
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(resourceID));
            }
        }

        public static string GetResourceDescription(this ResourceID resourceID) {
            switch (resourceID) {
                case ResourceID.Organics:
                    return _resourceIDXmlReader.OrganicsDescription;
                case ResourceID.Particulates:
                    return _resourceIDXmlReader.ParticulatesDescription;
                case ResourceID.Energy:
                    return _resourceIDXmlReader.EnergyDescription;
                case ResourceID.Titanium:
                    return _resourceIDXmlReader.TitaniumDescription;
                case ResourceID.Duranium:
                    return _resourceIDXmlReader.DuraniumDescription;
                case ResourceID.Unobtanium:
                    return _resourceIDXmlReader.UnobtaniumDescription;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(resourceID));
            }
        }

        public static ResourceCategory GetResourceCategory(this ResourceID resourceID) {
            // currently don't see a reason to externalize this
            switch (resourceID) {
                case ResourceID.Organics:
                case ResourceID.Particulates:
                case ResourceID.Energy:
                    return ResourceCategory.Common;
                case ResourceID.Titanium:
                case ResourceID.Duranium:
                case ResourceID.Unobtanium:
                    return ResourceCategory.Strategic;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(resourceID));
            }
        }

        #endregion

        #region GameSpeed

        private static GameSpeedXmlPropertyReader _gameSpeedXmlReader = GameSpeedXmlPropertyReader.Instance;

        public static float SpeedMultiplier(this GameSpeed gameSpeed) {
            switch (gameSpeed) {
                case GameSpeed.Slowest:
                    return _gameSpeedXmlReader.SlowestMultiplier;
                case GameSpeed.Slow:
                    return _gameSpeedXmlReader.SlowMultiplier;
                case GameSpeed.Normal:
                    return _gameSpeedXmlReader.NormalMultiplier;
                case GameSpeed.Fast:
                    return _gameSpeedXmlReader.FastMultiplier;
                case GameSpeed.Fastest:
                    return _gameSpeedXmlReader.FastestMultiplier;
                case GameSpeed.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(gameSpeed));
            }
        }


        #endregion

        //TODO The following have not yet externalized to XML

        #region Element Category

        //TODO MaxMountPoints and Dimensions are temporary until I determine whether I want either value
        // to be independant of the element category. If independant, the values would then be placed
        // in the HullStat.

        public static int __MaxLOSWeapons(this ShipHullCategory cat) {
            int value = 0;
            switch (cat) {
                case ShipHullCategory.Fighter:
                case ShipHullCategory.Scout:
                    value = 1;
                    break;
                case ShipHullCategory.Science:
                case ShipHullCategory.Support:
                case ShipHullCategory.Colonizer:
                case ShipHullCategory.Troop:
                    value = 2;
                    break;
                case ShipHullCategory.Frigate:
                    value = 3;
                    break;
                case ShipHullCategory.Destroyer:
                case ShipHullCategory.Carrier:
                    value = 4;
                    break;
                case ShipHullCategory.Cruiser:
                    value = 7;
                    break;
                case ShipHullCategory.Dreadnought:
                    value = TempGameValues.MaxLosWeaponsForAnyElement;   // 12
                    break;
                case ShipHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(cat));
            }
            D.Assert(value <= TempGameValues.MaxLosWeaponsForAnyElement);
            return value;
        }

        public static int __MaxMissileWeapons(this ShipHullCategory cat) {
            int value = 0;
            switch (cat) {
                case ShipHullCategory.Fighter:
                case ShipHullCategory.Scout:
                    break;
                case ShipHullCategory.Science:
                case ShipHullCategory.Support:
                case ShipHullCategory.Colonizer:
                case ShipHullCategory.Troop:
                    value = 1;
                    break;
                case ShipHullCategory.Frigate:
                    value = 2;
                    break;
                case ShipHullCategory.Destroyer:
                case ShipHullCategory.Carrier:
                    value = 3;
                    break;
                case ShipHullCategory.Cruiser:
                    value = 4;
                    break;
                case ShipHullCategory.Dreadnought:
                    value = TempGameValues.MaxMissileWeaponsForAnyElement;   // 6
                    break;
                case ShipHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(cat));
            }
            D.Assert(value <= TempGameValues.MaxMissileWeaponsForAnyElement);
            return value;
        }

        [System.Obsolete]
        public static Vector3 __HullDimensions(this ShipHullCategory cat) {
            switch (cat) {
                case ShipHullCategory.Frigate:  // 10.28.15 Hull collider dimensions increased to encompass turrets
                    //return new Vector3(.03F, .01F, .05F);
                    return new Vector3(.04F, .035F, .10F);
                case ShipHullCategory.Destroyer:
                case ShipHullCategory.Support:
                    //return new Vector3(.05F, .02F, .10F);
                    return new Vector3(.08F, .05F, .18F);
                case ShipHullCategory.Cruiser:
                case ShipHullCategory.Science:
                case ShipHullCategory.Colonizer:
                    //return new Vector3(.07F, .04F, .15F);
                    return new Vector3(.15F, .08F, .30F);
                case ShipHullCategory.Dreadnought:
                case ShipHullCategory.Troop:
                    //return new Vector3(.10F, .06F, .25F);
                    return new Vector3(.21F, .07F, .45F);
                case ShipHullCategory.Carrier:
                    //return new Vector3(.12F, .06F, .35F);
                    return new Vector3(.20F, .10F, .60F);
                case ShipHullCategory.Fighter:
                case ShipHullCategory.Scout:
                case ShipHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(cat));
            }
        }

        public static int __MaxLOSWeapons(this FacilityHullCategory cat) {
            int value = 0;
            switch (cat) {
                case FacilityHullCategory.Economic:
                case FacilityHullCategory.Factory:
                case FacilityHullCategory.Laboratory:
                case FacilityHullCategory.Barracks:
                case FacilityHullCategory.ColonyHab:
                    value = 2;
                    break;
                case FacilityHullCategory.CentralHub:
                case FacilityHullCategory.Defense:
                    value = TempGameValues.MaxLosWeaponsForAnyElement;   // 12
                    break;
                case FacilityHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(cat));
            }
            D.Assert(value <= TempGameValues.MaxLosWeaponsForAnyElement);
            return value;
        }

        public static int __MaxMissileWeapons(this FacilityHullCategory cat) {
            int value = 0;
            switch (cat) {
                case FacilityHullCategory.Economic:
                case FacilityHullCategory.Factory:
                case FacilityHullCategory.Laboratory:
                case FacilityHullCategory.ColonyHab:
                case FacilityHullCategory.Barracks:
                    value = 2;
                    break;
                case FacilityHullCategory.CentralHub:
                case FacilityHullCategory.Defense:
                    value = TempGameValues.MaxMissileWeaponsForAnyElement;
                    break;
                case FacilityHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(cat));
            }
            D.Assert(value <= TempGameValues.MaxMissileWeaponsForAnyElement);
            return value;
        }

        #endregion

        #region Gui Menu ElementID

        /// <summary>
        /// Returns the PlayerPrefs Property Name associated with this GuiMenuElementID.
        /// Returns null if the elementID has no PlayerPrefs Property associated with it.
        /// </summary>
        /// <param name="elementID">The ID for this Gui element.</param>
        /// <returns></returns>
        public static string PreferencePropertyName(this GuiElementID elementID) {
            switch (elementID) {
                case GuiElementID.CameraRollCheckbox:
                    return "IsCameraRollEnabled";
                case GuiElementID.ElementIconsCheckbox:
                    return "IsElementIconsEnabled";
                case GuiElementID.PauseOnLoadCheckbox:
                    return "IsPauseOnLoadEnabled";
                case GuiElementID.ResetOnFocusCheckbox:
                    return "IsResetOnFocusEnabled";
                case GuiElementID.ZoomOutOnCursorCheckbox:
                    return "IsZoomOutOnCursorEnabled";
                case GuiElementID.GameSpeedOnLoadPopupList:
                    return "GameSpeedOnLoad";
                case GuiElementID.QualitySettingPopupList:
                    return "QualitySetting";
                case GuiElementID.UniverseSizePopupList:
                    return "UniverseSizeSelection";
                case GuiElementID.PlayerCountPopupList:
                    return "PlayerCount";

                case GuiElementID.UserPlayerSpeciesPopupList:
                    return "UserPlayerSpeciesSelection";
                case GuiElementID.AIPlayer1SpeciesPopupList:
                    return "AIPlayer1SpeciesSelection";
                case GuiElementID.AIPlayer2SpeciesPopupList:
                    return "AIPlayer2SpeciesSelection";
                case GuiElementID.AIPlayer3SpeciesPopupList:
                    return "AIPlayer3SpeciesSelection";
                case GuiElementID.AIPlayer4SpeciesPopupList:
                    return "AIPlayer4SpeciesSelection";
                case GuiElementID.AIPlayer5SpeciesPopupList:
                    return "AIPlayer5SpeciesSelection";
                case GuiElementID.AIPlayer6SpeciesPopupList:
                    return "AIPlayer6SpeciesSelection";
                case GuiElementID.AIPlayer7SpeciesPopupList:
                    return "AIPlayer7SpeciesSelection";

                case GuiElementID.UserPlayerColorPopupList:
                    return "UserPlayerColor";
                case GuiElementID.AIPlayer1ColorPopupList:
                    return "AIPlayer1Color";
                case GuiElementID.AIPlayer2ColorPopupList:
                    return "AIPlayer2Color";
                case GuiElementID.AIPlayer3ColorPopupList:
                    return "AIPlayer3Color";
                case GuiElementID.AIPlayer4ColorPopupList:
                    return "AIPlayer4Color";
                case GuiElementID.AIPlayer5ColorPopupList:
                    return "AIPlayer5Color";
                case GuiElementID.AIPlayer6ColorPopupList:
                    return "AIPlayer6Color";
                case GuiElementID.AIPlayer7ColorPopupList:
                    return "AIPlayer7Color";

                case GuiElementID.AIPlayer1IQPopupList:
                    return "AIPlayer1IQ";
                case GuiElementID.AIPlayer2IQPopupList:
                    return "AIPlayer2IQ";
                case GuiElementID.AIPlayer3IQPopupList:
                    return "AIPlayer3IQ";
                case GuiElementID.AIPlayer4IQPopupList:
                    return "AIPlayer4IQ";
                case GuiElementID.AIPlayer5IQPopupList:
                    return "AIPlayer5IQ";
                case GuiElementID.AIPlayer6IQPopupList:
                    return "AIPlayer6IQ";
                case GuiElementID.AIPlayer7IQPopupList:
                    return "AIPlayer7IQ";

                default:
                    return null;
            }
        }

        #endregion

        #region Range Category

        /// <summary>
        /// Gets the base weapon range distance (prior to any owner modifiers being applied) for this RangeCategory.
        /// </summary>
        /// <param name="rangeCategory">The weapon range category.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static float GetBaseWeaponRange(this RangeCategory rangeCategory) {
            switch (rangeCategory) {
                case RangeCategory.Short:
                    return 7F;
                case RangeCategory.Medium:
                    return 10F;
                case RangeCategory.Long:
                    return 15F;
                case RangeCategory.None:
                    return Constants.ZeroF;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(rangeCategory));
            }
        }

        /// <summary>
        /// Gets the base sensor range distance spread (prior to any player modifiers being applied) for this RangeCategory.
        /// Allows me to create different distance values for a specific RangeCategory.
        /// </summary>
        /// <param name="rangeCategory">The sensor range category.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        [Obsolete]
        public static ValueRange<float> __GetBaseSensorRangeSpread(this RangeCategory rangeCategory) {
            switch (rangeCategory) {
                case RangeCategory.Short:
                    return new ValueRange<float>(100F, 200F);
                case RangeCategory.Medium:
                    return new ValueRange<float>(500F, 1000F);
                case RangeCategory.Long:
                    return new ValueRange<float>(2000F, 3000F);
                case RangeCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(rangeCategory));
            }
        }

        /// <summary>
        /// Gets the base sensor range distance (prior to any owner modifiers being applied) for this RangeCategory.
        /// </summary>
        /// <param name="rangeCategory">The sensor range category.</param>
        /// <returns></returns>
        public static float GetBaseSensorRange(this RangeCategory rangeCategory) {
            switch (rangeCategory) {
                case RangeCategory.Short:
                    return 150F;
                case RangeCategory.Medium:
                    return 750F;
                case RangeCategory.Long:
                    return 2500F;
                case RangeCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(rangeCategory));
            }
        }

        /// <summary>
        /// Gets the base active countermeasure range distance (prior to any owner modifiers being applied) for this RangeCategory.
        /// </summary>
        /// <param name="rangeCategory">The countermeasure range category.</param>
        /// <returns></returns>
        public static float GetBaseActiveCountermeasureRange(this RangeCategory rangeCategory) {
            return rangeCategory.GetBaseWeaponRange() / 2F;
        }

        /// <summary>
        /// Gets the base shield range distance (prior to any owner modifiers being applied) for this RangeCategory.
        /// </summary>
        /// <param name="rangeCategory">The shield range category.</param>
        /// <returns></returns>
        public static float GetBaseShieldRange(this RangeCategory rangeCategory) {
            return rangeCategory.GetBaseWeaponRange() / 4F;
        }

        #endregion

        #region Ship Speed 

        /// <summary>
        /// Gets the speed in units per hour for this ship moving as part of a fleet.
        /// </summary>
        /// <param name="speed">The speed enum value.</param>
        /// <param name="fleetData">The fleet data.</param>
        /// <returns></returns>
        public static float GetUnitsPerHour(this Speed speed, FleetCmdData fleetData) {
            return GetUnitsPerHour(speed, null, fleetData);
        }

        /// <summary>
        /// Gets the speed in units per hour for this ship moving independantly of its fleet.
        /// </summary>
        /// <param name="speed">The speed enum value.</param>
        /// <param name="shipData">The ship data.</param>
        /// <returns></returns>
        public static float GetUnitsPerHour(this Speed speed, ShipData shipData) {
            return GetUnitsPerHour(speed, shipData, null);
        }

        private static float GetUnitsPerHour(Speed speed, ShipData shipData, FleetCmdData fleetData) {
            // Note: see Flight.txt in GameDev Notes for analysis of speed values
            float fullSpeedFactor = Constants.ZeroF;
            switch (speed) {
                case Speed.HardStop:
                    return Constants.ZeroF;
                case Speed.Stop:
                    return Constants.ZeroF;
                case Speed.ThrustersOnly:
                    // 4.13.16 InSystem, STL = 0.05 but clamped at 0.2 below, OpenSpace, FTL = 1.2
                    fullSpeedFactor = 0.03F;
                    break;
                case Speed.Docking:
                    // 4.9.16 InSystem, STL = 0.08 but clamped at 0.2 below, OpenSpace, FTL = 2
                    fullSpeedFactor = 0.05F;
                    break;
                case Speed.DeadSlow:
                    // 4.9.16 InSystem, STL = 0.13 but clamped at 0.2 below, OpenSpace, FTL = 3.2
                    fullSpeedFactor = 0.08F;
                    break;
                case Speed.Slow:
                    // 4.9.16 InSystem, STL = 0.24, OpenSpace, FTL = 6
                    fullSpeedFactor = 0.15F;
                    break;
                case Speed.OneThird:
                    // 11.24.15 InSystem, STL = 0.4, OpenSpace, FTL = 10
                    fullSpeedFactor = 0.25F;
                    break;
                case Speed.TwoThirds:
                    // 11.24.15 InSystem, STL = 0.8, OpenSpace, FTL = 20
                    fullSpeedFactor = 0.50F;
                    break;
                case Speed.Standard:
                    // 11.24.15 InSystem, STL = 1.2, OpenSpace, FTL = 30
                    fullSpeedFactor = 0.75F;
                    break;
                case Speed.Full:
                    // 11.24.15 InSystem, STL = 1.6, OpenSpace, FTL = 40
                    fullSpeedFactor = 1.0F;
                    break;
                case Speed.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(speed));
            }

            float fullSpeed = Constants.ZeroF;
            if (shipData != null) {
                fullSpeed = shipData.FullSpeedValue; // 11.24.15 InSystem, STL = 1.6, OpenSpace, FTL = 40
                //D.Log("{0}.FullSpeed = {1} units/hour. IsFtlOperational = {2}.", shipData.FullName, fullSpeed, shipData.IsFtlOperational);
            }
            else {
                fullSpeed = fleetData.UnitFullSpeedValue;   // 11.24.15 InSystem, STL = 1.6, OpenSpace, FTL = 40
                //D.Log("{0}.FullSpeed = {1} units/hour.", fleetData.FullName, fullSpeed);
            }

            float speedValueResult = fullSpeedFactor * fullSpeed;
            if (speedValueResult < TempGameValues.ShipMinimumSpeedValue) {
                speedValueResult = TempGameValues.ShipMinimumSpeedValue;
                D.Log("Speed {0} value has been clamped to {1:0.00}.", speed.GetValueName(), speedValueResult);
            }
            return speedValueResult;
        }


        /// <summary>
        /// Tries to decrease the speed by one step below the source speed. Returns
        /// <c>true</c> if successful, <c>false</c> otherwise. Throws an exception if sourceSpeed
        /// represents a speed of value zero - aka no velocity.
        /// </summary>
        /// <param name="sourceSpeed">The source speed.</param>
        /// <param name="newSpeed">The new speed.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static bool TryDecreaseSpeed(this Speed sourceSpeed, out Speed newSpeed) {
            newSpeed = Speed.None;

            switch (sourceSpeed) {
                case Speed.ThrustersOnly:
                    return false;
                case Speed.Docking:
                    newSpeed = Speed.ThrustersOnly;
                    return true;
                case Speed.DeadSlow:
                    newSpeed = Speed.Docking;
                    return true;
                case Speed.Slow:
                    newSpeed = Speed.DeadSlow;
                    return true;
                case Speed.OneThird:
                    newSpeed = Speed.Slow;
                    return true;
                case Speed.TwoThirds:
                    newSpeed = Speed.OneThird;
                    return true;
                case Speed.Standard:
                    newSpeed = Speed.TwoThirds;
                    return true;
                case Speed.Full:
                    newSpeed = Speed.Standard;
                    return true;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(sourceSpeed));
            }
        }

        /// <summary>
        /// Tries to increase the speed by one step above the source speed. Returns
        /// <c>true</c> if successful, <c>false</c> otherwise. Throws an exception if sourceSpeed
        /// represents a speed of value zero - aka no velocity.
        /// </summary>
        /// <param name="sourceSpeed">The source speed.</param>
        /// <param name="newSpeed">The new speed.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static bool TryIncreaseSpeed(this Speed sourceSpeed, out Speed newSpeed) {
            newSpeed = Speed.None;

            switch (sourceSpeed) {
                case Speed.ThrustersOnly:
                    newSpeed = Speed.Docking;
                    return true;
                case Speed.Docking:
                    newSpeed = Speed.DeadSlow;
                    return true;
                case Speed.DeadSlow:
                    newSpeed = Speed.Slow;
                    return true;
                case Speed.Slow:
                    newSpeed = Speed.OneThird;
                    return true;
                case Speed.OneThird:
                    newSpeed = Speed.TwoThirds;
                    return true;
                case Speed.TwoThirds:
                    newSpeed = Speed.Standard;
                    return true;
                case Speed.Standard:
                    newSpeed = Speed.Full;
                    return true;
                case Speed.Full:
                    return false;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(sourceSpeed));
            }
        }

        #endregion

        #region Topography

        /// <summary>
        /// Gets the density of matter in this <c>topography</c> relative to Topography.OpenSpace.
        /// The density of a <c>Topography</c> affects the drag of ships and projectiles moving through it.
        /// </summary>
        /// <param name="topography">The topography.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static float GetRelativeDensity(this Topography topography) {
            switch (topography) {
                case Topography.OpenSpace:
                    return 1F;
                case Topography.System:
                case Topography.Nebula:
                    return 5F;
                case Topography.DeepNebula: //TODO
                case Topography.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(topography));
            }
        }

        #endregion

        /**************************************************************************************************************/

        #region Nested Classes

        /// <summary>
        /// Parses UniverseSize.xml used to provide externalized values for the UniverseSize enum.
        /// </summary>
        private sealed class UniverseSizeXmlPropertyReader : AEnumXmlPropertyReader<UniverseSizeXmlPropertyReader> {

            #region Universe Radius

            private float _tinyRadius;
            public float TinyRadius {
                get {
                    CheckValuesInitialized();
                    return _tinyRadius;
                }
                private set { _tinyRadius = value; }
            }

            private float _smallRadius;
            public float SmallRadius {
                get {
                    CheckValuesInitialized();
                    return _smallRadius;
                }
                private set { _smallRadius = value; }
            }

            private float _normalRadius;
            public float NormalRadius {
                get {
                    CheckValuesInitialized();
                    return _normalRadius;
                }
                private set { _normalRadius = value; }
            }

            private float _largeRadius;
            public float LargeRadius {
                get {
                    CheckValuesInitialized();
                    return _largeRadius;
                }
                private set { _largeRadius = value; }
            }

            private float _enormousRadius;
            public float EnormousRadius {
                get {
                    CheckValuesInitialized();
                    return _enormousRadius;
                }
                private set { _enormousRadius = value; }
            }

            private float _giganticRadius;
            public float GiganticRadius {
                get {
                    CheckValuesInitialized();
                    return _giganticRadius;
                }
                private set { _giganticRadius = value; }
            }

            #endregion


            #region Universe Default Player Count

            private int _tinyDefaultPlayerCount;
            public int TinyDefaultPlayerCount {
                get {
                    CheckValuesInitialized();
                    return _tinyDefaultPlayerCount;
                }
                private set { _tinyDefaultPlayerCount = value; }
            }

            private int _smallDefaultPlayerCount;
            public int SmallDefaultPlayerCount {
                get {
                    CheckValuesInitialized();
                    return _smallDefaultPlayerCount;
                }
                private set { _smallDefaultPlayerCount = value; }
            }

            private int _normalDefaultPlayerCount;
            public int NormalDefaultPlayerCount {
                get {
                    CheckValuesInitialized();
                    return _normalDefaultPlayerCount;
                }
                private set { _normalDefaultPlayerCount = value; }
            }

            private int _largeDefaultPlayerCount;
            public int LargeDefaultPlayerCount {
                get {
                    CheckValuesInitialized();
                    return _largeDefaultPlayerCount;
                }
                private set { _largeDefaultPlayerCount = value; }
            }

            private int _enormousDefaultPlayerCount;
            public int EnormousDefaultPlayerCount {
                get {
                    CheckValuesInitialized();
                    return _enormousDefaultPlayerCount;
                }
                private set { _enormousDefaultPlayerCount = value; }
            }

            private int _giganticDefaultPlayerCount;
            public int GiganticDefaultPlayerCount {
                get {
                    CheckValuesInitialized();
                    return _giganticDefaultPlayerCount;
                }
                private set { _giganticDefaultPlayerCount = value; }
            }

            #endregion

            /// <summary>
            /// The type of the enum being supported by this class.
            /// </summary>
            protected override Type EnumType { get { return typeof(UniverseSize); } }

            private UniverseSizeXmlPropertyReader() {
                Initialize();
            }

            public override string ToString() {
                return new ObjectAnalyzer().ToString(this);
            }
        }


        /// <summary>
        /// Parses ResourceID.xml used to provide externalized values for the ResourceID enum.
        /// </summary>
        private sealed class ResourceIDXmlPropertyReader : AEnumXmlPropertyReader<ResourceIDXmlPropertyReader> {

            #region Organics

            private string _organicsImageFilename;
            public string OrganicsImageFilename {
                get {
                    CheckValuesInitialized();
                    return _organicsImageFilename;
                }
                private set { _organicsImageFilename = value; }
            }

            private string _organicsImageAtlasID;
            public string OrganicsImageAtlasID {
                get {
                    CheckValuesInitialized();
                    return _organicsImageAtlasID;
                }
                private set { _organicsImageAtlasID = value; }
            }

            private string _organicsIconFilename;
            public string OrganicsIconFilename {
                get {
                    CheckValuesInitialized();
                    return _organicsIconFilename;
                }
                private set { _organicsIconFilename = value; }
            }

            private string _organicsIconAtlasID;
            public string OrganicsIconAtlasID {
                get {
                    CheckValuesInitialized();
                    return _organicsIconAtlasID;
                }
                private set { _organicsIconAtlasID = value; }
            }

            private string _organicsDescription;
            public string OrganicsDescription {
                get {
                    CheckValuesInitialized();
                    return _organicsDescription;
                }
                private set { _organicsDescription = value; }
            }

            #endregion

            #region Particulates

            private string _particulatesImageFilename;
            public string ParticulatesImageFilename {
                get {
                    CheckValuesInitialized();
                    return _particulatesImageFilename;
                }
                private set { _particulatesImageFilename = value; }
            }

            private string _particulatesImageAtlasID;
            public string ParticulatesImageAtlasID {
                get {
                    CheckValuesInitialized();
                    return _particulatesImageAtlasID;
                }
                private set { _particulatesImageAtlasID = value; }
            }

            private string _particulatesIconFilename;
            public string ParticulatesIconFilename {
                get {
                    CheckValuesInitialized();
                    return _particulatesIconFilename;
                }
                private set { _particulatesIconFilename = value; }
            }

            private string _particulatesIconAtlasID;
            public string ParticulatesIconAtlasID {
                get {
                    CheckValuesInitialized();
                    return _particulatesIconAtlasID;
                }
                private set { _particulatesIconAtlasID = value; }
            }

            private string _particulatesDescription;
            public string ParticulatesDescription {
                get {
                    CheckValuesInitialized();
                    return _particulatesDescription;
                }
                private set { _particulatesDescription = value; }
            }

            #endregion

            #region Energy

            private string _energyImageFilename;
            public string EnergyImageFilename {
                get {
                    CheckValuesInitialized();
                    return _energyImageFilename;
                }
                private set { _energyImageFilename = value; }
            }

            private string _energyImageAtlasID;
            public string EnergyImageAtlasID {
                get {
                    CheckValuesInitialized();
                    return _energyImageAtlasID;
                }
                private set { _energyImageAtlasID = value; }
            }

            private string _energyIconFilename;
            public string EnergyIconFilename {
                get {
                    CheckValuesInitialized();
                    return _energyIconFilename;
                }
                private set { _energyIconFilename = value; }
            }

            private string _energyIconAtlasID;
            public string EnergyIconAtlasID {
                get {
                    CheckValuesInitialized();
                    return _energyIconAtlasID;
                }
                private set { _energyIconAtlasID = value; }
            }

            private string _energyDescription;
            public string EnergyDescription {
                get {
                    CheckValuesInitialized();
                    return _energyDescription;
                }
                private set { _energyDescription = value; }
            }

            #endregion

            #region Titanium

            private string _titaniumImageFilename;
            public string TitaniumImageFilename {
                get {
                    CheckValuesInitialized();
                    return _titaniumImageFilename;
                }
                private set { _titaniumImageFilename = value; }
            }

            private string _titaniumImageAtlasID;
            public string TitaniumImageAtlasID {
                get {
                    CheckValuesInitialized();
                    return _titaniumImageAtlasID;
                }
                private set { _titaniumImageAtlasID = value; }
            }

            private string _titaniumIconFilename;
            public string TitaniumIconFilename {
                get {
                    CheckValuesInitialized();
                    return _titaniumIconFilename;
                }
                private set { _titaniumIconFilename = value; }
            }

            private string _titaniumIconAtlasID;
            public string TitaniumIconAtlasID {
                get {
                    CheckValuesInitialized();
                    return _titaniumIconAtlasID;
                }
                private set { _titaniumIconAtlasID = value; }
            }

            private string _titaniumDescription;
            public string TitaniumDescription {
                get {
                    CheckValuesInitialized();
                    return _titaniumDescription;
                }
                private set { _titaniumDescription = value; }
            }

            #endregion

            #region Duranium

            private string _duraniumImageFilename;
            public string DuraniumImageFilename {
                get {
                    CheckValuesInitialized();
                    return _duraniumImageFilename;
                }
                private set { _duraniumImageFilename = value; }
            }

            private string _duraniumImageAtlasID;
            public string DuraniumImageAtlasID {
                get {
                    CheckValuesInitialized();
                    return _duraniumImageAtlasID;
                }
                private set { _duraniumImageAtlasID = value; }
            }

            private string _duraniumIconFilename;
            public string DuraniumIconFilename {
                get {
                    CheckValuesInitialized();
                    return _duraniumIconFilename;
                }
                private set { _duraniumIconFilename = value; }
            }

            private string _duraniumIconAtlasID;
            public string DuraniumIconAtlasID {
                get {
                    CheckValuesInitialized();
                    return _duraniumIconAtlasID;
                }
                private set { _duraniumIconAtlasID = value; }
            }

            private string _duraniumDescription;
            public string DuraniumDescription {
                get {
                    CheckValuesInitialized();
                    return _duraniumDescription;
                }
                private set { _duraniumDescription = value; }
            }

            #endregion

            #region Unobtanium

            private string _unobtaniumImageFilename;
            public string UnobtaniumImageFilename {
                get {
                    CheckValuesInitialized();
                    return _unobtaniumImageFilename;
                }
                private set { _unobtaniumImageFilename = value; }
            }

            private string _unobtaniumImageAtlasID;
            public string UnobtaniumImageAtlasID {
                get {
                    CheckValuesInitialized();
                    return _unobtaniumImageAtlasID;
                }
                private set { _unobtaniumImageAtlasID = value; }
            }

            private string _unobtaniumIconFilename;
            public string UnobtaniumIconFilename {
                get {
                    CheckValuesInitialized();
                    return _unobtaniumIconFilename;
                }
                private set { _unobtaniumIconFilename = value; }
            }

            private string _unobtaniumIconAtlasID;
            public string UnobtaniumIconAtlasID {
                get {
                    CheckValuesInitialized();
                    return _unobtaniumIconAtlasID;
                }
                private set { _unobtaniumIconAtlasID = value; }
            }

            private string _unobtaniumDescription;
            public string UnobtaniumDescription {
                get {
                    CheckValuesInitialized();
                    return _unobtaniumDescription;
                }
                private set { _unobtaniumDescription = value; }
            }

            #endregion

            /// <summary>
            /// The type of the enum being supported by this class.
            /// </summary>
            protected override Type EnumType { get { return typeof(ResourceID); } }

            private ResourceIDXmlPropertyReader() {
                Initialize();
            }

            public override string ToString() {
                return new ObjectAnalyzer().ToString(this);
            }

        }




        /// <summary>
        /// Parses GameSpeed.xml used to provide externalized values for the GameSpeed enum.
        /// </summary>
        private sealed class GameSpeedXmlPropertyReader : AEnumXmlPropertyReader<GameSpeedXmlPropertyReader> {

            private float _slowestMultiplier;
            public float SlowestMultiplier {
                get {
                    CheckValuesInitialized();
                    return _slowestMultiplier;
                }
                private set { _slowestMultiplier = value; }
            }

            private float _slowMultiplier;
            public float SlowMultiplier {
                get {
                    CheckValuesInitialized();
                    return _slowMultiplier;
                }
                private set { _slowMultiplier = value; }
            }

            private float _normalMultiplier;
            public float NormalMultiplier {
                get {
                    CheckValuesInitialized();
                    return _normalMultiplier;
                }
                private set { _normalMultiplier = value; }
            }

            private float _fastMultiplier;
            public float FastMultiplier {
                get {
                    CheckValuesInitialized();
                    return _fastMultiplier;
                }
                private set { _fastMultiplier = value; }
            }

            private float _fastestMultiplier;
            public float FastestMultiplier {
                get {
                    CheckValuesInitialized();
                    return _fastestMultiplier;
                }
                private set { _fastestMultiplier = value; }
            }

            /// <summary>
            /// The type of the enum being supported by this class.
            /// </summary>
            protected override Type EnumType { get { return typeof(GameSpeed); } }

            private GameSpeedXmlPropertyReader() {
                Initialize();
            }

            public override string ToString() {
                return new ObjectAnalyzer().ToString(this);
            }

        }

        #endregion

    }
}

