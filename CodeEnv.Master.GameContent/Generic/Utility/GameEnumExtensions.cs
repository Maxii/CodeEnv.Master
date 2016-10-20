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

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    /// <summary>
    /// Extensions for Enums specific to the game. Most values
    /// are acquired from external XML files.
    /// </summary>
    public static class GameEnumExtensions {

        #region New Game - UniverseSize, SystemDensity, PlayerCount, StartLevel, SystemDesirability, PlayerSeparation

        private static UniverseSizeXmlPropertyReader _universeSizeXmlReader = UniverseSizeXmlPropertyReader.Instance;

        /// <summary>
        /// Converts this UniverseSizeGuiSelection value to a UniverseSize value.
        /// </summary>
        /// <param name="guiSelection">The universe size GUI selection.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static UniverseSize Convert(this UniverseSizeGuiSelection guiSelection) {
            switch (guiSelection) {
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
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(guiSelection));
            }
        }

        /// <summary>
        /// Radius of the Universe in Sectors.
        /// </summary>
        /// <param name="universeSize">Size of the universe.</param>
        /// <returns></returns>
        public static int RadiusInSectors(this UniverseSize universeSize) {
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

        /// <summary>
        /// Radius of the Universe in Units.
        /// </summary>
        /// <param name="universeSize">Size of the universe.</param>
        /// <returns></returns>
        public static float Radius(this UniverseSize universeSize) {
            return universeSize.RadiusInSectors() * TempGameValues.SectorSideLength;
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
            D.Assert(defaultPlayerCount <= MaxPlayerCount(universeSize));
            return defaultPlayerCount;
        }

        public static int MaxPlayerCount(this UniverseSize universeSize) {
            int maxPlayerCount;
            switch (universeSize) {
                case UniverseSize.Tiny:
                    maxPlayerCount = _universeSizeXmlReader.TinyMaxPlayerCount;
                    break;
                case UniverseSize.Small:
                    maxPlayerCount = _universeSizeXmlReader.SmallMaxPlayerCount;
                    break;
                case UniverseSize.Normal:
                    maxPlayerCount = _universeSizeXmlReader.NormalMaxPlayerCount;
                    break;
                case UniverseSize.Large:
                    maxPlayerCount = _universeSizeXmlReader.LargeMaxPlayerCount;
                    break;
                case UniverseSize.Enormous:
                    maxPlayerCount = _universeSizeXmlReader.EnormousMaxPlayerCount;
                    break;
                case UniverseSize.Gigantic:
                    maxPlayerCount = _universeSizeXmlReader.GiganticMaxPlayerCount;
                    break;
                case UniverseSize.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(universeSize));
            }
            D.Assert(maxPlayerCount <= TempGameValues.MaxPlayers);
            return maxPlayerCount;
        }

        /// <summary>
        /// Returns the minimum required number of systems that must be present
        /// for this universeSize.
        /// <remarks>Varies from ~6 (Tiny) to ~24 (Gigantic) to allow the maximum number of players
        /// to each start using EmpireStartLevel.Advanced.</remarks>
        /// </summary>
        /// <param name="universeSize">Size of the universe.</param>
        /// <returns></returns>
        public static int MinReqdSystemQty(this UniverseSize universeSize) {
            int maxPlayerCount = universeSize.MaxPlayerCount();
            int maxStartingSystemRqmtPerPlayer = EmpireStartLevel.Advanced.SettlementStartQty();
            return maxPlayerCount * maxStartingSystemRqmtPerPlayer;
        }

        /// <summary>
        /// Returns the percentage of occupiable sectors that should contain a system.
        /// <remarks>10.7.16 Manually tweaked to make sure the density value returned will generate
        /// at least the minimum system requirement for the universe size. This minimum system
        /// requirement is a function of the maximum number of players for a particular UniverseSize,
        /// and the maximum number of settlements per player that would be required if all players
        /// where set to start with EmpireStartLevel.Advanced. See UniverseSize.MinReqdSystemQty() extension below.</remarks>
        /// </summary>
        /// <param name="sysDensity">The system density.</param>
        /// <param name="universeSize">Size of the universe.</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static float SystemsPerSector(this SystemDensity sysDensity, UniverseSize universeSize) {
            float density = Constants.ZeroPercent;
            switch (sysDensity) {
                case SystemDensity.None:
                    break;
                case SystemDensity.Sparse:
                    switch (universeSize) {         // NonPeriphery/Total   Grid        MaxPlayer#      MinSystemRqmt
                        case UniverseSize.Tiny:     // 56/184 sectors       6x6x6       2               6
                            density = 0.12F;        // 6 systems
                            break;
                        case UniverseSize.Small:    // 136/408 sectors      8x8x8       3               9
                            density = 0.08F;        // 10 systems
                            break;
                        case UniverseSize.Normal:   // 624/1256 sectors      12x12x12    5               15
                            density = 0.03F;        // 18 systems
                            break;
                        case UniverseSize.Large:    // 1568/2728 sectors    16x16x16    7               21
                            density = 0.015F;       // 23 systems
                            break;
                        case UniverseSize.Enormous: // 4512/6680 sectors    22x22x22    8               24
                            density = 0.008F;       // 36 systems
                            break;
                        case UniverseSize.Gigantic: // 12112/16176 sectors  30x30x30    8               24
                            density = 0.004F;       // 48 systems
                            break;
                        case UniverseSize.None:
                        default:
                            throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(universeSize));
                    }
                    break;
                case SystemDensity.Low:
                    switch (universeSize) {         // NonPeriphery/Total   Grid        MaxPlayer#      MinSystemRqmt
                        case UniverseSize.Tiny:     // 56/184 sectors       6x6x6       2               6
                            density = 0.17F;        // 9 systems
                            break;
                        case UniverseSize.Small:    // 136/408 sectors      8x8x8       3               9
                            density = 0.10F;        // 13 systems
                            break;
                        case UniverseSize.Normal:   // 624/1256 sectors      12x12x12    5               15
                            density = 0.05F;        // 31 systems
                            break;
                        case UniverseSize.Large:    // 1568/2728 sectors    16x16x16    7               21
                            density = 0.03F;        // 47 systems
                            break;
                        case UniverseSize.Enormous: // 4512/6680 sectors    22x22x22    8               24
                            density = 0.015F;       // 67 systems
                            break;
                        case UniverseSize.Gigantic: // 12112/16176 sectors  30x30x30    8               24
                            density = 0.008F;       // 96 systems
                            break;
                        case UniverseSize.None:
                        default:
                            throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(universeSize));
                    }
                    break;
                case SystemDensity.Normal:
                    switch (universeSize) {         // NonPeriphery/Total   Grid        MaxPlayer#      MinSystemRqmt
                        case UniverseSize.Tiny:     // 56/184 sectors       6x6x6       2               6
                            density = 0.25F;        // 14 systems
                            break;
                        case UniverseSize.Small:    // 136/408 sectors      8x8x8       3               9
                            density = 0.15F;        // 20 systems
                            break;
                        case UniverseSize.Normal:   // 624/1256 sectors      12x12x12    5               15
                            density = 0.08F;        // 49 systems
                            break;
                        case UniverseSize.Large:    // 1568/2728 sectors    16x16x16    7               21
                            density = 0.04F;        // 62 systems
                            break;
                        case UniverseSize.Enormous: // 4512/6680 sectors    22x22x22    8               24
                            density = 0.02F;        // 90 systems
                            break;
                        case UniverseSize.Gigantic: // 12112/16176 sectors  30x30x30    8               24
                            density = 0.012F;       // 145 systems
                            break;
                        case UniverseSize.None:
                        default:
                            throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(universeSize));
                    }
                    break;
                case SystemDensity.High:
                    switch (universeSize) {         // NonPeriphery/Total   Grid        MaxPlayer#      MinSystemRqmt
                        case UniverseSize.Tiny:     // 56/184 sectors       6x6x6       2               6
                            density = 0.33F;        // 18 systems
                            break;
                        case UniverseSize.Small:    // 136/408 sectors      8x8x8       3               9
                            density = 0.25F;        // 34 systems
                            break;
                        case UniverseSize.Normal:   // 624/1256 sectors      12x12x12    5               15
                            density = 0.15F;        // 93 systems
                            break;
                        case UniverseSize.Large:    // 1568/2728 sectors    16x16x16    7               21
                            density = 0.10F;        // 156 systems
                            break;
                        case UniverseSize.Enormous: // 4512/6680 sectors    22x22x22    8               24
                            density = 0.07F;        // 315 systems  (FPS < 20)
                            break;
                        case UniverseSize.Gigantic: // 12112/16176 sectors  30x30x30    8               24
                            density = 0.04F;        // 484 systems  (FPS ~ 15)
                            break;
                        case UniverseSize.None:
                        default:
                            throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(universeSize));
                    }
                    break;
                case SystemDensity.Dense:
                    switch (universeSize) {         // NonPeriphery/Total   Grid        MaxPlayer#      MinSystemRqmt
                        case UniverseSize.Tiny:     // 56/184 sectors       6x6x6       2               6
                            density = 0.40F;        // 22 systems
                            break;
                        case UniverseSize.Small:    // 136/408 sectors      8x8x8       3               9
                            density = 0.35F;        // 47 systems
                            break;
                        case UniverseSize.Normal:   // 624/1256 sectors      12x12x12    5               15
                            density = 0.25F;        // 156 systems
                            break;
                        case UniverseSize.Large:    // 1568/2728 sectors    16x16x16    7               21
                            density = 0.15F;        // 235 systems
                            break;
                        case UniverseSize.Enormous: // 4512/6680 sectors    22x22x22    8               24
                            density = 0.10F;        // 451 systems (FPS ~ 15)
                            break;
                        case UniverseSize.Gigantic: // 12112/16176 sectors  30x30x30    8               24
                            density = 0.06F;        // 726 systems  (FPS ~ 6)
                            break;
                        case UniverseSize.None:
                        default:
                            throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(universeSize));
                    }
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(sysDensity));
            }
            D.Log("{0}: System Density of Universe is {1:0.##}.", typeof(GameEnumExtensions).Name, density);
            return density;
        }

        /// <summary>
        /// Converts this SystemDensityGuiSelection value to a SystemDensity value.
        /// </summary>
        /// <param name="guiSelection">The selection.</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static SystemDensity Convert(this SystemDensityGuiSelection guiSelection) {
            switch (guiSelection) {
                case SystemDensityGuiSelection.Random:
                    return Enums<SystemDensity>.GetRandom(excludeDefault: true);
                case SystemDensityGuiSelection.Sparse:
                    return SystemDensity.Sparse;
                case SystemDensityGuiSelection.Low:
                    return SystemDensity.Low;
                case SystemDensityGuiSelection.Normal:
                    return SystemDensity.Normal;
                case SystemDensityGuiSelection.High:
                    return SystemDensity.High;
                case SystemDensityGuiSelection.Dense:
                    return SystemDensity.Dense;
                case SystemDensityGuiSelection.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(guiSelection));
            }
        }

        /// <summary>
        /// The number of fleets each player starts with.
        /// </summary>
        /// <param name="level">The start level.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static int FleetStartQty(this EmpireStartLevel level) {
            switch (level) {
                case EmpireStartLevel.Early:
                    return 1;
                case EmpireStartLevel.Normal:
                    return 2;
                case EmpireStartLevel.Advanced:
                    return 3;
                case EmpireStartLevel.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(level));
            }
        }

        /// <summary>
        /// The number of settlements each player starts with.
        /// </summary>
        /// <param name="level">The start level.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static int SettlementStartQty(this EmpireStartLevel level) {
            switch (level) {
                case EmpireStartLevel.Early:
                    return 0;
                case EmpireStartLevel.Normal:
                    return 1;
                case EmpireStartLevel.Advanced:
                    return 3;
                case EmpireStartLevel.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(level));
            }
        }

        /// <summary>
        /// The number of starbases each player starts with.
        /// </summary>
        /// <param name="level">The start level.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static int StarbaseStartQty(this EmpireStartLevel level) {
            switch (level) {
                case EmpireStartLevel.Early:
                    return 0;
                case EmpireStartLevel.Normal:
                    return 1;
                case EmpireStartLevel.Advanced:
                    return 2;
                case EmpireStartLevel.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(level));
            }
        }

        public static SystemDesirability Convert(this SystemDesirabilityGuiSelection desirabilitySelection) {
            switch (desirabilitySelection) {
                case SystemDesirabilityGuiSelection.Random:
                    return RandomExtended.Choice(Enums<SystemDesirability>.GetValues(excludeDefault: true));
                case SystemDesirabilityGuiSelection.Desirable:
                    return SystemDesirability.Desirable;
                case SystemDesirabilityGuiSelection.Normal:
                    return SystemDesirability.Normal;
                case SystemDesirabilityGuiSelection.Challenged:
                    return SystemDesirability.Challenged;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(desirabilitySelection));
            }
        }

        public static PlayerSeparation Convert(this PlayerSeparationGuiSelection separationSelection) {
            switch (separationSelection) {
                case PlayerSeparationGuiSelection.Random:
                    return RandomExtended.Choice(Enums<PlayerSeparation>.GetValues(excludeDefault: true));
                case PlayerSeparationGuiSelection.Close:
                    return PlayerSeparation.Close;
                case PlayerSeparationGuiSelection.Normal:
                    return PlayerSeparation.Normal;
                case PlayerSeparationGuiSelection.Distant:
                    return PlayerSeparation.Distant;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(separationSelection));
            }
        }

        /// <summary>
        /// Converts this SpeciesGuiSelection value to a Species value.
        /// </summary>
        /// <param name="speciesSelection">The species selection.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static Species Convert(this SpeciesGuiSelection speciesSelection) {
            switch (speciesSelection) {
                case SpeciesGuiSelection.Random:
                    return Enums<Species>.GetRandom(excludeDefault: true);
                case SpeciesGuiSelection.Human:
                    return Species.Human;
                case SpeciesGuiSelection.Borg:
                    return Species.Borg;
                case SpeciesGuiSelection.Dominion:
                    return Species.Dominion;
                case SpeciesGuiSelection.Klingon:
                    return Species.Klingon;
                case SpeciesGuiSelection.Ferengi:
                    return Species.Ferengi;
                case SpeciesGuiSelection.Romulan:
                    return Species.Romulan;
                case SpeciesGuiSelection.GodLike:
                    return Species.GodLike;
                case SpeciesGuiSelection.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(speciesSelection));
            }
        }

        #endregion

        #region Celestial Category

        public static float Radius(this PlanetoidCategory cat) {
            switch (cat) {
                case PlanetoidCategory.GasGiant:
                    return 5F;
                case PlanetoidCategory.Ice:
                    return 2F;
                case PlanetoidCategory.Moon_001:
                    return 0.2F;
                case PlanetoidCategory.Moon_002:
                    return 0.2F;
                case PlanetoidCategory.Moon_003:
                    return 0.2F;
                case PlanetoidCategory.Moon_004:
                    return 0.5F;
                case PlanetoidCategory.Moon_005:
                    return 1F;
                case PlanetoidCategory.Terrestrial:
                    return 2F;
                case PlanetoidCategory.Volcanic:
                    return 1F;
                case PlanetoidCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(cat));
            }
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

        // TODO The following have not yet externalized to XML

        #region Element Hull Category

        #region Element Hull Category - Weapons

        // TODO MaxMountPoints and Dimensions are temporary until I determine whether I want either value
        // to be independent of the element category. If independent, the values would then be placed
        // in the HullStat.

        public static int __MaxLOSWeapons(this ShipHullCategory cat) {
            int value = 0;
            switch (cat) {
                case ShipHullCategory.Fighter:
                case ShipHullCategory.Scout:
                    value = 1;
                    break;
                case ShipHullCategory.Investigator:
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
                case ShipHullCategory.Investigator:
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

        #region Element Hull Category - Physics

        /// <summary>
        /// The dimensions of the hull of the Ship.
        /// </summary>
        /// <param name="hullCat">The category of hull.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static Vector3 Dimensions(this ShipHullCategory hullCat) {
            Vector3 dimensions;
            switch (hullCat) {  // 10.28.15 Hull collider dimensions increased to encompass turrets, 11.20.15 reduced mesh scale from 2 to 1
                case ShipHullCategory.Frigate:
                    dimensions = new Vector3(.02F, .03F, .05F); //new Vector3(.04F, .035F, .10F);
                    break;
                case ShipHullCategory.Destroyer:
                case ShipHullCategory.Support:
                    dimensions = new Vector3(.06F, .035F, .10F);    //new Vector3(.08F, .05F, .18F);
                    break;
                case ShipHullCategory.Cruiser:
                case ShipHullCategory.Investigator:
                case ShipHullCategory.Colonizer:
                    dimensions = new Vector3(.09F, .05F, .16F); //new Vector3(.15F, .08F, .30F); 
                    break;
                case ShipHullCategory.Dreadnought:
                case ShipHullCategory.Troop:
                    dimensions = new Vector3(.12F, .05F, .25F); //new Vector3(.21F, .07F, .45F);
                    break;
                case ShipHullCategory.Carrier:
                    dimensions = new Vector3(.10F, .06F, .32F); // new Vector3(.20F, .10F, .60F); 
                    break;
                case ShipHullCategory.Fighter:
                case ShipHullCategory.Scout:
                case ShipHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
            }
            float radius = dimensions.magnitude / 2F;
            D.Warn(radius > TempGameValues.ShipMaxRadius, "Ship {0}.Radius {1:0.####} > MaxRadius {2:0.##}.", hullCat.GetValueName(), radius, TempGameValues.ShipMaxRadius);
            return dimensions;
        }

        /// <summary>
        /// The dimensions of the hull of the Facility.
        /// </summary>
        /// <param name="hullCat">The category of hull.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static Vector3 Dimensions(this FacilityHullCategory hullCat) {
            Vector3 dimensions;
            switch (hullCat) {
                case FacilityHullCategory.CentralHub:
                case FacilityHullCategory.Defense:
                    dimensions = new Vector3(.4F, .4F, .4F);
                    break;
                case FacilityHullCategory.Economic:
                case FacilityHullCategory.Factory:
                case FacilityHullCategory.Laboratory:
                case FacilityHullCategory.ColonyHab:
                case FacilityHullCategory.Barracks:
                    dimensions = new Vector3(.2F, .2F, .2F);
                    break;
                case FacilityHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
            }
            float radius = dimensions.magnitude / 2F;
            D.Warn(radius > TempGameValues.FacilityMaxRadius, "Facility {0}.Radius {1:0.####} > MaxRadius {2:0.##}.", hullCat.GetValueName(), radius, TempGameValues.FacilityMaxRadius);
            return dimensions;
        }

        /// <summary>
        /// Drag of the ship's hull in Topography.OpenSpace.
        /// </summary>
        /// <param name="hullCat">The hull category.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static float Drag(this ShipHullCategory hullCat) {
            switch (hullCat) {
                case ShipHullCategory.Frigate:
                    return .05F;
                case ShipHullCategory.Destroyer:
                case ShipHullCategory.Support:
                    return .08F;
                case ShipHullCategory.Cruiser:
                case ShipHullCategory.Colonizer:
                case ShipHullCategory.Investigator:
                    return .10F;
                case ShipHullCategory.Dreadnought:
                case ShipHullCategory.Troop:
                    return .15F;
                case ShipHullCategory.Carrier:
                    return .25F;
                case ShipHullCategory.Scout:
                case ShipHullCategory.Fighter:
                case ShipHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
            }
        }

        /// <summary>
        /// Mass of the element's hull.
        /// </summary>
        /// <param name="hullCat">The hull category.</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static float Mass(this FacilityHullCategory hullCat) {
            switch (hullCat) {
                case FacilityHullCategory.CentralHub:
                    return 10000F;
                case FacilityHullCategory.Defense:
                case FacilityHullCategory.Factory:
                    return 5000F;
                case FacilityHullCategory.ColonyHab:
                case FacilityHullCategory.Economic:
                case FacilityHullCategory.Barracks:
                case FacilityHullCategory.Laboratory:
                    return 2000F;
                case FacilityHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
            }
        }

        /// <summary>
        /// Mass of the element's hull.
        /// </summary>
        /// <param name="hullCat">The hull category.</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static float Mass(this ShipHullCategory hullCat) {
            switch (hullCat) {
                case ShipHullCategory.Frigate:
                    return 50F;                     // mass * drag = 2.5
                case ShipHullCategory.Destroyer:
                case ShipHullCategory.Support:
                    return 100F;                     // mass * drag = 8
                case ShipHullCategory.Cruiser:
                case ShipHullCategory.Colonizer:
                case ShipHullCategory.Investigator:
                    return 200F;                     // mass * drag = 20
                case ShipHullCategory.Dreadnought:
                case ShipHullCategory.Troop:
                    return 400F;                     // mass * drag = 60
                case ShipHullCategory.Carrier:
                    return 500F;                     // mass * drag = 125
                case ShipHullCategory.Scout:
                case ShipHullCategory.Fighter:
                case ShipHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
            }
        }

        #endregion

        #region Element Hull Category - Economics

        /// <summary>
        /// Baseline science generated by the element's hull.
        /// <remarks>Not really the hull that generates the science, but what's inside the hull.</remarks>
        /// </summary>
        /// <param name="hullCat">The hull category.</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static float Science(this ShipHullCategory hullCat) {
            switch (hullCat) {
                case ShipHullCategory.Investigator:
                    return 10F;
                case ShipHullCategory.Carrier:
                case ShipHullCategory.Colonizer:
                case ShipHullCategory.Cruiser:
                case ShipHullCategory.Destroyer:
                case ShipHullCategory.Dreadnought:
                case ShipHullCategory.Frigate:
                case ShipHullCategory.Support:
                case ShipHullCategory.Troop:
                    return Constants.ZeroF;
                case ShipHullCategory.Fighter:
                case ShipHullCategory.Scout:
                case ShipHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
            }
        }

        /// <summary>
        /// Baseline culture generated by the element's hull.
        /// <remarks>Not really the hull that generates the culture, but what's inside the hull.</remarks>
        /// </summary>
        /// <param name="hullCat">The hull category.</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static float Culture(this ShipHullCategory hullCat) {
            switch (hullCat) {
                case ShipHullCategory.Colonizer:
                case ShipHullCategory.Support:
                    return 2F;
                case ShipHullCategory.Carrier:
                case ShipHullCategory.Dreadnought:
                case ShipHullCategory.Troop:
                case ShipHullCategory.Cruiser:
                case ShipHullCategory.Investigator:
                case ShipHullCategory.Destroyer:
                case ShipHullCategory.Frigate:
                    return Constants.ZeroF;
                case ShipHullCategory.Fighter:
                case ShipHullCategory.Scout:
                case ShipHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
            }
        }


        /// <summary>
        /// Baseline income generated by the element's hull.
        /// <remarks>Not really the hull that generates the income, but what's inside the hull.</remarks>
        /// </summary>
        /// <param name="hullCat">The hull category.</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static float Income(this ShipHullCategory hullCat) {
            switch (hullCat) {
                case ShipHullCategory.Support:
                    return 3F;
                case ShipHullCategory.Carrier:
                case ShipHullCategory.Colonizer:
                case ShipHullCategory.Cruiser:
                case ShipHullCategory.Destroyer:
                case ShipHullCategory.Dreadnought:
                case ShipHullCategory.Frigate:
                case ShipHullCategory.Investigator:
                case ShipHullCategory.Troop:
                    return Constants.ZeroF;
                case ShipHullCategory.Fighter:
                case ShipHullCategory.Scout:
                case ShipHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
            }
        }

        /// <summary>
        /// Baseline expense to maintain the element's hull.
        /// </summary>
        /// <param name="hullCat">The hull category.</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static float Expense(this ShipHullCategory hullCat) {
            switch (hullCat) {
                case ShipHullCategory.Carrier:
                case ShipHullCategory.Dreadnought:
                case ShipHullCategory.Troop:
                case ShipHullCategory.Colonizer:
                    return 5F;
                case ShipHullCategory.Cruiser:
                case ShipHullCategory.Support:
                case ShipHullCategory.Investigator:
                    return 3F;
                case ShipHullCategory.Destroyer:
                    return 2F;
                case ShipHullCategory.Frigate:
                    return 1F;
                case ShipHullCategory.Fighter:
                case ShipHullCategory.Scout:
                case ShipHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
            }
        }

        public static float Income(this FacilityHullCategory hullCat, bool _isSettlement) {
            float factor = _isSettlement ? 2.0F : 1.0F;
            float result;
            switch (hullCat) {
                case FacilityHullCategory.CentralHub:
                    result = 5F;
                    break;
                case FacilityHullCategory.Economic:
                    result = 20F;
                    break;
                case FacilityHullCategory.Barracks:
                case FacilityHullCategory.ColonyHab:
                case FacilityHullCategory.Defense:
                case FacilityHullCategory.Factory:
                case FacilityHullCategory.Laboratory:
                    result = Constants.ZeroF;
                    break;
                case FacilityHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
            }
            return result * factor;
        }

        public static float Expense(this FacilityHullCategory hullCat, bool _isSettlement) {
            float factor = _isSettlement ? 2.0F : 1.0F;
            float result;
            switch (hullCat) {
                case FacilityHullCategory.CentralHub:
                case FacilityHullCategory.Economic:
                    result = Constants.ZeroF;
                    break;
                case FacilityHullCategory.Barracks:
                case FacilityHullCategory.ColonyHab:
                    result = 3F;
                    break;
                case FacilityHullCategory.Defense:
                case FacilityHullCategory.Factory:
                case FacilityHullCategory.Laboratory:
                    result = 5F;
                    break;
                case FacilityHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
            }
            return result * factor;
        }

        public static float Science(this FacilityHullCategory hullCat, bool _isSettlement) {
            float factor = _isSettlement ? 2.0F : 1.0F;
            float result;
            switch (hullCat) {
                case FacilityHullCategory.Laboratory:
                    result = 10F;
                    break;
                case FacilityHullCategory.CentralHub:
                case FacilityHullCategory.Economic:
                case FacilityHullCategory.Barracks:
                case FacilityHullCategory.ColonyHab:
                case FacilityHullCategory.Defense:
                case FacilityHullCategory.Factory:
                    result = Constants.ZeroF;
                    break;
                case FacilityHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
            }
            return result * factor;
        }

        public static float Culture(this FacilityHullCategory hullCat, bool _isSettlement) {
            float factor = _isSettlement ? 2.0F : 1.0F;
            float result;
            switch (hullCat) {
                case FacilityHullCategory.CentralHub:
                case FacilityHullCategory.ColonyHab:
                    result = 3F;
                    break;
                case FacilityHullCategory.Economic:
                case FacilityHullCategory.Barracks:
                case FacilityHullCategory.Defense:
                case FacilityHullCategory.Factory:
                case FacilityHullCategory.Laboratory:
                    result = Constants.ZeroF;
                    break;
                case FacilityHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
            }
            return result * factor;
        }

        #endregion

        public static Priority __HQPriority(this FacilityHullCategory cat) {
            switch (cat) {
                case FacilityHullCategory.CentralHub:
                    return Priority.Primary;
                case FacilityHullCategory.Defense:
                    return Priority.Secondary;
                case FacilityHullCategory.ColonyHab:
                case FacilityHullCategory.Economic:
                case FacilityHullCategory.Factory:
                case FacilityHullCategory.Barracks:
                case FacilityHullCategory.Laboratory:
                    return Priority.Low;
                case FacilityHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(cat));
            }
        }

        public static Priority __HQPriority(this ShipHullCategory cat) {
            switch (cat) {
                case ShipHullCategory.Carrier:
                    return Priority.Primary;
                case ShipHullCategory.Troop:
                case ShipHullCategory.Dreadnought:
                    return Priority.Secondary;
                case ShipHullCategory.Cruiser:
                case ShipHullCategory.Colonizer:
                    return Priority.Tertiary;
                case ShipHullCategory.Support:
                case ShipHullCategory.Investigator:
                case ShipHullCategory.Destroyer:
                case ShipHullCategory.Frigate:
                    return Priority.Low;
                case ShipHullCategory.Fighter:
                case ShipHullCategory.Scout:
                case ShipHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(cat));
            }
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
                case GuiElementID.SystemDensityPopupList:
                    return "SystemDensitySelection";

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

                case GuiElementID.UserPlayerTeamPopupList:
                    return "UserPlayerTeam";
                case GuiElementID.AIPlayer1TeamPopupList:
                    return "AIPlayer1Team";
                case GuiElementID.AIPlayer2TeamPopupList:
                    return "AIPlayer2Team";
                case GuiElementID.AIPlayer3TeamPopupList:
                    return "AIPlayer3Team";
                case GuiElementID.AIPlayer4TeamPopupList:
                    return "AIPlayer4Team";
                case GuiElementID.AIPlayer5TeamPopupList:
                    return "AIPlayer5Team";
                case GuiElementID.AIPlayer6TeamPopupList:
                    return "AIPlayer6Team";
                case GuiElementID.AIPlayer7TeamPopupList:
                    return "AIPlayer7Team";

                case GuiElementID.PlayerCountPopupList:
                default:
                    if (elementID != GuiElementID.SavedGamesPopupList) { // TODO Not currently implemented
                        D.Warn("{0}: No Property Name found for {1}.{2}.", typeof(GameEnumExtensions).Name, typeof(GuiElementID).Name, elementID.GetValueName());
                    }
                    return null;
            }
        }

        #endregion

        #region Range Category

        /// <summary>
        /// Gets the baseline weapon range distance (prior to any owner modifiers being applied) for this RangeCategory.
        /// </summary>
        /// <param name="rangeCategory">The weapon range category.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static float GetBaselineWeaponRange(this RangeCategory rangeCategory) {
            float range = Constants.ZeroF;
            switch (rangeCategory) {
                case RangeCategory.Short:
                    range = 7F;
                    break;
                case RangeCategory.Medium:
                    range = 10F;
                    break;
                case RangeCategory.Long:
                    range = 15F;
                    break;
                case RangeCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(rangeCategory));
            }
            return range;
        }

        /// <summary>
        /// Gets the baseline sensor range distance spread (prior to any player modifiers being applied) for this RangeCategory.
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
        /// Gets the baseline sensor range distance (prior to any owner modifiers being applied) for this RangeCategory.
        /// </summary>
        /// <param name="rangeCategory">The sensor range category.</param>
        /// <returns></returns>
        public static float GetBaselineSensorRange(this RangeCategory rangeCategory) {
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
        /// Gets the baseline active countermeasure range distance (prior to any owner modifiers being applied) for this RangeCategory.
        /// </summary>
        /// <param name="rangeCategory">The countermeasure range category.</param>
        /// <returns></returns>
        public static float GetBaselineActiveCountermeasureRange(this RangeCategory rangeCategory) {
            return rangeCategory.GetBaselineWeaponRange() / 2F;
        }

        /// <summary>
        /// Gets the baseline shield range distance (prior to any owner modifiers being applied) for this RangeCategory.
        /// </summary>
        /// <param name="rangeCategory">The shield range category.</param>
        /// <returns></returns>
        public static float GetBaselineShieldRange(this RangeCategory rangeCategory) {
            return rangeCategory.GetBaselineWeaponRange() / 4F;
        }

        #endregion

        #region Ship and Fleet Speed 

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
        /// Gets the speed in units per hour for this ship moving independently of its fleet.
        /// </summary>
        /// <param name="speed">The speed enum value.</param>
        /// <param name="shipData">The ship data.</param>
        /// <returns></returns>
        public static float GetUnitsPerHour(this Speed speed, ShipData shipData) {
            return GetUnitsPerHour(speed, shipData, null);
        }

        private static float GetUnitsPerHour(Speed speed, ShipData shipData, FleetCmdData fleetData) {
            float fullSpeedValue = Constants.ZeroF;
            if (shipData != null) {
                fullSpeedValue = shipData.FullSpeedValue; // 11.24.15 InSystem, STL = 1.6, OpenSpace, FTL = 40
                //D.Log("{0}.FullSpeed = {1} units/hour. IsFtlOperational = {2}.", shipData.FullName, fullSpeedValue, shipData.IsFtlOperational);
            }
            else {
                fullSpeedValue = fleetData.UnitFullSpeedValue;   // 11.24.15 InSystem, STL = 1.6, OpenSpace, FTL = 40
                //D.Log("{0}.FullSpeed = {1} units/hour.", fleetData.FullName, fullSpeedValue);
            }
            return speed.GetUnitsPerHour(fullSpeedValue);
        }

        public static float GetUnitsPerHour(this Speed speed, float fullSpeedValue) {
            // Note: see Flight.txt in GameDev Notes for analysis of speed values
            float fullSpeedFactor = Constants.ZeroF;
            switch (speed) {
                case Speed.HardStop:
                    return Constants.ZeroF;
                case Speed.Stop:
                    return Constants.ZeroF;
                case Speed.ThrustersOnly:
                    // 4.13.16 InSystem, STL = 0.05 but clamped at ~0.2 below, OpenSpace, FTL = 1.2
                    fullSpeedFactor = 0.03F;
                    break;
                case Speed.Docking:
                    // 4.9.16 InSystem, STL = 0.08 but clamped at ~0.2 below, OpenSpace, FTL = 2
                    fullSpeedFactor = 0.05F;
                    break;
                case Speed.DeadSlow:
                    // 4.9.16 InSystem, STL = 0.13 but clamped at ~0.2 below, OpenSpace, FTL = 3.2
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

            float speedValueResult = fullSpeedFactor * fullSpeedValue;
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

        #region DiplomaticRelationship 

        public static bool IsEnemy(this DiplomaticRelationship relation) {
            switch (relation) {
                case DiplomaticRelationship.War:
                case DiplomaticRelationship.ColdWar:
                    return true;
                case DiplomaticRelationship.None:
                case DiplomaticRelationship.Neutral:
                case DiplomaticRelationship.Friendly:
                case DiplomaticRelationship.Alliance:
                case DiplomaticRelationship.Self:
                    return false;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(relation));
            }
        }

        public static bool IsFriendly(this DiplomaticRelationship relation) {
            switch (relation) {
                case DiplomaticRelationship.None:
                case DiplomaticRelationship.Neutral:
                case DiplomaticRelationship.War:
                case DiplomaticRelationship.ColdWar:
                    return false;
                case DiplomaticRelationship.Friendly:
                case DiplomaticRelationship.Alliance:
                case DiplomaticRelationship.Self:
                    return true;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(relation));
            }
        }

        #endregion


        #region Debug Enums

        public static DiplomaticRelationship Convert(this DebugDiploUserRelations debugUserRelations) {
            switch (debugUserRelations) {
                case DebugDiploUserRelations.Alliance:
                    return DiplomaticRelationship.Alliance;
                case DebugDiploUserRelations.Friendly:
                    return DiplomaticRelationship.Friendly;
                case DebugDiploUserRelations.Neutral:
                    return DiplomaticRelationship.Neutral;
                case DebugDiploUserRelations.ColdWar:
                    return DiplomaticRelationship.ColdWar;
                case DebugDiploUserRelations.War:
                    return DiplomaticRelationship.War;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(debugUserRelations));
            }
        }

        public static Formation Convert(this DebugBaseFormation debugFormation) {
            switch (debugFormation) {
                case DebugBaseFormation.Random:
                    return Enums<Formation>.GetRandomExcept(Formation.Wedge, default(Formation));
                case DebugBaseFormation.Globe:
                    return Formation.Globe;
                case DebugBaseFormation.Plane:
                    return Formation.Plane;
                case DebugBaseFormation.Diamond:
                    return Formation.Diamond;
                case DebugBaseFormation.Spread:
                    return Formation.Spread;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(debugFormation));
            }
        }

        public static Formation Convert(this DebugFleetFormation debugFormation) {
            switch (debugFormation) {
                case DebugFleetFormation.Random:
                    return Enums<Formation>.GetRandom(excludeDefault: true);
                case DebugFleetFormation.Globe:
                    return Formation.Globe;
                case DebugFleetFormation.Plane:
                    return Formation.Plane;
                case DebugFleetFormation.Diamond:
                    return Formation.Diamond;
                case DebugFleetFormation.Spread:
                    return Formation.Spread;
                case DebugFleetFormation.Wedge:
                    return Formation.Wedge;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(debugFormation));
            }
        }

        public static UniverseSize Convert(this DebugUniverseSize debugUniverseSize) {
            switch (debugUniverseSize) {
                case DebugUniverseSize.Tiny:
                    return UniverseSize.Tiny;
                case DebugUniverseSize.Small:
                    return UniverseSize.Small;
                case DebugUniverseSize.Normal:
                    return UniverseSize.Normal;
                case DebugUniverseSize.Large:
                    return UniverseSize.Large;
                case DebugUniverseSize.Enormous:
                    return UniverseSize.Enormous;
                case DebugUniverseSize.Gigantic:
                    return UniverseSize.Gigantic;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(debugUniverseSize));
            }
        }

        public static SystemDensity Convert(this DebugSystemDensity debugSystemDensity) {
            switch (debugSystemDensity) {
                case DebugSystemDensity.Sparse:
                    return SystemDensity.Sparse;
                case DebugSystemDensity.Low:
                    return SystemDensity.Low;
                case DebugSystemDensity.Normal:
                    return SystemDensity.Normal;
                case DebugSystemDensity.High:
                    return SystemDensity.High;
                case DebugSystemDensity.Dense:
                    return SystemDensity.Dense;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(debugSystemDensity));
            }
        }

        public static EmpireStartLevel Convert(this DebugEmpireStartLevel debugEmpireStartLevel) {
            switch (debugEmpireStartLevel) {
                case DebugEmpireStartLevel.Early:
                    return EmpireStartLevel.Early;
                case DebugEmpireStartLevel.Normal:
                    return EmpireStartLevel.Normal;
                case DebugEmpireStartLevel.Advanced:
                    return EmpireStartLevel.Advanced;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(debugEmpireStartLevel));
            }
        }

        public static SystemDesirability Convert(this DebugSystemDesirability debugDesirability) {
            switch (debugDesirability) {
                case DebugSystemDesirability.Desirable:
                    return SystemDesirability.Desirable;
                case DebugSystemDesirability.Normal:
                    return SystemDesirability.Normal;
                case DebugSystemDesirability.Challenged:
                    return SystemDesirability.Challenged;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(debugDesirability));
            }
        }

        public static PlayerSeparation Convert(this DebugPlayerSeparation debugSeparation) {
            switch (debugSeparation) {
                case DebugPlayerSeparation.Close:
                    return PlayerSeparation.Close;
                case DebugPlayerSeparation.Normal:
                    return PlayerSeparation.Normal;
                case DebugPlayerSeparation.Distant:
                    return PlayerSeparation.Distant;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(debugSeparation));
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

            private int _tinyRadius;
            public int TinyRadius {
                get {
                    CheckValuesInitialized();
                    return _tinyRadius;
                }
                private set { _tinyRadius = value; }
            }

            private int _smallRadius;
            public int SmallRadius {
                get {
                    CheckValuesInitialized();
                    return _smallRadius;
                }
                private set { _smallRadius = value; }
            }

            private int _normalRadius;
            public int NormalRadius {
                get {
                    CheckValuesInitialized();
                    return _normalRadius;
                }
                private set { _normalRadius = value; }
            }

            private int _largeRadius;
            public int LargeRadius {
                get {
                    CheckValuesInitialized();
                    return _largeRadius;
                }
                private set { _largeRadius = value; }
            }

            private int _enormousRadius;
            public int EnormousRadius {
                get {
                    CheckValuesInitialized();
                    return _enormousRadius;
                }
                private set { _enormousRadius = value; }
            }

            private int _giganticRadius;
            public int GiganticRadius {
                get {
                    CheckValuesInitialized();
                    return _giganticRadius;
                }
                private set { _giganticRadius = value; }
            }


            //private float _tinyRadius;
            //public float TinyRadius {
            //    get {
            //        CheckValuesInitialized();
            //        return _tinyRadius;
            //    }
            //    private set { _tinyRadius = value; }
            //}

            //private float _smallRadius;
            //public float SmallRadius {
            //    get {
            //        CheckValuesInitialized();
            //        return _smallRadius;
            //    }
            //    private set { _smallRadius = value; }
            //}

            //private float _normalRadius;
            //public float NormalRadius {
            //    get {
            //        CheckValuesInitialized();
            //        return _normalRadius;
            //    }
            //    private set { _normalRadius = value; }
            //}

            //private float _largeRadius;
            //public float LargeRadius {
            //    get {
            //        CheckValuesInitialized();
            //        return _largeRadius;
            //    }
            //    private set { _largeRadius = value; }
            //}

            //private float _enormousRadius;
            //public float EnormousRadius {
            //    get {
            //        CheckValuesInitialized();
            //        return _enormousRadius;
            //    }
            //    private set { _enormousRadius = value; }
            //}

            //private float _giganticRadius;
            //public float GiganticRadius {
            //    get {
            //        CheckValuesInitialized();
            //        return _giganticRadius;
            //    }
            //    private set { _giganticRadius = value; }
            //}

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

            #region Universe Max Player Count

            private int _tinyMaxPlayerCount;
            public int TinyMaxPlayerCount {
                get {
                    CheckValuesInitialized();
                    return _tinyMaxPlayerCount;
                }
                private set { _tinyMaxPlayerCount = value; }
            }

            private int _smallMaxPlayerCount;
            public int SmallMaxPlayerCount {
                get {
                    CheckValuesInitialized();
                    return _smallMaxPlayerCount;
                }
                private set { _smallMaxPlayerCount = value; }
            }

            private int _normalMaxPlayerCount;
            public int NormalMaxPlayerCount {
                get {
                    CheckValuesInitialized();
                    return _normalMaxPlayerCount;
                }
                private set { _normalMaxPlayerCount = value; }
            }

            private int _largeMaxPlayerCount;
            public int LargeMaxPlayerCount {
                get {
                    CheckValuesInitialized();
                    return _largeMaxPlayerCount;
                }
                private set { _largeMaxPlayerCount = value; }
            }

            private int _enormousMaxPlayerCount;
            public int EnormousMaxPlayerCount {
                get {
                    CheckValuesInitialized();
                    return _enormousMaxPlayerCount;
                }
                private set { _enormousMaxPlayerCount = value; }
            }

            private int _giganticMaxPlayerCount;
            public int GiganticMaxPlayerCount {
                get {
                    CheckValuesInitialized();
                    return _giganticMaxPlayerCount;
                }
                private set { _giganticMaxPlayerCount = value; }
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

