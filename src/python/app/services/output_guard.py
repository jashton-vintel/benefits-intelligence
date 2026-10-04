import re
from collections.abc import Iterable

# Words that turn a factual description into a judgement or advice. The prompt forbids
# them too, but the check is made in code so the guarantee does not depend on the model.
_JUDGEMENT = re.compile(
    r"\b("
    r"better|worse|best|worst|superior|inferior|preferable|prefer(?:red)?|"
    r"recommend(?:s|ed|ation|ations)?|advis(?:e|able|ed)|advice|should|ought|"
    r"good value|poor value|great|excellent|disappointing"
    r")\b",
    re.IGNORECASE,
)
_NUMBER = re.compile(r"\d[\d,]*(?:\.\d+)?")
_WORD = re.compile(r"[a-z0-9£]+")

# An evaluative word counts as quoted when it and the words after it appear in a source.
_QUOTED_PHRASE_WORDS = 3

# Policy wording often spells out small numbers ("up to ten sessions"), which a summary or
# answer may reasonably write as digits.
_NUMBER_WORDS = {
    word: str(value)
    for value, word in enumerate(
        "zero one two three four five six seven eight nine ten eleven twelve thirteen fourteen "
        "fifteen sixteen seventeen eighteen nineteen twenty".split()
    )
} | {"thirty": "30", "forty": "40", "fifty": "50", "sixty": "60", "hundred": "100"}
_NUMBER_WORD = re.compile(rf"\b({'|'.join(_NUMBER_WORDS)})\b", re.IGNORECASE)


def judgement_words(text: str, sources: Iterable[str] = ()) -> list[str]:
    """Evaluative words in the text that are not simply repeating the sources' own wording.

    Policies use some of these words themselves ("when recommended by a consultant"), so a word
    is allowed when it appears in a source followed by the same words as in the text.
    """
    quoted = [_words(source) for source in sources]
    found: set[str] = set()
    for match in _JUDGEMENT.finditer(text):
        phrase = _words(match.group(0) + text[match.end() :])[:_QUOTED_PHRASE_WORDS]
        if not any(_contains(source, phrase) for source in quoted):
            found.add(match.group(0).lower())
    return sorted(found)


def unsupported_numbers(text: str, sources: Iterable[str]) -> list[str]:
    """Figures in the text that appear nowhere in the sources it was written from."""
    known: set[str] = set()
    for source in sources:
        known |= {_number(match) for match in _NUMBER.findall(source)}
        known |= {_NUMBER_WORDS[word.lower()] for word in _NUMBER_WORD.findall(source)}
    found = {_number(match) for match in _NUMBER.findall(text)}
    return sorted(found - known)


def _words(text: str) -> list[str]:
    return _WORD.findall(text.lower())


def _contains(words: list[str], phrase: list[str]) -> bool:
    size = len(phrase)
    return any(words[index : index + size] == phrase for index in range(len(words) - size + 1))


def _number(text: str) -> str:
    # "12,000" and "12000" are the same figure; a trailing ".00" adds nothing.
    value = text.replace(",", "")
    return value[:-3] if value.endswith(".00") else value
