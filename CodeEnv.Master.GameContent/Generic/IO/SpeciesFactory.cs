// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SpeciesFactory.cs
// Singleton. Factory that makes SpeciesStat instances from values externally acquired via Xml.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Singleton. Factory that makes SpeciesStat instances from values externally acquired via Xml.
    /// </summary>
    public class SpeciesFactory : AGenericSingleton<SpeciesFactory> {

        private SpeciesStatXmlReader _xmlReader;
        private IDictionary<Species, SpeciesStat> _statCache;

        private SpeciesFactory() {
            Initialize();
        }

        protected sealed override void Initialize() {
            _xmlReader = SpeciesStatXmlReader.Instance;
            _statCache = new Dictionary<Species, SpeciesStat>(SpeciesEqualityComparer.Default);
            // WARNING: Do not use Instance or _instance in here as this is still part of Constructor
        }

        public SpeciesStat MakeInstance(Species species) {
            D.AssertNotDefault((int)species);
            SpeciesStat stat;
            if (!_statCache.TryGetValue(species, out stat)) {
                stat = _xmlReader.CreateStat(species);
                _statCache.Add(species, stat);
            }
            return stat;
        }

        #region Nested Classes

        private class SpeciesStatXmlReader : AXmlReader<SpeciesStatXmlReader> {

            private string _speciesTagName = "Species";
            private string _speciesAttributeTagName = "SpeciesName";
            private string _speciesPluralNameTagName = "PluralName";
            private string _speciesDescriptionTagName = "Description";
            private string _speciesImageAtlasIDTagName = "ImageAtlasID";
            private string _speciesImageFilenameTagName = "ImageFilename";
            private string _speciesSensorRangeMultiplierTagName = "SensorRangeMultiplier";
            private string _speciesWeaponRangeMultiplierTagName = "WeaponRangeMultiplier";
            private string _speciesActiveCmRangeMultiplierTagName = "ActiveCmRangeMultiplier";
            private string _speciesWeaponReloadPeriodMultiplierTagName = "WeaponReloadPeriodMultiplier";
            private string _speciesActiveCmReloadPeriodMultiplierTagName = "ActiveCmReloadPeriodMultiplier";
            private string _speciesBuyoutCostMultiplierTagName = "BuyoutCostMultiplier";

            protected override string XmlFilename { get { return "SpeciesValues"; } }

            private SpeciesStatXmlReader() {
                Initialize();
            }

            internal SpeciesStat CreateStat(Species species) {
                SpeciesStat stat = default(SpeciesStat);
                var speciesNodes = _xElement.Elements(_speciesTagName);
                foreach (var speciesNode in speciesNodes) {
                    var speciesAttribute = speciesNode.Attribute(_speciesAttributeTagName);
                    string speciesName = speciesAttribute.Value;

                    if (species.GetValueName() == speciesName) {
                        // found the right species node
                        string pluralName = speciesNode.Element(_speciesPluralNameTagName).Value;
                        string description = speciesNode.Element(_speciesDescriptionTagName).Value;
                        AtlasID atlasID = Enums<AtlasID>.Parse(speciesNode.Element(_speciesImageAtlasIDTagName).Value);
                        string filename = speciesNode.Element(_speciesImageFilenameTagName).Value;
                        float sensorRangeMultiplier = float.Parse(speciesNode.Element(_speciesSensorRangeMultiplierTagName).Value);
                        float weaponRangeMultiplier = float.Parse(speciesNode.Element(_speciesWeaponRangeMultiplierTagName).Value);
                        float activeCmRangeMultiplier = float.Parse(speciesNode.Element(_speciesActiveCmRangeMultiplierTagName).Value);
                        float weaponReloadPeriodMultiplier = float.Parse(speciesNode.Element(_speciesWeaponReloadPeriodMultiplierTagName).Value);
                        float activeCmReloadPeriodMultiplier = float.Parse(speciesNode.Element(_speciesActiveCmReloadPeriodMultiplierTagName).Value);
                        float buyoutCostMultiplier = float.Parse(speciesNode.Element(_speciesBuyoutCostMultiplierTagName).Value);
                        stat = new SpeciesStat(species, pluralName, description, atlasID, filename, sensorRangeMultiplier, weaponRangeMultiplier,
                            activeCmRangeMultiplier, weaponReloadPeriodMultiplier, activeCmReloadPeriodMultiplier, buyoutCostMultiplier);
                        break;
                    }
                }
                if (stat == default(SpeciesStat)) {
                    D.Error("{0} could not find Xml Node for Species {1}.", DebugName, species.GetValueName());
                }
                return stat;
            }

        }

        #endregion

    }
}


