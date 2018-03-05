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
    /// <remarks>TODO Acquire content indicators from XML values and make use of Player parameter.</remarks>
    /// </summary>
    public class TechnologyFactory : AGenericSingleton<TechnologyFactory> {

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
            float costIncrement = UnityEngine.Random.Range(30F, 60F);
            for (int i = 0; i < 100; i++) {
                float researchCost = baselineResearchCost + i * costIncrement;
                string techname = __GetUniqueTechname();
                var eStat = EquipmentStatFactory.Instance.__GetRandomEquipmentStat();
                _cache.Add(techname, new Technology(techname, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description", researchCost,
                    new ATechStat[] { eStat }));
            }
        }

        public Technology MakeInstance(Player player, string techname) {
            return _cache[techname];
        }

        public IEnumerable<Technology> GetAllTechs(Player player) {
            return _cache.Values;
        }

        public Technology __GetStartingTech(Player player) {
            return _cache.Values.OrderBy(t => t.ResearchCost).First();
        }

        #region XML Reader 

        // UNDONE

        #endregion

        #region Debug

        public Technology __GetRandomTech() {
            return RandomExtended.Choice<Technology>(_cache.Values);
        }

        public Technology __GetNextHigherCostTechThan(Technology tech) {
            var ascendingCostOrderTechs = _cache.Values.OrderBy(t => t.ResearchCost).ToList();
            int techIndex = ascendingCostOrderTechs.FindIndex(t => t.ResearchCost == tech.ResearchCost);
            return ascendingCostOrderTechs[techIndex + 1];
        }

        private const string TechnameFormat = "Tech{0}";

        private int _nameCounter = Constants.One;

        private string __GetUniqueTechname() {
            string name = TechnameFormat.Inject(_nameCounter);
            _nameCounter++;
            return name;
        }

        #endregion


    }
}

