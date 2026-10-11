// Copyright (c) 2026 Reny
// Licensed under the Apache License, Version 2.0.

namespace Rulealize.Plugin.Text
{
    /// <summary>A named, fixed set of characters, which <c>str.only</c> checks a text against.</summary>
    /// <remarks>
    /// Each is a list of code ranges written here and in the specification, and nothing else
    /// decides it: no Unicode category, no culture. A class that depended on the runtime's tables
    /// would answer differently on a runtime with newer ones.
    /// </remarks>
    internal sealed class CharacterClass
    {
        /// <summary>The white space <c>str.trim</c> takes off: tab, line feed, carriage return, space, and the ideographic space.</summary>
        public static readonly char[] WhiteSpace = ['\t', '\n', '\r', ' ', (char)0x3000];

        private readonly (char From, char To)[] _ranges;

        private CharacterClass(string name, params (char From, char To)[] ranges)
        {
            Name = name;
            _ranges = ranges;
        }

        /// <summary>Gets every class, in the order the specification lists them.</summary>
        public static IReadOnlyList<CharacterClass> All { get; } =
        [
            new("asciiDigit", ('0', '9')),
            new("asciiUpper", ('A', 'Z')),
            new("asciiLower", ('a', 'z')),
            new("asciiLetter", ('A', 'Z'), ('a', 'z')),
            new("space", (' ', ' ')),
            new("hyphen", ('-', '-')),
            new("hiragana", ((char)0x3041, (char)0x3096), ((char)0x309D, (char)0x309F), ((char)0x30FC, (char)0x30FC)),
            new("katakana", ((char)0x30A1, (char)0x30FF)),
        ];

        /// <summary>Gets the name a document writes the class by.</summary>
        public string Name { get; }

        /// <summary>The class a document names, or null where it names none.</summary>
        public static CharacterClass? Named(string name) => All.FirstOrDefault(each => each.Name == name);

        /// <summary>Whether a character is in the class.</summary>
        public bool Holds(char character) => _ranges.Any(range => character >= range.From && character <= range.To);
    }
}
