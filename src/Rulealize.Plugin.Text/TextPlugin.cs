// Copyright (c) 2026 Reny
// Licensed under the Apache License, Version 2.0.

using Rulealize.Abstraction.Plugin;

namespace Rulealize.Plugin.Text
{
    /// <summary>Questions about text over the <c>str</c> namespace: its length, its parts, and what it is made of.</summary>
    /// <remarks>
    /// <para>
    /// Every operation answers the same on every runtime, which is why there is no pattern. A
    /// regular expression is read by whichever dialect is underneath, and a rule set whose answer
    /// depended on that would not be one rule set; TypeSchema leaves a pattern out of
    /// <c>type.string</c> for the same reason. What a pattern would have checked is written with
    /// <c>str.slice</c>, <c>str.length</c> and <c>str.only</c>, whose classes are fixed ranges of
    /// characters rather than Unicode categories.
    /// </para>
    /// <para>
    /// Every operand has to be text, and null is no exception, as with arithmetic: "contains, in a
    /// value that is not there" has no answer, and a value that might be null is flattened with
    /// <c>cmp.coalesce</c> first.
    /// </para>
    /// </remarks>
    public sealed class TextPlugin : IRulealizePlugin
    {
        /// <inheritdoc />
        public PluginManifest Manifest { get; } =
            new("Rulealize.Plugin.Text", new Version(1, 0, 0), "str");

        /// <inheritdoc />
        public void Register(IPluginRegistry registry)
        {
            ArgumentNullException.ThrowIfNull(registry);

            registry.AddExpression("trim", TrimNode.Build);
            registry.AddExpression("length", LengthNode.Build);
            registry.AddExpression("contains", AffixNode.BuildContains);
            registry.AddExpression("startsWith", AffixNode.BuildStartsWith);
            registry.AddExpression("endsWith", AffixNode.BuildEndsWith);
            registry.AddExpression("slice", SliceNode.Build);
            registry.AddExpression("only", OnlyNode.Build);
        }
    }
}
