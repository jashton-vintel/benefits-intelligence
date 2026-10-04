from functools import cache

import tiktoken

# Encoding used by current OpenAI models. Chunk budgets are measured in the same units the
# model is billed and limited in, rather than approximated from character counts.
ENCODING_NAME = "o200k_base"


@cache
def _encoding() -> tiktoken.Encoding:
    # Loaded on first use: tiktoken fetches the encoding file once and caches it locally.
    return tiktoken.get_encoding(ENCODING_NAME)


def count_tokens(text: str) -> int:
    return len(_encoding().encode(text))
