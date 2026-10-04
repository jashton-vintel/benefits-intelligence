import re
from collections.abc import Iterable

# Words that turn a description of differences into a judgement or advice. The prompt forbids
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


def judgement_words(summary: str) -> list[str]:
    return sorted({match.group(0).lower() for match in _JUDGEMENT.finditer(summary)})


def unsupported_numbers(summary: str, facts: Iterable[str]) -> list[str]:
    """Figures in the summary that appear nowhere in the facts it was written from."""
    known = {_number(match) for fact in facts for match in _NUMBER.findall(fact)}
    found = {_number(match) for match in _NUMBER.findall(summary)}
    return sorted(found - known)


def _number(text: str) -> str:
    # "12,000" and "12000" are the same figure; a trailing ".00" adds nothing.
    value = text.replace(",", "")
    return value[:-3] if value.endswith(".00") else value
