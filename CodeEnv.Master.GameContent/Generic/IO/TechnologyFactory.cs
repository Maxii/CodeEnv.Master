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
    using CodeEnv.Master.Common;

    /// <summary>
    /// Singleton. Factory that makes and caches Technology instances.
    /// <remarks>TODO Read values from XML and build TechStats. Make use of Player parameter.</remarks>
    /// </summary>
    public class TechnologyFactory : AGenericSingleton<TechnologyFactory> {

        private static IDictionary<string, string[]> TechPrerequisiteNames = new Dictionary<string, string[]>() {
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

        private static IDictionary<string, TreeNodeID> TechNodeIDs = new Dictionary<string, TreeNodeID>() {
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

        private IDictionary<string, Technology> _cache;

        private TechnologyFactory() {
            Initialize();
        }

        protected override void Initialize() {
            // WARNING: Do not use Instance or _instance in here as this is still part of Constructor
            CreateAllTechnologies();
        }

        private void CreateAllTechnologies() {
            _cache = new Dictionary<string, Technology>();
            float baselineResearchCost = 200F;
            __techCostIncrement = UnityEngine.Random.Range(30F, 60F);
            for (int i = 0; i < 5; i++) {   // 5 predefined techs including the first FutureTech
                float researchCost = baselineResearchCost + i * __techCostIncrement;
                bool isFirstFutureTech = i == 4;
                Technology nextTech = __MakeTech(researchCost, isFirstFutureTech);  // TechNames start with Tech0
                _cache.Add(nextTech.Name, nextTech);
            }

            foreach (var tech in _cache.Values) {
                __AddPrerequisitesTo(tech);
            }
        }

        public Technology MakeInstance(Player player, string techname) {
            return _cache[techname];
        }

        public IEnumerable<Technology> GetAllPredefinedTechs(Player player) {
            return _cache.Values;
        }

        public Technology __GetStartingTech(Player player) {
            var lowestCostTech = _cache.Values.OrderBy(t => t.ResearchCost).First();
            var firstTechByDesign = _cache["Tech0"];
            D.AssertEqual(lowestCostTech, firstTechByDesign);
            return firstTechByDesign;
        }

        #region XML Reader 

        // UNDONE

        #endregion

        #region Debug

        private const string __TechnameFormat = "Tech{0}";

        private int __nameCounter = Constants.Zero;
        private float __techCostIncrement;

        private Technology __MakeTech(float researchCost, bool isFutureTech) {
            var randomEquipStat = EquipmentStatFactory.Instance.__GetRandomNonHullEquipmentStat();
            string techName = isFutureTech ? "FutureTech" : __GetUniqueTechname();
            string[] prereqNames = TechPrerequisiteNames[techName];
            TreeNodeID nodeID = TechNodeIDs[techName];
            TechStat techStat = new TechStat(techName, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description", nodeID, prereqNames);
            return new Technology(techStat, researchCost, new AImprovableStat[] { randomEquipStat });
        }

        public Technology __MakeNextFutureTechFollowing(Technology previousFutureTech) {
            D.AssertEqual("FutureTech", previousFutureTech.Name);
            float nextRschCost = previousFutureTech.ResearchCost + __techCostIncrement;
            var nextFutureTech = __MakeTech(nextRschCost, true);
            _cache[nextFutureTech.Name] = nextFutureTech;
            __AddPrerequisitesTo(nextFutureTech);
            return nextFutureTech;
        }

        private void __AddPrerequisitesTo(Technology tech) {
            var prereqNames = tech.PrerequisiteTechNames;
            IList<Technology> prereqTechs = new List<Technology>();
            foreach (string name in prereqNames) {
                prereqTechs.Add(_cache[name]);
            }
            tech.Prerequisites = prereqTechs.ToArray();
        }

        private string __GetUniqueTechname() {
            string name = __TechnameFormat.Inject(__nameCounter);
            __nameCounter++;
            return name;
        }

        #endregion


    }
}

