# Rulealize.Plugin.Text

Questions about text for [Rulealize](https://github.com/reny-develop/Rulealize) rule sets.

| | |
| --- | --- |
| Plugin id | `Rulealize.Plugin.Text` |
| Namespace | `str` |
| Reserved prefix | none |
| Depends on | `Rulealize.Abstraction` |
| Specification | [doc/specification.md](doc/specification.md) |

`str.trim`, `length`, `contains`, `startsWith`, `endsWith`, `slice`, `only`. Comparing two texts
for equality is not here; that is `cmp.eq`, and it belongs to the plugin that owns comparison.

**There is no regular expression, and every answer is the same on every runtime.** A pattern is
read by whichever dialect is underneath, and a rule set whose answer depended on that would not
be one rule set — the reason [TypeSchema](https://github.com/reny-develop/Rulealize.Plugin.TypeSchema/blob/main/doc/specification.md)
leaves a pattern out of `type.string`. What a pattern would have checked is written with
`str.slice`, `str.length` and `str.only`, whose classes of character — `asciiDigit`,
`katakana` and the rest — are fixed ranges the specification lists, not Unicode categories that
move with the runtime's tables. A postal code is eight long, three digits, a hyphen and four
digits, and each of those is one check.

Every operand must be text: null is an evaluation error rather than an empty text, as it is for
arithmetic.

## Building

`dotnet build`. `Rulealize.Abstraction` restores from nuget.org like any other package, so
this repository builds on its own.

`dotnet test` runs the vocabulary's own tests, in `test/`: each operation evaluated by the
Rulealize runtime as the projection of a rule set, and a postal code checked in an input's
`validate`, where a rule set uses them. The runtime and the other vocabularies come from
nuget.org; the plugin under test is this repository's, handed to the runtime as an instance.

## License

Apache-2.0.
