// Copyright (c) 2026 Reny
// Licensed under the Apache License, Version 2.0.

using System.Text.Json;
using Rulealize.Abstraction;
using Rulealize.Plugin.Binding;
using Rulealize.Plugin.Comparison;
using Rulealize.Plugin.Logic;
using Rulealize.Plugin.State;
using Rulealize.Plugin.TypeSchema;

namespace Rulealize.Plugin.Text.Tests;

/// <summary>Questions about text, answered the same on every runtime.</summary>
public sealed class TextTests
{
    private static readonly RuleRuntime Runtime = new RuleRuntime()
        .AddPlugin(new TextPlugin())
        .AddPlugin(new TypeSchemaPlugin())
        .AddPlugin(new StatePlugin())
        .AddPlugin(new BindingPlugin())
        .AddPlugin(new LogicPlugin())
        .AddPlugin(new ComparisonPlugin());

    // ── str.trim ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData("\"  alice \"", "alice")]
    [InlineData("\"\\talice\\r\\n\"", "alice")]
    [InlineData("\"　山田　\"", "山田")]
    [InlineData("\"a b\"", "a b")]
    [InlineData("\"   \"", "")]
    public void TrimTakesOffTheWhiteSpaceAtEitherEndAndNothingBetween(string value, string trimmed) =>
        Assert.Equal(trimmed, Evaluated($$"""{ "op": "str.trim", "value": {{value}} }""").GetString());

    // ── str.length ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData("\"\"", 0)]
    [InlineData("\"alice\"", 5)]
    [InlineData("\"山田\"", 2)]
    public void LengthCountsAsTypeStringCountsMaxLength(string value, int length) =>
        Assert.Equal(length, Evaluated($$"""{ "op": "str.length", "value": {{value}} }""").GetInt32());

    // ── str.contains, str.startsWith, str.endsWith ─────────────────────────

    [Theory]
    [InlineData("str.contains", "\"alice@example.com\"", "\"example\"", true)]
    [InlineData("str.contains", "\"alice\"", "\"example\"", false)]
    [InlineData("str.contains", "\"alice@example.com\"", "{ \"op\": \"literal\", \"value\": \"@\" }", true)]
    [InlineData("str.contains", "\"alice\"", "{ \"op\": \"literal\", \"value\": \"@\" }", false)]
    [InlineData("str.startsWith", "{ \"op\": \"literal\", \"value\": \"$100\" }", "{ \"op\": \"literal\", \"value\": \"$\" }", true)]
    [InlineData("str.startsWith", "\"Tokyo\"", "\"To\"", true)]
    [InlineData("str.startsWith", "\"Tokyo\"", "\"to\"", false)]
    [InlineData("str.endsWith", "\"report.pdf\"", "\".pdf\"", true)]
    [InlineData("str.endsWith", "\"report.pdf\"", "\".PDF\"", false)]
    [InlineData("str.contains", "\"anything\"", "\"\"", true)]
    public void APartIsSoughtCharacterByCharacterWithCaseMattering(string op, string value, string part, bool held) =>
        Assert.Equal(held, Evaluated($$"""{ "op": "{{op}}", "value": {{value}}, "part": {{part}} }""").GetBoolean());

    // ── str.slice ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData("\"123-4567\"", 0, 3, "123")]
    [InlineData("\"123-4567\"", 4, null, "4567")]
    [InlineData("\"123-4567\"", 3, 1, "-")]
    [InlineData("\"abc\"", 1, 10, "bc")]
    [InlineData("\"abc\"", 5, 1, "")]
    public void ASliceIsCutShortAtTheEndRatherThanRefused(string value, int from, int? count, string sliced) =>
        Assert.Equal(sliced, Evaluated(count is null
            ? $$"""{ "op": "str.slice", "value": {{value}}, "from": {{from}} }"""
            : $$"""{ "op": "str.slice", "value": {{value}}, "from": {{from}}, "count": {{count}} }""").GetString());

    [Theory]
    [InlineData("-1", "str.slice.from")]
    [InlineData("1.5", "str.slice.from")]
    public void APositionNoTextHasIsAFault(string from, string origin) =>
        Assert.Equal(origin, Assert.Throws<RuleEvaluationException>(
            () => Evaluated($$"""{ "op": "str.slice", "value": "abc", "from": {{from}} }""")).Origin);

    // ── str.only ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData("\"1234567\"", "\"asciiDigit\"", true)]
    [InlineData("\"１２３\"", "\"asciiDigit\"", false)]
    [InlineData("\"Abc\"", "\"asciiLetter\"", true)]
    [InlineData("\"Abc\"", "\"asciiLower\"", false)]
    [InlineData("\"ヤマダ タロウ\"", "\"katakana\", \"space\"", true)]
    [InlineData("\"ジョン・スミス\"", "\"katakana\"", true)]
    [InlineData("\"やまだ\"", "\"katakana\"", false)]
    [InlineData("\"らーめん\"", "\"hiragana\"", true)]
    [InlineData("\"\"", "\"asciiDigit\"", true)]
    public void EveryCharacterHasToBeOfOneOfTheClassesNamed(string value, string of, bool only) =>
        Assert.Equal(only, Evaluated($$"""{ "op": "str.only", "value": {{value}}, "of": [{{of}}] }""").GetBoolean());

    [Theory]
    [InlineData("[]", "names at least one class of character")]
    [InlineData("[\"digit\"]", "names classes out of asciiDigit, asciiUpper, asciiLower, asciiLetter, space, hyphen, hiragana, katakana, and 'digit' is none of them")]
    public void AClassNobodyDefinedIsRefusedWhenTheRuleSetIsBuilt(string of, string said) =>
        Assert.Contains(said, Assert.Throws<RuleSetBuildException>(
            () => Runtime.CreateContext(Probe($$"""{ "op": "str.only", "value": "1", "of": {{of}} }"""))).Message, StringComparison.Ordinal);

    // ── null, and kinds ────────────────────────────────────────────────────

    [Theory]
    [InlineData("""{ "op": "str.length", "value": null }""", "str.length.value")]
    [InlineData("""{ "op": "str.contains", "value": "a", "part": 1 }""", "str.contains.part")]
    public void AnOperandThatIsNotTextIsAFaultAndNullIsNoException(string expression, string origin) =>
        Assert.Equal(origin, Assert.Throws<RuleEvaluationException>(() => Evaluated(expression)).Origin);

    // ── where a rule set uses them ─────────────────────────────────────────

    /// <summary>A postal code typed in, held to three digits, a hyphen and four digits, with no pattern.</summary>
    private const string Address = """
        {
          "id": "address", "version": "1.0.0",
          "requires": [
            { "plugin": "Rulealize.Plugin.Text" }, { "plugin": "Rulealize.Plugin.TypeSchema" }, { "plugin": "Rulealize.Plugin.State" },
            { "plugin": "Rulealize.Plugin.Binding" }, { "plugin": "Rulealize.Plugin.Logic" }, { "plugin": "Rulealize.Plugin.Comparison" }
          ],
          "state": { "schema": { "postal": { "op": "type.string", "maxLength": 8 } }, "initial": { "postal": "" } },
          "inputs": {
            "setPostal": {
              "params": { "to": { "open": { "field": "postal" } } },
              "validate": [
                { "require": { "op": "logic.and", "all": [
                    { "op": "cmp.eq", "left": { "op": "str.length", "value": "@to" }, "right": 8 },
                    { "op": "str.only", "value": { "op": "str.slice", "value": "@to", "from": 0, "count": 3 }, "of": ["asciiDigit"] },
                    { "op": "cmp.eq", "left": { "op": "str.slice", "value": "@to", "from": 3, "count": 1 }, "right": "-" },
                    { "op": "str.only", "value": { "op": "str.slice", "value": "@to", "from": 4 }, "of": ["asciiDigit"] } ] },
                  "code": "postal.malformed" }
              ],
              "effects": [ { "op": "state.set", "path": "postal", "value": "@to" } ]
            }
          }
        }
        """;

    [Fact]
    public void APostalCodeIsCheckedWithSlicesAndClassesWhereAPatternWouldHaveBeen()
    {
        RuleContext context = Runtime.CreateContext(Address);

        string state = context.ApplyToState("""{ "input": "setPostal", "args": { "to": "123-4567" } }""", context.InitialState).State;
        using (JsonDocument document = JsonDocument.Parse(state))
        {
            Assert.Equal("123-4567", document.RootElement.GetProperty("data").GetProperty("postal").GetString());
        }

        foreach (string malformed in new[] { "1234567", "12-34567", "123-456a", "１２３-4567" })
        {
            InputRejectedException refused = Assert.Throws<InputRejectedException>(
                () => context.ApplyToState($$"""{ "input": "setPostal", "args": { "to": "{{malformed}}" } }""", context.InitialState));
            Assert.Equal("postal.malformed", Assert.Single(refused.Rejections).Code);
        }
    }

    /// <summary>What an expression evaluates to, read as the projection of a rule set that holds only it.</summary>
    private static JsonElement Evaluated(string expression)
    {
        RuleContext context = Runtime.CreateContext(Probe(expression));
        using JsonDocument value = JsonDocument.Parse(context.Project("value", context.InitialState));
        return value.RootElement.Clone();
    }

    private static string Probe(string expression) => $$"""
        {
          "id": "probe", "version": "1.0.0",
          "requires": [ { "plugin": "Rulealize.Plugin.Text" }, { "plugin": "Rulealize.Plugin.TypeSchema" } ],
          "state": { "schema": { "unused": { "op": "type.bool" } }, "initial": { "unused": false } },
          "inputs": { "nothing": { "effects": [] } },
          "projections": { "value": {{expression}} }
        }
        """;
}
