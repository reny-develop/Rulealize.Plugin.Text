// Copyright (c) 2026 Reny
// Licensed under the Apache License, Version 2.0.

using Rulealize.Abstraction;
using Rulealize.Abstraction.Building;
using Rulealize.Abstraction.Evaluation;
using Rulealize.Abstraction.Node;
using Rulealize.Abstraction.Value;

namespace Rulealize.Plugin.Text
{
    /// <summary><c>value</c> without the white space at either end.</summary>
    /// <remarks>
    /// White space is the fixed set the specification lists, not whatever the runtime's Unicode
    /// tables call white space today, so a name trimmed on one machine is trimmed the same on
    /// another.
    /// </remarks>
    internal sealed class TrimNode(ExpressionNode value) : ExpressionNode
    {
        public static ExpressionNode Build(INodeBuildContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            return new TrimNode(context.RequireExpression("value"));
        }

        public override RuleValue Evaluate(IEvaluationContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            return RuleValue.Text(value.Evaluate(context).AsText("str.trim.value").Trim(CharacterClass.WhiteSpace));
        }
    }

    /// <summary>How long <c>value</c> is.</summary>
    /// <remarks>
    /// Counted the way <c>type.string</c> counts <c>maxLength</c>, in UTF-16 code units, so that a
    /// guard comparing a length with a bound and the schema holding the same bound agree.
    /// </remarks>
    internal sealed class LengthNode(ExpressionNode value) : ExpressionNode
    {
        public static ExpressionNode Build(INodeBuildContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            return new LengthNode(context.RequireExpression("value"));
        }

        public override RuleValue Evaluate(IEvaluationContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            return RuleValue.Number(value.Evaluate(context).AsText("str.length.value").Length);
        }
    }

    /// <summary>Which of the three questions about a part an <see cref="AffixNode"/> asks.</summary>
    internal enum Affix
    {
        Contains,
        StartsWith,
        EndsWith,
    }

    /// <summary>Whether <c>value</c> holds <c>part</c>: anywhere, at its start, or at its end.</summary>
    /// <remarks>
    /// Compared character by character, so case matters and nothing is folded or normalized:
    /// <c>"Tokyo"</c> does not start with <c>"tokyo"</c>. Every text holds the empty text.
    /// </remarks>
    internal sealed class AffixNode(Affix affix, ExpressionNode value, ExpressionNode part) : ExpressionNode
    {
        public static ExpressionNode BuildContains(INodeBuildContext context) => Build(context, Affix.Contains);

        public static ExpressionNode BuildStartsWith(INodeBuildContext context) => Build(context, Affix.StartsWith);

        public static ExpressionNode BuildEndsWith(INodeBuildContext context) => Build(context, Affix.EndsWith);

        public override RuleValue Evaluate(IEvaluationContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            string origin = affix switch
            {
                Affix.Contains => "str.contains",
                Affix.StartsWith => "str.startsWith",
                _ => "str.endsWith",
            };
            string text = value.Evaluate(context).AsText($"{origin}.value");
            string sought = part.Evaluate(context).AsText($"{origin}.part");

            return RuleValue.Boolean(affix switch
            {
                Affix.Contains => text.Contains(sought, StringComparison.Ordinal),
                Affix.StartsWith => text.StartsWith(sought, StringComparison.Ordinal),
                _ => text.EndsWith(sought, StringComparison.Ordinal),
            });
        }

        private static ExpressionNode Build(INodeBuildContext context, Affix affix)
        {
            ArgumentNullException.ThrowIfNull(context);
            return new AffixNode(affix, context.RequireExpression("value"), context.RequireExpression("part"));
        }
    }

    /// <summary>The part of <c>value</c> that starts at <c>from</c> and runs for <c>count</c>, or to the end.</summary>
    /// <remarks>
    /// <para>
    /// What a pattern would have been for. A postal code is eight long, three digits, a hyphen and
    /// four digits, and each of those is a slice and a check — longer to write than a pattern, and
    /// read the same way by every runtime.
    /// </para>
    /// <para>
    /// A slice reaching past the end is cut short rather than refused, as reading past the end of a
    /// sequence is not a fault: the length is the thing to check, and checking it first is the rule
    /// set's to write. A position that is negative or not whole is a fault, since no text has one.
    /// </para>
    /// </remarks>
    internal sealed class SliceNode(ExpressionNode value, ExpressionNode from, ExpressionNode? count) : ExpressionNode
    {
        public static ExpressionNode Build(INodeBuildContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            return new SliceNode(context.RequireExpression("value"), context.RequireExpression("from"), context.OptionalExpression("count"));
        }

        public override RuleValue Evaluate(IEvaluationContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            string text = value.Evaluate(context).AsText("str.slice.value");
            int start = Position(from.Evaluate(context), "str.slice.from");
            int length = count is null ? int.MaxValue : Position(count.Evaluate(context), "str.slice.count");

            if (start >= text.Length)
            {
                return RuleValue.Text(string.Empty);
            }

            return RuleValue.Text(text.Substring(start, Math.Min(length, text.Length - start)));
        }

        private static int Position(RuleValue value, string origin)
        {
            decimal number = value.AsNumber(origin);
            if (number < 0 || decimal.Truncate(number) != number)
            {
                throw new RuleEvaluationException(origin, $"A position in a text is a whole number from zero, and {RuleValue.FormatNumber(number)} is not.");
            }

            return number > int.MaxValue ? int.MaxValue : (int)number;
        }
    }

    /// <summary>Whether every character of <c>value</c> is of one of the classes <c>of</c> names.</summary>
    /// <remarks>
    /// <para>
    /// The classes are fixed ranges of characters the specification lists, not Unicode categories,
    /// because a category is whatever the runtime's tables say it is and a rule set's answer would
    /// move with the runtime. That is the same reason there is no pattern.
    /// </para>
    /// <para>
    /// The empty text is made only of anything, so it passes; whether it may be empty is a length
    /// to check, or the schema's <c>minLength</c>.
    /// </para>
    /// </remarks>
    internal sealed class OnlyNode(ExpressionNode value, IReadOnlyList<CharacterClass> classes) : ExpressionNode
    {
        public static ExpressionNode Build(INodeBuildContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            IReadOnlyList<string> named = context.RequireStringArray("of");
            if (named.Count == 0)
            {
                throw context.Error("of", "names at least one class of character.");
            }

            List<CharacterClass> classes = [];
            foreach (string name in named)
            {
                classes.Add(CharacterClass.Named(name)
                    ?? throw context.Error("of", $"names classes out of {string.Join(", ", CharacterClass.All.Select(each => each.Name))}, and '{name}' is none of them."));
            }

            return new OnlyNode(context.RequireExpression("value"), classes);
        }

        public override RuleValue Evaluate(IEvaluationContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            string text = value.Evaluate(context).AsText("str.only.value");
            foreach (char character in text)
            {
                if (!classes.Any(each => each.Holds(character)))
                {
                    return RuleValue.Boolean(false);
                }
            }

            return RuleValue.Boolean(true);
        }
    }
}
