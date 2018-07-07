// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: NewGameConfigurator.cs
// Singleton. Makes various new game configuration settings available via XML.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using System.Collections.Generic;

    /// <summary>
    /// Singleton. Makes various new game configuration settings available via XML.
    /// TODO Consolidate into/with GameEnumExtensions.
    /// </summary>
    public class NewGameConfigurator : AGenericSingleton<NewGameConfigurator> {

        private NewGameConfigXmlReader _xmlReader;

        private NewGameConfigurator() {
            Initialize();
        }

        protected override void Initialize() {
            // WARNING: Do not use Instance or _instance in here as this is still part of Constructor
            _xmlReader = NewGameConfigXmlReader.Instance;
        }

        public IEnumerable<string> GetTechNamesThatStartCompleted(EmpireStartLevel startLevel) {
            return _xmlReader.GetCompletedTechNamesFor(startLevel);
        }

        #region Nested Classes

        private class NewGameConfigXmlReader : AXmlReader<NewGameConfigXmlReader> {

            private string _completedStartingTechsTagName = "CompletedStartingTechs";
            private string _empireStartLevelTagName = "EmpireStartLevel";
            private string _startLevelNameTagName = "StartLevelName";
            private string _completedTechTagName = "CompletedTech";

            protected override string XmlFilename { get { return "NewGameConfigValues"; } }

            private NewGameConfigXmlReader() {
                Initialize();
            }

            protected override void InitializeValuesAndReferences() {
                base.InitializeValuesAndReferences();
            }

            internal IEnumerable<string> GetCompletedTechNamesFor(EmpireStartLevel startLevel) {
                IList<string> completedTechNames = new List<string>();
                var startingTechsNode = _xElement.Element(_completedStartingTechsTagName);
                var startLevelNodes = startingTechsNode.Elements(_empireStartLevelTagName);
                foreach (var startLevelNode in startLevelNodes) {
                    var startLevelNodeAttribute = startLevelNode.Attribute(_startLevelNameTagName);
                    string startLevelName = startLevelNodeAttribute.Value;
                    EmpireStartLevel startLevelFound = Enums<EmpireStartLevel>.Parse(startLevelName);
                    if (startLevel == startLevelFound) {
                        var techNameNodes = startLevelNode.Elements(_completedTechTagName);
                        foreach (var techNameNode in techNameNodes) {
                            string techNameNodeValue = techNameNode.Value;
                            if (techNameNode.IsEmpty || techNameNodeValue == string.Empty) {
                                continue;
                            }
                            completedTechNames.Add(techNameNodeValue);
                        }
                    }
                }
                return completedTechNames;
            }

        }

        #endregion

    }
}


