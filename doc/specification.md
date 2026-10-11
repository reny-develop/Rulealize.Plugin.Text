# Rulealize.Plugin.Text

| | |
| --- | --- |
| Identifier | `Rulealize.Plugin.Text` |
| Namespace | `str` |
| Version | `1.0.0` |
| Reserved prefix | none |
| Depends on | [the value model](https://github.com/reny-develop/Rulealize.Abstraction/blob/main/doc/value-model.md), and nothing else |
| Notation | [how a plugin specification is written](https://github.com/reny-develop/Rulealize.Abstraction/blob/main/doc/specification-notation.md) |

Questions about `Text`: how long it is, what it holds, and what it is made of.

## Nodes

| Node | Kind |
| --- | --- |
| `str.trim` | expression |
| `str.length` | expression |
| `str.contains` / `str.startsWith` / `str.endsWith` | expression |
| `str.slice` | expression |
| `str.only` | expression |

---

## The same answer on every runtime

**There is no regular expression.** A pattern is read by whichever dialect is underneath, and
a rule set whose answer depended on that would not be one rule set. [TypeSchema](https://github.com/reny-develop/Rulealize.Plugin.TypeSchema/blob/main/doc/specification.md)
leaves a pattern out of `type.string` for the same reason.

For the same reason nothing here asks the runtime's Unicode tables. A class of character is a
list of code ranges written below; white space is a list of characters written below; case is
not folded and text is not normalized. A Unicode category, or "is a letter", would answer
differently on a runtime with newer tables, and the rule set's answer would move with it.

Length and position are counted in UTF-16 code units, as `type.string` counts `minLength` and
`maxLength`, so a guard measuring a text and the schema holding the same bound agree. Every
character the classes below name is one unit.

## Null and kinds

**Every operand that takes text faults when it is not `Text`, and `Null` is no exception**, as
every operand of [Arithmetic](https://github.com/reny-develop/Rulealize.Plugin.Arithmetic/blob/main/doc/specification.md)
faults when it is not a number. "Contains, in a value that is not there" has no answer. A value
that might be `Null` is flattened with `cmp.coalesce` first.

---

## `str.trim`

### Form

```jsonc
{ "op": "str.trim", "value": <expression:Text> }
```

### How it evaluates

`value` without the white space at either end. White space is exactly these: tab (U+0009), line
feed (U+000A), carriage return (U+000D), space (U+0020) and the ideographic space (U+3000), which
a Japanese input method types. Nothing between the ends is touched.

---

## `str.length`

### Form

```jsonc
{ "op": "str.length", "value": <expression:Text> }
```

### How it evaluates

The length of `value` as a `Number`, in UTF-16 code units.

---

## `str.contains` / `str.startsWith` / `str.endsWith`

### Form

```jsonc
{ "op": "str.contains",   "value": <expression:Text>, "part": <expression:Text> }
{ "op": "str.startsWith", "value": <expression:Text>, "part": <expression:Text> }
{ "op": "str.endsWith",   "value": <expression:Text>, "part": <expression:Text> }
```

### How it evaluates

Whether `value` holds `part` anywhere, at its start, or at its end, compared code unit by code
unit: case matters, and `"Tokyo"` does not start with `"tokyo"`. Every text holds the empty text.

### A part that begins with a reserved character

A literal written in the rule set document that begins with a reserved character is read as
that shorthand — `"@"` as a parameter, `"$"` as a field — so `"part": "@"` does not ask whether
an address holds an at sign. Text like that is [written as a `literal`
node](https://github.com/reny-develop/Rulealize/blob/main/doc/ruleset-guide.md#text-that-begins-with-a-reserved-character):

```jsonc
{ "op": "str.contains", "value": "@email", "part": { "op": "literal", "value": "@" } }
```

A literal with such a character only after its first, `"a@"`, needs none of this, and neither
does text that arrives in an argument or a state document.

---

## `str.slice`

### Form

```jsonc
{ "op": "str.slice", "value": <expression:Text>, "from": <expression:Number>, "count": <expression:Number> }
```

`count` is optional.

### How it evaluates

The part of `value` that starts at `from`, counting from zero, and runs for `count` code units,
or to the end where `count` is left out.

A slice reaching past the end is cut short rather than refused, as reading past the end of a
sequence is not a fault: from `"abc"`, `from: 1, count: 10` is `"bc"` and `from: 5` is `""`. The
length is the thing to check, and checking it is the rule set's to write.

### Errors

| Condition | When |
| --- | --- |
| `from` or `count` is negative or not a whole number | evaluation |

### Example

A postal code, three digits, a hyphen and four digits, written where a pattern would have been:

```jsonc
{ "op": "logic.and", "all": [
  { "op": "cmp.eq", "left": { "op": "str.length", "value": "@to" }, "right": 8 },
  { "op": "str.only", "value": { "op": "str.slice", "value": "@to", "from": 0, "count": 3 }, "of": ["asciiDigit"] },
  { "op": "cmp.eq", "left": { "op": "str.slice", "value": "@to", "from": 3, "count": 1 }, "right": "-" },
  { "op": "str.only", "value": { "op": "str.slice", "value": "@to", "from": 4 }, "of": ["asciiDigit"] } ] }
```

---

## `str.only`

### Form

```jsonc
{ "op": "str.only", "value": <expression:Text>, "of": ["<class>", …] }
```

`of` is static: at least one class, each one of the names below.

### How it evaluates

`true` where every code unit of `value` is in at least one of the classes `of` names. The empty
text is made only of anything, so it is `true`; whether it may be empty is a length to check, or
the schema's `minLength`.

### The classes

| Class | Code units |
| --- | --- |
| `asciiDigit` | `0`–`9` (U+0030–U+0039). Not the full-width `０`–`９` |
| `asciiUpper` | `A`–`Z` (U+0041–U+005A) |
| `asciiLower` | `a`–`z` (U+0061–U+007A) |
| `asciiLetter` | `asciiUpper` and `asciiLower` |
| `space` | the space, U+0020 |
| `hyphen` | the hyphen-minus, U+002D |
| `hiragana` | U+3041–U+3096 and U+309D–U+309F, and the long vowel mark `ー` (U+30FC) |
| `katakana` | U+30A1–U+30FF, which holds the long vowel mark `ー` and the middle dot `・` |

A name read aloud is often written in katakana with a space or a middle dot between its parts,
`ヤマダ タロウ` or `ジョン・スミス`, which is why `katakana` holds the dot and `space` exists to be
named beside it.

### Errors

| Condition | When |
| --- | --- |
| `of` is empty, or names a class not in the table | build |

---

## Decided

- **No pattern.** Argued above; a rule set checks a shape with `str.slice`, `str.length` and
  `str.only`, which is longer to write and means the same thing everywhere.
- **No case folding, no normalization.** Both depend on the runtime's Unicode tables. A rule
  set that wants `"Tokyo"` and `"tokyo"` to be one name compares against both.
- **No classes by Unicode category.** "A letter" or "a digit" in every script would move with
  the tables; the classes are the ranges a form in Japan and in ASCII actually asks for, and a
  class is added by writing its ranges here.
- **Positions in UTF-16 code units**, as `type.string` counts length. A character outside the
  Basic Multilingual Plane is two units, and `str.only` refuses it, since no class holds half of
  one.
