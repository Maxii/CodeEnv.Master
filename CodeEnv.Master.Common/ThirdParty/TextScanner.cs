﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: TextScanner.cs
// SbText Scanner with easy to use syntax for reading text, booleans and values.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    using System.IO;
    using System.Text;

    /// <summary>
    /// TextScanner similar to Java's Scanner class that provides for easy syntax progressively reading
    /// strings and parsing them into words, booleans, ints or floats. The parsing is done with <i>space</i>
    /// as a delimiter. There is no alternative delimiter at this point.
    /// Usage: Use File.ReadAllLines(path) to generate an array of lines from the text file at path,
    /// then use TextScanner to analyze each line for content.
    /// </summary>
    public class TextScanner : StringReader {

        public string DebugName { get { return GetType().Name; } }

        private string _currentWord;

        public TextScanner(string source)
            : base(source) {
            ReadNextWord();
        }

        private void ReadNextWord() {
            StringBuilder sb = new StringBuilder();
            char nextChar;
            int next;
            do {
                next = this.Read();
                if (next < 0) {
                    // -1 no more chars
                    break;
                }
                nextChar = (char)next;
                if (char.IsWhiteSpace(nextChar)) {
                    // first white space so value probably contains a word
                    break;
                }
                sb.Append(nextChar);
            }
            while (true);

            while ((this.Peek() >= 0) && (char.IsWhiteSpace((char)this.Peek()))) {
                // move through whitespace
                this.Read();
            }
            if (sb.Length > 0) {
                _currentWord = sb.ToString();
            }
            else {
                _currentWord = null;
            }
        }

        public bool HasNextInt() {
            if (_currentWord == null) {
                return false;
            }
            int dummy;
            return int.TryParse(_currentWord, out dummy);
        }

        public int NextInt() {
            try {
                return int.Parse(_currentWord);
            }
            finally {
                ReadNextWord();
            }
        }

        public bool HasNextFloat() {
            if (_currentWord == null) {
                return false;
            }
            double dummy;
            return double.TryParse(_currentWord, out dummy);
        }

        public float NextFloat() {
            try {
                return float.Parse(_currentWord);
            }
            finally {
                ReadNextWord();
            }
        }

        public bool HasNextBoolean() {
            if (_currentWord == null) {
                return false;
            }
            bool dummy;
            return bool.TryParse(_currentWord, out dummy);
        }

        public bool NextBoolean() {
            try {
                return bool.Parse(_currentWord);
            }
            finally {
                ReadNextWord();
            }
        }

        public bool HasNext() {
            return _currentWord != null;
        }

        public string Next() {
            return _currentWord;
        }

        public sealed override int Read() {
            return base.Read();
        }

        public sealed override int Peek() {
            return base.Peek();
        }

        public override string ToString() {
            return DebugName;
        }

    }
}

