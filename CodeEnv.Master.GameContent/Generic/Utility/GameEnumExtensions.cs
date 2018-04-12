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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    /// <summary>
    /// Extensions for Enums specific to the game. 
    /// <remarks>TODO Externalize all to XML</remarks>
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
                case SystemDesirabilityGuiSelection.None:
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
                case PlayerSeparationGuiSelection.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(separationSelection));
            }
        }

        public static EmpireStartLevel Convert(this EmpireStartLevelGuiSelection startLevelSelection) {
            switch (startLevelSelection) {
                case EmpireStartLevelGuiSelection.Random:
                    return RandomExtended.Choice(Enums<EmpireStartLevel>.GetValues(excludeDefault: true));
                case EmpireStartLevelGuiSelection.Early:
                    return EmpireStartLevel.Early;
                case EmpireStartLevelGuiSelection.Normal:
                    return EmpireStartLevel.Normal;
                case EmpireStartLevelGuiSelection.Advanced:
                    return EmpireStartLevel.Advanced;
                case EmpireStartLevelGuiSelection.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(startLevelSelection));
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

        /// <summary>
        /// Gets the filename of the image (not the icon) of the resource.
        /// </summary>
        /// <param name="id">The resource identifier.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static string GetImageFilename(this ResourceID id) {
            switch (id) {
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
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(id));
            }
        }

        public static AtlasID GetImageAtlasID(this ResourceID id) {
            switch (id) {
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
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(id));
            }
        }

        /// <summary>
        /// Gets the filename of the icon (not the image) of the resource.
        /// </summary>
        /// <param name="id">The resource identifier.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static string GetIconFilename(this ResourceID id) {
            switch (id) {
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
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(id));
            }
        }

        public static AtlasID GetIconAtlasID(this ResourceID id) {
            switch (id) {
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
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(id));
            }
        }

        public static string GetDescription(this ResourceID id) {
            switch (id) {
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
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(id));
            }
        }

        public static ResourceCategory GetResourceCategory(this ResourceID id) {
            // currently don't see a reason to externalize this
            switch (id) {
                case ResourceID.Organics:
                case ResourceID.Particulates:
                case ResourceID.Energy:
                    return ResourceCategory.Common;
                case ResourceID.Titanium:
                case ResourceID.Duranium:
                case ResourceID.Unobtanium:
                    return ResourceCategory.Strategic;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(id));
            }
        }

        #endregion

        #region ResourceCategory

        private static ResourceID[] CommonResourceIDs = { ResourceID.Organics, ResourceID.Particulates, ResourceID.Energy };
        private static ResourceID[] StrategicResourceIDs = { ResourceID.Titanium, ResourceID.Duranium, ResourceID.Unobtanium };
        private static ResourceID[] AllResourceIDs = { ResourceID.Organics, ResourceID.Particulates, ResourceID.Energy, ResourceID.Titanium, ResourceID.Duranium, ResourceID.Unobtanium };

        public static IEnumerable<ResourceID> GetResourceIDs(this ResourceCategory category) {
            switch (category) {
                case ResourceCategory.Common:
                    return CommonResourceIDs;
                case ResourceCategory.Strategic:
                    return StrategicResourceIDs;
                case ResourceCategory.All:
                    return AllResourceIDs;
                case ResourceCategory.Luxury:
                case ResourceCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(category));
            }
        }

        #endregion

        #region OutputID

        // IMPROVE Externalize like ResourcesID

        /// <summary>
        /// Gets the filename of the image (not the icon) of the output.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static string GetImageFilename(this OutputID id) {
            switch (id) {
                case OutputID.Food:
                case OutputID.Production:
                case OutputID.Income:
                case OutputID.Expense:
                case OutputID.NetIncome:
                case OutputID.Science:
                case OutputID.Culture:
                    return TempGameValues.AnImageFilename;
                case OutputID.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(id));
            }
        }

        public static AtlasID GetImageAtlasID(this OutputID id) {
            switch (id) {
                case OutputID.Food:
                case OutputID.Production:
                case OutputID.Income:
                case OutputID.Expense:
                case OutputID.NetIncome:
                case OutputID.Science:
                case OutputID.Culture:
                    return AtlasID.MyGui;
                case OutputID.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(id));
            }
        }

        /// <summary>
        /// Gets the filename of the icon (not the image) of the output.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static string GetIconFilename(this OutputID id) {
            switch (id) {
                case OutputID.Food:
                    return TempGameValues.FoodIconFilename;
                case OutputID.Production:
                    return TempGameValues.ProductionIconFilename;
                case OutputID.Income:
                    return TempGameValues.IncomeIconFilename;
                case OutputID.Expense:
                    return TempGameValues.ExpenseIconFilename;
                case OutputID.NetIncome:
                    return TempGameValues.NetIncomeIconFilename;
                case OutputID.Science:
                    return TempGameValues.ScienceIconFilename;
                case OutputID.Culture:
                    return TempGameValues.CultureIconFilename;
                case OutputID.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(id));
            }
        }

        public static AtlasID GetIconAtlasID(this OutputID id) {
            switch (id) {
                case OutputID.Food:
                case OutputID.Production:
                case OutputID.Income:
                case OutputID.Expense:
                case OutputID.NetIncome:
                case OutputID.Science:
                case OutputID.Culture:
                    return AtlasID.MyGui;
                case OutputID.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(id));
            }
        }

        public static string GetDescription(this OutputID id) {
            switch (id) {
                case OutputID.Food:
                case OutputID.Production:
                case OutputID.Income:
                case OutputID.Expense:
                case OutputID.NetIncome:
                case OutputID.Science:
                case OutputID.Culture:
                    return "An Outputs Description...";
                case OutputID.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(id));
            }
        }

        #endregion

        #region Combat Strength

        public static AtlasID GetImageAtlasID(this WDVCategory category) {
            switch (category) {
                case WDVCategory.Beam:
                case WDVCategory.Projectile:
                case WDVCategory.Missile:
                case WDVCategory.AssaultVehicle:
                    return AtlasID.MyGui;
                case WDVCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(category));
            }
        }

        public static string GetImageFilename(this WDVCategory category) {
            switch (category) {
                case WDVCategory.Beam:
                case WDVCategory.Projectile:
                case WDVCategory.Missile:
                case WDVCategory.AssaultVehicle:
                    return TempGameValues.AnImageFilename;
                case WDVCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(category));
            }
        }

        public static AtlasID GetIconAtlasID(this WDVCategory category) {
            switch (category) {
                case WDVCategory.Beam:
                case WDVCategory.Projectile:
                case WDVCategory.Missile:
                case WDVCategory.AssaultVehicle:
                    return AtlasID.MyGui;
                case WDVCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(category));
            }
        }

        public static string GetIconFilename(this WDVCategory category) {
            switch (category) {
                case WDVCategory.Beam:
                    return TempGameValues.BeamIconFilename;
                case WDVCategory.Projectile:
                    return TempGameValues.ProjectileIconFilename;
                case WDVCategory.Missile:
                    return TempGameValues.MissileIconFilename;
                case WDVCategory.AssaultVehicle:
                    return TempGameValues.AssaultVehicleIconFilename;
                case WDVCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(category));
            }
        }

        public static AtlasID GetIconAtlasID(this CombatStrength.CombatMode combatMode) {
            switch (combatMode) {
                case CombatStrength.CombatMode.Offensive:
                case CombatStrength.CombatMode.Defensive:
                    return AtlasID.MyGui;
                case CombatStrength.CombatMode.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(combatMode));
            }
        }

        public static string GetIconFilename(this CombatStrength.CombatMode combatMode) {
            switch (combatMode) {
                case CombatStrength.CombatMode.Offensive:
                    return TempGameValues.OffensiveStrengthIconFilename;
                case CombatStrength.CombatMode.Defensive:
                    return TempGameValues.DefensiveStrengthIconFilename;
                case CombatStrength.CombatMode.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(combatMode));
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


        #region Unit Directive

        [Obsolete("Icons on Order Directive Buttons currently manually assigned")]
        public static AtlasID __GetAtlasID(this FleetDirective directive) {
            switch (directive) {
                case FleetDirective.Patrol:
                case FleetDirective.Guard:
                    return AtlasID.MyGui;
                case FleetDirective.Move:
                case FleetDirective.FullSpeedMove:
                case FleetDirective.Explore:
                case FleetDirective.Attack:
                case FleetDirective.Refit:
                case FleetDirective.Repair:
                case FleetDirective.Disband:
                case FleetDirective.AssumeFormation:
                case FleetDirective.Regroup:
                case FleetDirective.JoinFleet:
                case FleetDirective.Withdraw:
                case FleetDirective.Retreat:
                case FleetDirective.Scuttle:
                case FleetDirective.ChangeHQ:
                case FleetDirective.Cancel:
                case FleetDirective.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
            }
        }

        [Obsolete("Icons on Order Directive Buttons currently manually assigned")]
        public static string __GetFilename(this FleetDirective directive) {
            switch (directive) {
                case FleetDirective.Patrol:
                case FleetDirective.Guard:
                    return TempGameValues.AnImageFilename;
                case FleetDirective.Move:
                case FleetDirective.FullSpeedMove:
                case FleetDirective.Explore:
                case FleetDirective.Attack:
                case FleetDirective.Refit:
                case FleetDirective.Repair:
                case FleetDirective.Disband:
                case FleetDirective.AssumeFormation:
                case FleetDirective.Regroup:
                case FleetDirective.JoinFleet:
                case FleetDirective.Withdraw:
                case FleetDirective.Retreat:
                case FleetDirective.Scuttle:
                case FleetDirective.ChangeHQ:
                case FleetDirective.Cancel:
                case FleetDirective.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
            }
        }

        [Obsolete("Icons on Order Directive Buttons currently manually assigned")]
        public static AtlasID __GetAtlasID(this BaseDirective directive) {
            switch (directive) {
                case BaseDirective.Attack:
                case BaseDirective.Repair:
                case BaseDirective.Refit:
                case BaseDirective.Disband:
                    return AtlasID.MyGui;
                case BaseDirective.Scuttle:
                case BaseDirective.ChangeHQ:
                case BaseDirective.Cancel:
                case BaseDirective.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
            }
        }

        [Obsolete("Icons on Order Directive Buttons currently manually assigned")]
        public static string __GetFilename(this BaseDirective directive) {
            switch (directive) {
                case BaseDirective.Attack:
                case BaseDirective.Repair:
                case BaseDirective.Refit:
                case BaseDirective.Disband:
                    return TempGameValues.AnImageFilename;
                case BaseDirective.Scuttle:
                case BaseDirective.ChangeHQ:
                case BaseDirective.Cancel:
                case BaseDirective.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(directive));
            }
        }

        #endregion

        #region Element Hull Category

        #region Element Hull Category - Weapons

        // TODO MaxMountPoints and Dimensions are temporary until I determine whether I want either value
        // to be independent of the element category. If independent, the values would then be placed
        // in the HullStat.

        public static int MaxTurretMounts(this ShipHullCategory hullCat) {
            int value = 0;
            switch (hullCat) {
                // WARNING: To change these values, the Hull prefabs must also be changed
                case ShipHullCategory.Frigate:
                    value = 3;
                    break;
                case ShipHullCategory.Destroyer:
                case ShipHullCategory.Carrier:
                case ShipHullCategory.Support:
                    value = 4;
                    break;
                case ShipHullCategory.Cruiser:
                case ShipHullCategory.Colonizer:
                case ShipHullCategory.Investigator:
                    value = 7;
                    break;
                case ShipHullCategory.Dreadnought:
                case ShipHullCategory.Troop:
                    value = TempGameValues.MaxTurretHullMounts;   // 12
                    break;
                case ShipHullCategory.None:
                case ShipHullCategory.Fighter:
                case ShipHullCategory.Scout:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
            }
            D.Assert(value <= TempGameValues.MaxTurretHullMounts);
            return value;
        }

        public static int MaxSiloMounts(this ShipHullCategory hullCat) {
            int value = 0;
            switch (hullCat) {
                // WARNING: To change these values, the Hull prefabs must also be changed
                case ShipHullCategory.Frigate:
                    value = 2;
                    break;
                case ShipHullCategory.Destroyer:
                case ShipHullCategory.Carrier:
                case ShipHullCategory.Support:
                    value = 3;
                    break;
                case ShipHullCategory.Cruiser:
                case ShipHullCategory.Investigator:
                case ShipHullCategory.Colonizer:
                    value = 4;
                    break;
                case ShipHullCategory.Dreadnought:
                case ShipHullCategory.Troop:
                    value = TempGameValues.MaxSiloHullMounts;   // 6
                    break;
                case ShipHullCategory.None:
                case ShipHullCategory.Fighter:
                case ShipHullCategory.Scout:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
            }
            D.Assert(value <= TempGameValues.MaxSiloHullMounts);
            return value;
        }

        public static int MaxTurretMounts(this FacilityHullCategory hullCat) {
            int value = 0;
            switch (hullCat) {
                // WARNING: To change these values, the Hull prefabs must also be changed
                case FacilityHullCategory.Economic:
                case FacilityHullCategory.Factory:
                case FacilityHullCategory.Laboratory:
                case FacilityHullCategory.Barracks:
                case FacilityHullCategory.ColonyHab:
                    value = 2;
                    break;
                case FacilityHullCategory.CentralHub:
                case FacilityHullCategory.Defense:
                    value = TempGameValues.MaxTurretHullMounts;   // 12
                    break;
                case FacilityHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
            }
            D.Assert(value <= TempGameValues.MaxTurretHullMounts);
            return value;
        }

        public static int MaxSiloMounts(this FacilityHullCategory hullCat) {
            int value = 0;
            switch (hullCat) {
                // WARNING: To change these values, the Hull prefabs must also be changed
                case FacilityHullCategory.Economic:
                case FacilityHullCategory.Factory:
                case FacilityHullCategory.Laboratory:
                case FacilityHullCategory.ColonyHab:
                case FacilityHullCategory.Barracks:
                    value = 2;
                    break;
                case FacilityHullCategory.CentralHub:
                case FacilityHullCategory.Defense:
                    value = TempGameValues.MaxSiloHullMounts;  // 6
                    break;
                case FacilityHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
            }
            D.Assert(value <= TempGameValues.MaxSiloHullMounts);
            return value;
        }

        #endregion

        #region Element Hull Category - Equipment

        public static int __MaxSensorMounts(this ShipHullCategory hullCat) {
            int value = 1;  // Reqd SR Sensor
            switch (hullCat) {
                case ShipHullCategory.Support:
                case ShipHullCategory.Frigate:
                    break;
                case ShipHullCategory.Destroyer:
                    value = 2;
                    break;
                case ShipHullCategory.Troop:
                case ShipHullCategory.Colonizer:
                case ShipHullCategory.Cruiser:
                    value = 3;
                    break;
                case ShipHullCategory.Investigator:
                case ShipHullCategory.Carrier:
                case ShipHullCategory.Dreadnought:
                    value = TempGameValues.MaxSensorMounts;   // 4
                    break;
                case ShipHullCategory.Scout:
                case ShipHullCategory.Fighter:
                case ShipHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
            }
            D.Assert(value <= TempGameValues.MaxSensorMounts);
            return value;
        }

        public static int __MaxSkinMounts(this ShipHullCategory hullCat) {
            int value = 0;
            switch (hullCat) {
                case ShipHullCategory.Support:
                case ShipHullCategory.Frigate:
                    value = 2;
                    break;
                case ShipHullCategory.Investigator:
                case ShipHullCategory.Destroyer:
                    value = 3;
                    break;
                case ShipHullCategory.Troop:
                case ShipHullCategory.Colonizer:
                case ShipHullCategory.Cruiser:
                    value = 4;
                    break;
                case ShipHullCategory.Carrier:
                case ShipHullCategory.Dreadnought:
                    value = TempGameValues.MaxSkinMounts;   // 6
                    break;
                case ShipHullCategory.Scout:
                case ShipHullCategory.Fighter:
                case ShipHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
            }
            D.Assert(value <= TempGameValues.MaxSkinMounts);
            return value;
        }

        public static int __MaxScreenMounts(this ShipHullCategory hullCat) {
            int value = 0;
            switch (hullCat) {
                case ShipHullCategory.Support:
                case ShipHullCategory.Frigate:
                    value = 3;
                    break;
                case ShipHullCategory.Destroyer:
                    value = 5;
                    break;
                case ShipHullCategory.Troop:
                case ShipHullCategory.Colonizer:
                case ShipHullCategory.Cruiser:
                    value = 7;
                    break;
                case ShipHullCategory.Investigator:
                case ShipHullCategory.Carrier:
                case ShipHullCategory.Dreadnought:
                    value = TempGameValues.MaxScreenMounts;   // 10
                    break;
                case ShipHullCategory.Scout:
                case ShipHullCategory.Fighter:
                case ShipHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
            }
            D.Assert(value <= TempGameValues.MaxScreenMounts);
            return value;
        }

        public static int __MaxFlexMounts(this ShipHullCategory hullCat) {
            int value = 0;
            switch (hullCat) {
                case ShipHullCategory.Support:
                case ShipHullCategory.Frigate:
                case ShipHullCategory.Destroyer:
                    value = 1;
                    break;
                case ShipHullCategory.Troop:
                case ShipHullCategory.Colonizer:
                case ShipHullCategory.Cruiser:
                    value = 2;
                    break;
                case ShipHullCategory.Investigator:
                case ShipHullCategory.Carrier:
                case ShipHullCategory.Dreadnought:
                    value = TempGameValues.MaxFlexMounts;   // 3
                    break;
                case ShipHullCategory.Scout:
                case ShipHullCategory.Fighter:
                case ShipHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
            }
            D.Assert(value <= TempGameValues.MaxFlexMounts);
            return value;
        }

        /// <summary>
        /// Debug: Supports the auto creation of Ship designs in NewGameUnitConfigurator.
        /// </summary>
        /// <param name="cat">The ShipHullCategory.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static int __MaxPassiveCMs(this ShipHullCategory cat) {
            int value = 0;
            switch (cat) {
                case ShipHullCategory.Fighter:
                case ShipHullCategory.Scout:
                    break;
                case ShipHullCategory.Investigator:
                case ShipHullCategory.Frigate:
                    value = 2;
                    break;
                case ShipHullCategory.Support:
                case ShipHullCategory.Destroyer:
                case ShipHullCategory.Troop:
                    value = 3;
                    break;
                case ShipHullCategory.Colonizer:
                case ShipHullCategory.Cruiser:
                    value = 4;
                    break;
                case ShipHullCategory.Carrier:
                case ShipHullCategory.Dreadnought:
                    value = TempGameValues.__MaxElementPassiveCMs;   // 6
                    break;
                case ShipHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(cat));
            }
            D.Assert(value <= TempGameValues.__MaxElementPassiveCMs);
            return value;
        }

        /// <summary>
        /// Debug: Supports the auto creation of Ship designs in NewGameUnitConfigurator.
        /// </summary>
        /// <param name="cat">The ShipHullCategory.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static int __MaxActiveCMs(this ShipHullCategory cat) {
            int value = 0;
            switch (cat) {
                case ShipHullCategory.Fighter:
                case ShipHullCategory.Scout:
                    break;
                case ShipHullCategory.Investigator:
                case ShipHullCategory.Frigate:
                    value = 2;
                    break;
                case ShipHullCategory.Support:
                case ShipHullCategory.Destroyer:
                case ShipHullCategory.Troop:
                    value = 3;
                    break;
                case ShipHullCategory.Colonizer:
                case ShipHullCategory.Cruiser:
                    value = 4;
                    break;
                case ShipHullCategory.Carrier:
                case ShipHullCategory.Dreadnought:
                    value = TempGameValues.__MaxElementActiveCMs;   // 6
                    break;
                case ShipHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(cat));
            }
            D.Assert(value <= TempGameValues.__MaxElementActiveCMs);
            return value;
        }

        /// <summary>
        /// Debug: Supports the auto creation of Ship designs in NewGameUnitConfigurator.
        /// </summary>
        /// <param name="cat">The ShipHullCategory.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static int __MaxSensors(this ShipHullCategory cat) {
            int value = 1;
            switch (cat) {
                case ShipHullCategory.Fighter:
                case ShipHullCategory.Scout:
                case ShipHullCategory.Support:
                case ShipHullCategory.Frigate:
                    break;
                case ShipHullCategory.Troop:
                case ShipHullCategory.Destroyer:
                    value = 2;
                    break;
                case ShipHullCategory.Colonizer:
                case ShipHullCategory.Cruiser:
                    value = 3;
                    break;
                case ShipHullCategory.Investigator:
                case ShipHullCategory.Carrier:
                case ShipHullCategory.Dreadnought:
                    value = TempGameValues.__MaxElementSensors;   // 4
                    break;
                case ShipHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(cat));
            }
            D.Assert(value <= TempGameValues.__MaxElementSensors);
            return value;
        }

        /// <summary>
        /// Debug: Supports the auto creation of Ship designs in NewGameUnitConfigurator.
        /// </summary>
        /// <param name="cat">The ShipHullCategory.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static int __MaxShieldGenerators(this ShipHullCategory cat) {
            int value = 0;
            switch (cat) {
                case ShipHullCategory.Fighter:
                case ShipHullCategory.Scout:
                    break;
                case ShipHullCategory.Support:
                case ShipHullCategory.Frigate:
                    value = 1;
                    break;
                case ShipHullCategory.Destroyer:
                    value = 2;
                    break;
                case ShipHullCategory.Investigator:
                case ShipHullCategory.Colonizer:
                case ShipHullCategory.Troop:
                case ShipHullCategory.Cruiser:
                    value = 3;
                    break;
                case ShipHullCategory.Carrier:
                case ShipHullCategory.Dreadnought:
                    value = TempGameValues.__MaxElementShieldGenerators;   // 4
                    break;
                case ShipHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(cat));
            }
            D.Assert(value <= TempGameValues.__MaxElementShieldGenerators);
            return value;
        }

        public static int __MaxSensorMounts(this FacilityHullCategory hullCat) {
            int value = 1;  // Reqd SR Sensor
            switch (hullCat) {
                case FacilityHullCategory.Economic:
                case FacilityHullCategory.Factory:
                case FacilityHullCategory.Barracks:
                    break;
                case FacilityHullCategory.ColonyHab:
                    value = 2;
                    break;
                case FacilityHullCategory.Laboratory:
                case FacilityHullCategory.Defense:
                    value = 3;
                    break;
                case FacilityHullCategory.CentralHub:
                    value = TempGameValues.MaxSensorMounts;   // 4
                    break;
                case FacilityHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
            }
            D.Assert(value <= TempGameValues.MaxSensorMounts);
            return value;
        }

        public static int __MaxSkinMounts(this FacilityHullCategory hullCat) {
            int value = 0;
            switch (hullCat) {
                case FacilityHullCategory.Economic:
                case FacilityHullCategory.Laboratory:
                    value = 2;
                    break;
                case FacilityHullCategory.Factory:
                case FacilityHullCategory.ColonyHab:
                    value = 3;
                    break;
                case FacilityHullCategory.Barracks:
                    value = 4;
                    break;
                case FacilityHullCategory.Defense:
                    value = 5;
                    break;
                case FacilityHullCategory.CentralHub:
                    value = TempGameValues.MaxSkinMounts;   // 6
                    break;
                case FacilityHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
            }
            D.Assert(value <= TempGameValues.MaxSkinMounts);
            return value;
        }

        public static int __MaxScreenMounts(this FacilityHullCategory hullCat) {
            int value = 0;
            switch (hullCat) {
                case FacilityHullCategory.Economic:
                case FacilityHullCategory.Laboratory:
                    value = 5;
                    break;
                case FacilityHullCategory.Factory:
                case FacilityHullCategory.Barracks:
                case FacilityHullCategory.ColonyHab:
                    value = 7;
                    break;
                case FacilityHullCategory.Defense:
                    value = 8;
                    break;
                case FacilityHullCategory.CentralHub:
                    value = TempGameValues.MaxScreenMounts;   // 10
                    break;
                case FacilityHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
            }
            D.Assert(value <= TempGameValues.MaxScreenMounts);
            return value;
        }

        public static int __MaxFlexMounts(this FacilityHullCategory hullCat) {
            int value = 0;
            switch (hullCat) {
                case FacilityHullCategory.Economic:
                case FacilityHullCategory.Laboratory:
                    value = 1;
                    break;
                case FacilityHullCategory.Factory:
                case FacilityHullCategory.Barracks:
                case FacilityHullCategory.ColonyHab:
                    value = 2;
                    break;
                case FacilityHullCategory.Defense:
                case FacilityHullCategory.CentralHub:
                    value = TempGameValues.MaxFlexMounts;   // 3
                    break;
                case FacilityHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
            }
            D.Assert(value <= TempGameValues.MaxFlexMounts);
            return value;
        }

        /// <summary>
        /// Debug: Supports the auto creation of Facility designs in NewGameUnitConfigurator.
        /// </summary>
        /// <param name="cat">The FacilityHullCategory.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static int __MaxPassiveCMs(this FacilityHullCategory cat) {
            int value = 0;
            switch (cat) {
                case FacilityHullCategory.Economic:
                case FacilityHullCategory.Factory:
                case FacilityHullCategory.Laboratory:
                case FacilityHullCategory.Barracks:
                case FacilityHullCategory.ColonyHab:
                    value = 3;
                    break;
                case FacilityHullCategory.CentralHub:
                case FacilityHullCategory.Defense:
                    value = TempGameValues.__MaxElementPassiveCMs;   // 6
                    break;
                case FacilityHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(cat));
            }
            D.Assert(value <= TempGameValues.__MaxElementPassiveCMs);
            return value;
        }

        /// <summary>
        /// Debug: Supports the auto creation of Facility designs in NewGameUnitConfigurator.
        /// </summary>
        /// <param name="cat">The FacilityHullCategory.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static int __MaxActiveCMs(this FacilityHullCategory cat) {
            int value = 0;
            switch (cat) {
                case FacilityHullCategory.Economic:
                case FacilityHullCategory.Factory:
                case FacilityHullCategory.Laboratory:
                case FacilityHullCategory.Barracks:
                case FacilityHullCategory.ColonyHab:
                    value = 3;
                    break;
                case FacilityHullCategory.CentralHub:
                case FacilityHullCategory.Defense:
                    value = TempGameValues.__MaxElementActiveCMs;   // 6
                    break;
                case FacilityHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(cat));
            }
            D.Assert(value <= TempGameValues.__MaxElementActiveCMs);
            return value;
        }

        /// <summary>
        /// Debug: Supports the auto creation of Facility designs in NewGameUnitConfigurator.
        /// </summary>
        /// <param name="cat">The FacilityHullCategory.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static int __MaxSensors(this FacilityHullCategory cat) {
            int value = 1;
            switch (cat) {
                case FacilityHullCategory.Economic:
                case FacilityHullCategory.Factory:
                case FacilityHullCategory.Barracks:
                case FacilityHullCategory.Defense:
                case FacilityHullCategory.ColonyHab:
                    value = 2;
                    break;
                case FacilityHullCategory.Laboratory:
                case FacilityHullCategory.CentralHub:
                    value = TempGameValues.__MaxElementSensors;   // 4
                    break;
                case FacilityHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(cat));
            }
            D.Assert(value <= TempGameValues.__MaxElementSensors);
            return value;
        }

        /// <summary>
        /// Debug: Supports the auto creation of Facility designs in NewGameUnitConfigurator.
        /// </summary>
        /// <param name="cat">The FacilityHullCategory.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static int __MaxShieldGenerators(this FacilityHullCategory cat) {
            int value = 0;
            switch (cat) {
                case FacilityHullCategory.Economic:
                case FacilityHullCategory.Factory:
                case FacilityHullCategory.Barracks:
                case FacilityHullCategory.Laboratory:
                case FacilityHullCategory.ColonyHab:
                    value = 2;
                    break;
                case FacilityHullCategory.Defense:
                case FacilityHullCategory.CentralHub:
                    value = TempGameValues.__MaxElementShieldGenerators;   // 4
                    break;
                case FacilityHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(cat));
            }
            D.Assert(value <= TempGameValues.__MaxElementShieldGenerators);
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
                    dimensions = new Vector3(.02F, .03F, .05F);     // magnitude = .066
                    break;
                case ShipHullCategory.Destroyer:
                case ShipHullCategory.Support:
                    dimensions = new Vector3(.06F, .035F, .10F);    // magnitude = .122
                    break;
                case ShipHullCategory.Cruiser:
                case ShipHullCategory.Investigator:
                case ShipHullCategory.Colonizer:
                    dimensions = new Vector3(.09F, .05F, .16F);     // magnitude = .190
                    break;
                case ShipHullCategory.Dreadnought:
                case ShipHullCategory.Troop:
                    dimensions = new Vector3(.12F, .05F, .25F);     // magnitude = .282
                    break;
                case ShipHullCategory.Carrier:
                    dimensions = new Vector3(.10F, .06F, .32F);     // magnitude = .341
                    break;
                case ShipHullCategory.Fighter:
                case ShipHullCategory.Scout:
                case ShipHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
            }
            float radius = dimensions.magnitude / 2F;
            if (radius > TempGameValues.MaxShipRadius) {
                D.Warn("Ship {0}.Radius {1:0.####} > MaxRadius {2:0.##}.", hullCat.GetValueName(), radius, TempGameValues.MaxShipRadius);
            }
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
                    dimensions = new Vector3(.4F, .4F, .4F);    // magnitude = .693
                    break;
                case FacilityHullCategory.Economic:
                case FacilityHullCategory.Factory:
                case FacilityHullCategory.Laboratory:
                case FacilityHullCategory.ColonyHab:
                case FacilityHullCategory.Barracks:
                    dimensions = new Vector3(.2F, .2F, .2F);    // magnitude = .346
                    break;
                case FacilityHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
            }
            float radius = dimensions.magnitude / 2F;
            if (radius > TempGameValues.MaxFacilityRadius) {
                D.Warn("Facility {0}.Radius {1:0.####} > MaxRadius {2:0.##}.", hullCat.GetValueName(), radius, TempGameValues.MaxFacilityRadius);
            }
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

        public static float HitPoints(this FacilityHullCategory hullCat) {
            switch (hullCat) {
                case FacilityHullCategory.CentralHub:
                    return 100F;
                case FacilityHullCategory.Defense:
                case FacilityHullCategory.Factory:
                    return 80F;
                case FacilityHullCategory.ColonyHab:
                case FacilityHullCategory.Economic:
                case FacilityHullCategory.Barracks:
                case FacilityHullCategory.Laboratory:
                    return 60F;
                case FacilityHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
            }
        }

        public static float HitPoints(this ShipHullCategory hullCat) {
            switch (hullCat) {
                case ShipHullCategory.Frigate:
                    return 20F;
                case ShipHullCategory.Destroyer:
                case ShipHullCategory.Support:
                    return 30F;
                case ShipHullCategory.Cruiser:
                case ShipHullCategory.Colonizer:
                case ShipHullCategory.Investigator:
                    return 40F;
                case ShipHullCategory.Troop:
                    return 50F;
                case ShipHullCategory.Dreadnought:
                case ShipHullCategory.Carrier:
                    return 60F;
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

        public static float ConstructionCost(this ShipHullCategory hullCat) {
            switch (hullCat) {
                case ShipHullCategory.Frigate:
                    return 125F;
                case ShipHullCategory.Investigator:
                    return 130F;
                case ShipHullCategory.Destroyer:
                    return 200F;
                case ShipHullCategory.Support:
                    return 240F;
                case ShipHullCategory.Troop:
                    return 245F;
                case ShipHullCategory.Cruiser:
                    return 350F;
                case ShipHullCategory.Colonizer:
                    return 355F;
                case ShipHullCategory.Dreadnought:
                    return 470F;
                case ShipHullCategory.Carrier:
                    return 480F;
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
                    return 3;
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
                    return 5;
                case ShipHullCategory.Cruiser:
                case ShipHullCategory.Support:
                case ShipHullCategory.Investigator:
                    return 3;
                case ShipHullCategory.Destroyer:
                    return 2;
                case ShipHullCategory.Frigate:
                    return 1;
                case ShipHullCategory.Fighter:
                case ShipHullCategory.Scout:
                case ShipHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
            }
        }

        public static float Income(this FacilityHullCategory hullCat) {
            float result;
            switch (hullCat) {
                case FacilityHullCategory.CentralHub:
                    result = 5;
                    break;
                case FacilityHullCategory.Economic:
                    result = 20;
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
            return result;
        }

        public static float Expense(this FacilityHullCategory hullCat) {
            float result;
            switch (hullCat) {
                case FacilityHullCategory.CentralHub:
                case FacilityHullCategory.Economic:
                    result = Constants.ZeroF;
                    break;
                case FacilityHullCategory.Barracks:
                case FacilityHullCategory.ColonyHab:
                    result = 3;
                    break;
                case FacilityHullCategory.Defense:
                case FacilityHullCategory.Factory:
                case FacilityHullCategory.Laboratory:
                    result = 5;
                    break;
                case FacilityHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
            }
            return result;
        }

        public static float Science(this FacilityHullCategory hullCat) {
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
            return result;
        }

        public static float Culture(this FacilityHullCategory hullCat) {
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
            return result;
        }

        public static float Food(this FacilityHullCategory hullCat) {
            float result;
            switch (hullCat) {
                case FacilityHullCategory.CentralHub:
                case FacilityHullCategory.ColonyHab:
                    result = 10F;
                    break;
                case FacilityHullCategory.Factory:
                case FacilityHullCategory.Economic:
                case FacilityHullCategory.Barracks:
                case FacilityHullCategory.Defense:
                case FacilityHullCategory.Laboratory:
                    result = Constants.ZeroF;
                    break;
                case FacilityHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
            }
            return result;
        }

        public static float Production(this FacilityHullCategory hullCat) {
            float result;
            switch (hullCat) {
                case FacilityHullCategory.Factory:
                case FacilityHullCategory.CentralHub:
                    result = 10F;
                    break;
                case FacilityHullCategory.Economic:
                case FacilityHullCategory.Barracks:
                case FacilityHullCategory.ColonyHab:
                case FacilityHullCategory.Defense:
                case FacilityHullCategory.Laboratory:
                    result = Constants.OneF;    // TEMP Reqd to make sure UnitProduction is not zero when used as a denominator
                    break;
                case FacilityHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
            }
            return result;
        }

        public static float ConstructionCost(this FacilityHullCategory hullCat) {
            float result;
            switch (hullCat) {
                case FacilityHullCategory.Barracks:
                    result = 250F;
                    break;
                case FacilityHullCategory.Economic:
                    result = 260F;
                    break;
                case FacilityHullCategory.ColonyHab:
                    result = 270F;
                    break;
                case FacilityHullCategory.Laboratory:
                    result = 280F;
                    break;
                case FacilityHullCategory.Factory:
                    result = 290F;
                    break;
                case FacilityHullCategory.Defense:
                    result = 300F;
                    break;
                case FacilityHullCategory.CentralHub:
                    result = 400F;
                    break;
                case FacilityHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
            }
            return result;
        }

        #endregion

        public static string GetEmptyTemplateDesignName(this ShipHullCategory cat) {
            string designNameFormat = "Empty{0}Design";
            string designName;
            switch (cat) {
                case ShipHullCategory.Investigator:
                case ShipHullCategory.Frigate:
                case ShipHullCategory.Support:
                case ShipHullCategory.Destroyer:
                case ShipHullCategory.Colonizer:
                case ShipHullCategory.Troop:
                case ShipHullCategory.Cruiser:
                case ShipHullCategory.Carrier:
                case ShipHullCategory.Dreadnought:
                    designName = designNameFormat.Inject(cat.GetValueName());
                    break;
                case ShipHullCategory.Fighter:
                case ShipHullCategory.Scout:
                case ShipHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(cat));
            }
            D.AssertNotNull(designName);
            return designName;
        }

        public static string GetEmptyTemplateDesignName(this FacilityHullCategory cat) {
            string designNameFormat = "Empty{0}Design";
            string designName;
            switch (cat) {
                case FacilityHullCategory.Barracks:
                case FacilityHullCategory.CentralHub:
                case FacilityHullCategory.ColonyHab:
                case FacilityHullCategory.Defense:
                case FacilityHullCategory.Economic:
                case FacilityHullCategory.Factory:
                case FacilityHullCategory.Laboratory:
                    designName = designNameFormat.Inject(cat.GetValueName());
                    break;
                case FacilityHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(cat));
            }
            D.AssertNotNull(designName);
            return designName;
        }

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

        #region Equipment Category

        private static EquipmentMountCategory[] AllowedTurretMounts = new EquipmentMountCategory[] { EquipmentMountCategory.Turret };
        private static EquipmentMountCategory[] AllowedSiloMounts = new EquipmentMountCategory[] { EquipmentMountCategory.Silo };
        private static EquipmentMountCategory[] AllowedPassiveCmMounts = new EquipmentMountCategory[] { EquipmentMountCategory.Skin, EquipmentMountCategory.Flex };
        private static EquipmentMountCategory[] AllowedActiveCmMounts = new EquipmentMountCategory[] { EquipmentMountCategory.Screen, EquipmentMountCategory.Flex };
        private static EquipmentMountCategory[] AllowedSensorMounts = new EquipmentMountCategory[] { EquipmentMountCategory.Sensor, EquipmentMountCategory.Flex };
        private static EquipmentMountCategory[] AllowedShieldGenMounts = new EquipmentMountCategory[] { EquipmentMountCategory.Screen, EquipmentMountCategory.Flex };

        public static EquipmentMountCategory[] AllowedMounts(this EquipmentCategory eqCat) {
            switch (eqCat) {
                case EquipmentCategory.BeamWeapon:
                case EquipmentCategory.ProjectileWeapon:
                    return AllowedTurretMounts;
                case EquipmentCategory.MissileWeapon:
                case EquipmentCategory.AssaultWeapon:
                    return AllowedSiloMounts;
                case EquipmentCategory.PassiveCountermeasure:
                    return AllowedPassiveCmMounts;
                case EquipmentCategory.ActiveCountermeasure:
                    return AllowedActiveCmMounts;
                case EquipmentCategory.SRSensor:
                case EquipmentCategory.MRSensor:
                case EquipmentCategory.LRSensor:
                    return AllowedSensorMounts;
                case EquipmentCategory.ShieldGenerator:
                    return AllowedShieldGenMounts;
                case EquipmentCategory.FtlDampener:
                case EquipmentCategory.Hull:
                case EquipmentCategory.StlPropulsion:
                case EquipmentCategory.FtlPropulsion:
                case EquipmentCategory.StarbaseCmdModule:
                case EquipmentCategory.SettlementCmdModule:
                case EquipmentCategory.FleetCmdModule:
                case EquipmentCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(eqCat));
            }
        }

        public static EquipmentCategory[] SupportedEquipment(this EquipmentMountCategory mountCat) {
            switch (mountCat) {
                case EquipmentMountCategory.Turret:
                    return new EquipmentCategory[] { EquipmentCategory.BeamWeapon, EquipmentCategory.ProjectileWeapon };
                case EquipmentMountCategory.Silo:
                    return new EquipmentCategory[] { EquipmentCategory.MissileWeapon, EquipmentCategory.AssaultWeapon };
                case EquipmentMountCategory.Sensor:
                    return new EquipmentCategory[] { EquipmentCategory.SRSensor, EquipmentCategory.MRSensor, EquipmentCategory.LRSensor };
                case EquipmentMountCategory.Skin:
                    return new EquipmentCategory[] { EquipmentCategory.PassiveCountermeasure };
                case EquipmentMountCategory.Screen:
                    return new EquipmentCategory[] { EquipmentCategory.ActiveCountermeasure, EquipmentCategory.ShieldGenerator };
                case EquipmentMountCategory.Flex:
                    return new EquipmentCategory[] { EquipmentCategory.SRSensor, EquipmentCategory.MRSensor, EquipmentCategory.LRSensor,
                        EquipmentCategory.PassiveCountermeasure, EquipmentCategory.ActiveCountermeasure, EquipmentCategory.ShieldGenerator };
                case EquipmentMountCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(mountCat));
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

                case GuiElementID.UserPlayerHomeDesirabilityPopupList:
                    return "UserPlayerHomeDesirabilitySelection";
                case GuiElementID.AIPlayer1HomeDesirabilityPopupList:
                    return "AIPlayer1HomeDesirabilitySelection";
                case GuiElementID.AIPlayer2HomeDesirabilityPopupList:
                    return "AIPlayer2HomeDesirabilitySelection";
                case GuiElementID.AIPlayer3HomeDesirabilityPopupList:
                    return "AIPlayer3HomeDesirabilitySelection";
                case GuiElementID.AIPlayer4HomeDesirabilityPopupList:
                    return "AIPlayer4HomeDesirabilitySelection";
                case GuiElementID.AIPlayer5HomeDesirabilityPopupList:
                    return "AIPlayer5HomeDesirabilitySelection";
                case GuiElementID.AIPlayer6HomeDesirabilityPopupList:
                    return "AIPlayer6HomeDesirabilitySelection";
                case GuiElementID.AIPlayer7HomeDesirabilityPopupList:
                    return "AIPlayer7HomeDesirabilitySelection";

                case GuiElementID.UserPlayerStartLevelPopupList:
                    return "UserPlayerStartLevelSelection";
                case GuiElementID.AIPlayer1StartLevelPopupList:
                    return "AIPlayer1StartLevelSelection";
                case GuiElementID.AIPlayer2StartLevelPopupList:
                    return "AIPlayer2StartLevelSelection";
                case GuiElementID.AIPlayer3StartLevelPopupList:
                    return "AIPlayer3StartLevelSelection";
                case GuiElementID.AIPlayer4StartLevelPopupList:
                    return "AIPlayer4StartLevelSelection";
                case GuiElementID.AIPlayer5StartLevelPopupList:
                    return "AIPlayer5StartLevelSelection";
                case GuiElementID.AIPlayer6StartLevelPopupList:
                    return "AIPlayer6StartLevelSelection";
                case GuiElementID.AIPlayer7StartLevelPopupList:
                    return "AIPlayer7StartLevelSelection";

                case GuiElementID.AIPlayer1UserSeparationPopupList:
                    return "AIPlayer1UserSeparationSelection";
                case GuiElementID.AIPlayer2UserSeparationPopupList:
                    return "AIPlayer2UserSeparationSelection";
                case GuiElementID.AIPlayer3UserSeparationPopupList:
                    return "AIPlayer3UserSeparationSelection";
                case GuiElementID.AIPlayer4UserSeparationPopupList:
                    return "AIPlayer4UserSeparationSelection";
                case GuiElementID.AIPlayer5UserSeparationPopupList:
                    return "AIPlayer5UserSeparationSelection";
                case GuiElementID.AIPlayer6UserSeparationPopupList:
                    return "AIPlayer6UserSeparationSelection";
                case GuiElementID.AIPlayer7UserSeparationPopupList:
                    return "AIPlayer7UserSeparationSelection";

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

        public static float __GetBaselineFtlDampenerRange(this RangeCategory rangeCategory) {
            float value = Constants.ZeroF;
            switch (rangeCategory) {
                case RangeCategory.Short:
                    value = GetBaselineSensorRange(rangeCategory);
                    break;
                case RangeCategory.Medium:
                case RangeCategory.Long:
                case RangeCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(rangeCategory));
            }
            return value;
        }

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
                    range = 8F;
                    break;
                case RangeCategory.Medium:
                    range = 12F;
                    break;
                case RangeCategory.Long:
                    range = 16F;
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
            float value = Constants.ZeroF;
            switch (rangeCategory) {
                case RangeCategory.Short:
                    value = 150F;
                    break;
                case RangeCategory.Medium:
                    value = 750F;
                    break;
                case RangeCategory.Long:
                    value = 2500F;
                    break;
                case RangeCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(rangeCategory));
            }
            return value;
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

        #region New Order Availability

        public static bool TryGetLessRestrictiveAvailability(this NewOrderAvailability availability, out NewOrderAvailability lessRestrictiveAvailability) {
            switch (availability) {
                case NewOrderAvailability.Unavailable:
                    lessRestrictiveAvailability = NewOrderAvailability.None;
                    return false;
                case NewOrderAvailability.BarelyAvailable:
                    lessRestrictiveAvailability = NewOrderAvailability.Unavailable;
                    return true;
                case NewOrderAvailability.FairlyAvailable:
                    lessRestrictiveAvailability = NewOrderAvailability.BarelyAvailable;
                    return true;
                case NewOrderAvailability.EasilyAvailable:
                    lessRestrictiveAvailability = NewOrderAvailability.FairlyAvailable;
                    return true;
                case NewOrderAvailability.Available:
                    lessRestrictiveAvailability = NewOrderAvailability.EasilyAvailable;
                    return true;
                case NewOrderAvailability.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(availability));
            }
        }

        #endregion

        #region Ship and Fleet Speed 

        public static float GetUnitsPerHour(this Speed speed, float fullSpeedValue) {
            // Note: see Flight.txt in GameDev Notes for analysis of speed values
            // 4.12.18 Also Notebook
            float fullSpeedFactor = Constants.ZeroF;
            switch (speed) {
                case Speed.HardStop:
                    return Constants.ZeroF;
                case Speed.Stop:
                    return Constants.ZeroF;
                case Speed.ThrustersOnly:
                    // 4.13.16 InSystem, STL = 0.05 but clamped at ~0.2 below, OpenSpace: STL = 0.24, FTL = 1.2
                    // 4.12.18 with Fastest Engine tech: InSystem, STL = 0.13, OpenSpace: STL = 0.5, FTL = 1.25
                    fullSpeedFactor = 0.03F;
                    break;
                case Speed.Docking:
                    // 4.9.16 InSystem, STL = 0.08 but clamped at ~0.2 below, OpenSpace: STL = 0.4, FTL = 2
                    // 4.12.18 with Fastest Engine tech: InSystem, STL = 0.2, OpenSpace: STL = 0.8, FTL = 2
                    fullSpeedFactor = 0.05F;
                    break;
                case Speed.DeadSlow:
                    // 4.9.16 InSystem, STL = 0.13 but clamped at ~0.2 below, OpenSpace: STL = 0.64, FTL = 3.2
                    // 4.12.18 with Fastest Engine tech: InSystem, STL = 0.32, OpenSpace: STL = 1.3, FTL = 3.2
                    fullSpeedFactor = 0.08F;
                    break;
                case Speed.Slow:
                    // 4.9.16 InSystem, STL = 0.24, OpenSpace: STL = 1.2, FTL = 6
                    // 4.12.18 with Fastest Engine tech: InSystem, STL = 0.6, OpenSpace: STL = 2.4, FTL = 6
                    fullSpeedFactor = 0.15F;
                    break;
                case Speed.OneThird:
                    // 11.24.15 InSystem, STL = 0.4, OpenSpace: STL = 2, FTL = 10
                    // 4.12.18 with Fastest Engine tech: InSystem, STL = 1, OpenSpace: STL = 4, FTL = 10
                    fullSpeedFactor = 0.25F;
                    break;
                case Speed.TwoThirds:
                    // 11.24.15 InSystem, STL = 0.8, OpenSpace: STL = 4, FTL = 20
                    // 4.12.18 with Fastest Engine tech: InSystem, STL = 2, OpenSpace: STL = 8, FTL = 20
                    fullSpeedFactor = 0.50F;
                    break;
                case Speed.Standard:
                    // 11.24.15 InSystem, STL = 1.2, OpenSpace: STL = 6, FTL = 30
                    // 4.12.18 with Fastest Engine tech: InSystem, STL = 3, OpenSpace: STL = 12, FTL = 30
                    fullSpeedFactor = 0.75F;
                    break;
                case Speed.Full:
                    // 11.24.15 InSystem, STL = 1.6, OpenSpace: STL = 8, FTL = 40
                    // 4.12.18 with Fastest Engine tech: InSystem, STL = 4, OpenSpace: STL = 16, FTL = 40
                    fullSpeedFactor = 1.0F;
                    break;
                case Speed.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(speed));
            }

            float speedValueResult = fullSpeedFactor * fullSpeedValue;
            if (speedValueResult < TempGameValues.ShipMinimumSpeedValue) {  // 4.12.18 0.2 -> 0.125 -> 0.05
                D.Warn("Speed {0} value {1:0.0000} is being clamped to {2:0.0000}.", speed.GetValueName(), speedValueResult, TempGameValues.ShipMinimumSpeedValue);
                speedValueResult = TempGameValues.ShipMinimumSpeedValue;
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
                    return TempGameValues.SystemNebulaToOpenSpaceDensityFactor;
                case Topography.DeepNebula: // TODO
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

        public static float RepairCapacityFactor(this DiplomaticRelationship relation) {
            switch (relation) {
                case DiplomaticRelationship.None:
                case DiplomaticRelationship.Neutral:
                    return 0.5F;
                case DiplomaticRelationship.Friendly:
                    return 1.0F;
                case DiplomaticRelationship.Alliance:
                case DiplomaticRelationship.Self:
                    return 2.0F;
                case DiplomaticRelationship.War:
                case DiplomaticRelationship.ColdWar:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(relation));
            }
        }


        #endregion

        #region Sound Effects

        /// <summary>
        /// Returns the name of the AudioClip associated with this clipID.
        /// SoundManagerPro uses the string name of the clip to find it. 
        /// <remarks>Done this way to allow my own clipID naming, independent of the name
        /// of the AudioClips that I can acquire.</remarks>
        /// </summary>
        /// <param name="clipID">The clip identifier.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static string SfxClipName(this SfxClipID clipID) {
            switch (clipID) {
                case SfxClipID.Select:
                    return "Select";
                case SfxClipID.Explosion1:
                    return "Explosion1";
                case SfxClipID.Error:
                    return "Rumble";
                case SfxClipID.Swipe:
                    return "Swipe";
                case SfxClipID.Tap:
                    return "Tap";
                case SfxClipID.UnSelect:
                    return "Crumple";
                case SfxClipID.OpenShut:
                    return "OpenShut";
                case SfxClipID.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(clipID));
            }
        }

        #endregion

        #region Formation

        public static int MaxFormationSlots(this Formation formation) {
            switch (formation) {
                case Formation.Globe:
                    return 35;
                case Formation.Wedge:
                    return 61;
                case Formation.Plane:
                    return 31;
                case Formation.Diamond:
                    return 31;
                case Formation.Spread:
                    return 39;
                case Formation.Hanger:
                    return 8;
                case Formation.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(formation));
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
                    return Enums<Formation>.GetRandomFrom(TempGameValues.AcceptableBaseFormations);
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
                    return Enums<Formation>.GetRandomFrom(TempGameValues.AcceptableFleetFormations);
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

        }

        #endregion

    }
}

