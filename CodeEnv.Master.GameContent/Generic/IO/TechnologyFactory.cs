// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: TechnologyFactory.cs
// Singleton. Factory that makes and caches Technology instances.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Singleton. Factory that makes and caches Technology instances.
    /// <remarks>Make use of Player parameter.</remarks>
    /// </summary>
    public class TechnologyFactory : AGenericSingleton<TechnologyFactory> {

        [System.Obsolete]
        private static IDictionary<string, string[]> __TechPrerequisiteNames = new Dictionary<string, string[]>() {
            //              Tech1 - Tech3 
            //          /                   \
            //  Tech0                           FutureTech
            //          \                   /
            //              Tech2       
            {"Tech0", new string[] { }                          },
            {"Tech1", new string[] { "Tech0" }                  },
            {"Tech2", new string[] { "Tech0" }                  },
            {"Tech3", new string[] { "Tech1" }                  },
            {"FutureTech", new string[] { "Tech2", "Tech3" }    }
        };

        [System.Obsolete]
        private static IDictionary<string, TreeNodeID> __TechNodeIDs = new Dictionary<string, TreeNodeID>() {
                    //  Col1        Col2    Col3        Col4
     // Row1        //              Tech1 - Tech3                       
                    //          /                   \
     // Row2        //  Tech0                           FutureTech
                    //          \                   /
    // Row3         //              Tech2       
            {"Tech0", new TreeNodeID(1, 2)          },
            {"Tech1", new TreeNodeID(2, 1)          },
            {"Tech2", new TreeNodeID(2, 3)          },
            {"Tech3", new TreeNodeID(3, 1)          },
            {"FutureTech", new TreeNodeID(4, 2)     }
        };

        private float _futureTechRschCostIncrement = 1000F;

        private IDictionary<string, Technology> _cache;
        private EquipmentStatFactory _eStatFactory;

        private TechnologyFactory() {
            Initialize();
        }

        protected override void Initialize() {
            // WARNING: Do not use Instance or _instance in here as this is still part of Constructor
            _eStatFactory = EquipmentStatFactory.Instance;
            // LazyInitialize() approach taken to make sure AllPlayers has been set
        }

        /// <summary>
        /// Checks whether the initialization and creation of all techs has yet occurred.
        /// If it hasn't, this method takes care of it.
        /// <remarks>Handled this way so that GameManager.AllPlayers is set when initialization occurs.</remarks>
        /// </summary>
        private void LazyInitialize() {
            if (_cache == null) {
                CreateAllTechnologies();
            }
            D.AssertNotNull(_cache);
        }

        private void CreateAllTechnologies() {
            var allPlayers = GameReferences.GameManager.AllPlayers;
            // EquipmentStatFactory requires a Player parameter in anticipation of implementing EquipmentStats varying by player
            Player __tempPlayer = RandomExtended.Choice(allPlayers);
            IList<Technology> allTechs = new List<Technology>();
            var allTechStats = TechStatXmlReader.Instance.CreateAllStats();
            foreach (var techStat in allTechStats) {
                Technology tech = CreateInstance(__tempPlayer, techStat);
                allTechs.Add(tech);
            }
            _cache = allTechs.ToDictionary(tech => tech.Name);

            foreach (var tech in allTechs) {
                AddPrerequisitesTo(tech);
            }
        }

        /// <summary>
        /// Returns all Technologies that are predefined in the game, including the first FutureTech.
        /// <remarks>Subsequent FutureTechs are dynamically generated and recorded via MakeFutureTechInstanceFollowing().</remarks>
        /// </summary>
        /// <param name="player">The player.</param>
        /// <returns></returns>
        public IEnumerable<Technology> GetAllPredefinedTechs(Player player) {
            LazyInitialize();
            return _cache.Values;
        }

        /// <summary>
        /// Returns the cached 'predefined' Technology whose name is techName.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="techName">The name of the tech to return.</param>
        /// <returns></returns>
        public Technology MakeInstance(Player player, string techName) {
            __ValidateInitialized();
            return _cache[techName];
        }

        /// <summary>
        /// Dynamically creates, records and returns the 'FutureTech' Technology that should follow the <c>previousFutureTech</c>.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="previousFutureTech">The previous future tech.</param>
        /// <returns></returns>
        public Technology MakeFutureTechInstanceFollowing(Player player, Technology previousFutureTech) {
            __ValidateInitialized();
            D.AssertEqual("FutureTech", previousFutureTech.Name);
            float nextRschCost = previousFutureTech.ResearchCost + _futureTechRschCostIncrement;
            var nextFutureTech = CreateInstance(player, previousFutureTech.Stat);
            _cache[nextFutureTech.Name] = nextFutureTech;
            AddPrerequisitesTo(nextFutureTech);
            return nextFutureTech;
        }

        private Technology CreateInstance(Player player, TechStat techStat) {
            float rschCost = CalcResearchCost(player, techStat.BaseResearchCost);
            var enabledEquipStats = GetEnabledEquipStats(player, techStat.EnabledEquipmentIDs);
            return new Technology(techStat, rschCost, enabledEquipStats);
        }

        /// <summary>
        /// Calculates the research cost required to complete research of a Technology from the
        /// baselineRschCost provided.
        /// <remarks>IMPROVE Placeholder for modifying research cost based on PlayerIQ, GameDuration, etc.</remarks>
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="baselineRschCost">The baseline cost to research a Technology.</param>
        /// <returns></returns>
        private float CalcResearchCost(Player player, float baselineRschCost) {
            return baselineRschCost;
        }

        private IEnumerable<AEquipmentStat> GetEnabledEquipStats(Player player, IEnumerable<EquipmentStatID> equipmentStatIDs) {
            IList<AEquipmentStat> enabledEquipStats = new List<AEquipmentStat>();
            foreach (var eStatID in equipmentStatIDs) {
                AEquipmentStat eStat = _eStatFactory.MakeInstance(player, eStatID);
                enabledEquipStats.Add(eStat);
            }
            return enabledEquipStats;
        }

        private void AddPrerequisitesTo(Technology tech) {
            var prereqNames = tech.Stat.PrerequisiteTechNames;
            IList<Technology> prereqTechs = new List<Technology>();
            foreach (string name in prereqNames) {
                prereqTechs.Add(_cache[name]);
            }
            tech.Prerequisites = prereqTechs;
        }

        #region Debug

        [System.Diagnostics.Conditional("DEBUG")]
        private void __ValidateInitialized() {
            D.AssertNotNull(_cache);
        }

        #endregion

        #region Nested Classes

        private class TechStatXmlReader : AXmlReader<TechStatXmlReader> {

            private string _techStatTagName = "TechStat";
            private string _nameTagName = "Name";
            private string _imageAtlasIDTagName = "ImageAtlasID";
            private string _imageFilenameTagName = "ImageFilename";
            private string _descriptionTagName = "Description";
            private string _baseRschCostTagName = "BaseRschCost";
            private string _treeNodeIDTagName = "TreeNodeID";
            private string _columnTagName = "Column";
            private string _rowTagName = "Row";
            private string _prereqTechNamesTagName = "PrereqTechs";
            private string _enabledStatsTagName = "EnabledStats";
            private string _equipIdTagName = "EquipID";
            private string _equipCatTagName = "EquipCat";
            private string _levelTagName = "Level";

            private string _capabilityIdTagName = "CapabilityID";
            private string _capabilityTagName = "Capability";

            protected override string XmlFilename { get { return "TechStatValues"; } }

            private TechStatXmlReader() {
                Initialize();
            }

            protected override void InitializeValuesAndReferences() {
                base.InitializeValuesAndReferences();
            }

            internal IList<TechStat> CreateAllStats() {
                IList<TechStat> stats = new List<TechStat>();
                var techStatNodes = _xElement.Elements(_techStatTagName);
                foreach (var techStatNode in techStatNodes) {
                    string techName = techStatNode.Element(_nameTagName).Value;
                    AtlasID imageAtlasID = Enums<AtlasID>.Parse(techStatNode.Element(_imageAtlasIDTagName).Value);
                    string imageFilename = techStatNode.Element(_imageFilenameTagName).Value;
                    string description = techStatNode.Element(_descriptionTagName).Value;
                    float baseRschCost = float.Parse(techStatNode.Element(_baseRschCostTagName).Value);
                    TreeNodeID treeNodeID = GetTreeNodeID(techStatNode);
                    IEnumerable<string> prereqTechNames = GetPrereqTechNames(techStatNode);
                    IEnumerable<EquipmentStatID> enabledEquipIDs = GetEnabledEquipmentIDs(techStatNode);
                    IEnumerable<CapabilityStatID> enabledCapabilityIDs = GetEnabledCapabilityIDs(techStatNode);
                    TechStat stat = new TechStat(techName, imageAtlasID, imageFilename, description, baseRschCost, treeNodeID, prereqTechNames, enabledEquipIDs, enabledCapabilityIDs);
                    stats.Add(stat);
                }
                return stats;
            }

            private TreeNodeID GetTreeNodeID(XElement techStatNode) {
                XElement treeNode = techStatNode.Element(_treeNodeIDTagName);
                int column = int.Parse(treeNode.Element(_columnTagName).Value);
                int row = int.Parse(treeNode.Element(_rowTagName).Value);
                return new TreeNodeID(column, row);
            }

            private IEnumerable<string> GetPrereqTechNames(XElement techStatNode) {
                IList<string> prereqTechNames = new List<string>();
                XElement prereqTechNamesNode = techStatNode.Element(_prereqTechNamesTagName);
                var nameNodes = prereqTechNamesNode.Elements(_nameTagName);
                foreach (var nameNode in nameNodes) {
                    string nameNodeValue = nameNode.Value;
                    if (nameNode.IsEmpty || nameNodeValue.Equals(string.Empty)) {
                        continue;
                    }
                    prereqTechNames.Add(nameNodeValue);
                }
                return prereqTechNames;
            }

            private IEnumerable<EquipmentStatID> GetEnabledEquipmentIDs(XElement techStatNode) {
                IList<EquipmentStatID> enabledEquipIDs = new List<EquipmentStatID>();
                XElement enabledStatsNode = techStatNode.Element(_enabledStatsTagName);
                var equipIdNodes = enabledStatsNode.Elements(_equipIdTagName);
                foreach (var idNode in equipIdNodes) {
                    XElement equipCatNode = idNode.Element(_equipCatTagName);
                    string equipCatNodeValue = equipCatNode.Value;
                    if (equipCatNode.IsEmpty || equipCatNodeValue == string.Empty) {    // XElement.IsEmpty is true only if in the format <TagName/> 
                        continue;
                    }
                    EquipmentCategory eqCat = Enums<EquipmentCategory>.Parse(equipCatNodeValue);
                    Level eqLevel = Enums<Level>.Parse(idNode.Element(_levelTagName).Value);
                    EquipmentStatID eqID = new EquipmentStatID(eqCat, eqLevel);
                    enabledEquipIDs.Add(eqID);
                }
                return enabledEquipIDs;
            }

            private IEnumerable<CapabilityStatID> GetEnabledCapabilityIDs(XElement techStatNode) {
                IList<CapabilityStatID> enabledCapIDs = new List<CapabilityStatID>();
                XElement enabledStatsNode = techStatNode.Element(_enabledStatsTagName);
                var capIdNodes = enabledStatsNode.Elements(_capabilityIdTagName);
                foreach (var idNode in capIdNodes) {
                    XElement capNode = idNode.Element(_capabilityTagName);
                    string capNodeValue = capNode.Value;
                    if (capNode.IsEmpty || capNodeValue == string.Empty) {  // XElement.IsEmpty is true only if in the format <TagName/> 
                        continue;
                    }
                    Capability cap = Enums<Capability>.Parse(capNodeValue);
                    Level capLevel = Enums<Level>.Parse(idNode.Element(_levelTagName).Value);
                    CapabilityStatID capID = new CapabilityStatID(cap, capLevel);
                    enabledCapIDs.Add(capID);
                }
                return enabledCapIDs;
            }

        }

        #endregion


    }
}

