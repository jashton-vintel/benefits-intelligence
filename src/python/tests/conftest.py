from decimal import Decimal

import pytest

from app.models.document import DocumentChunk
from app.models.extraction import PolicyExtraction


class FakeExtractor:
    def __init__(self, result: PolicyExtraction) -> None:
        self.result = result
        self.received: list[DocumentChunk] = []

    async def extract(self, chunks: list[DocumentChunk]) -> PolicyExtraction:
        self.received = chunks
        return self.result


@pytest.fixture
def fake_extractor() -> FakeExtractor:
    return FakeExtractor(
        PolicyExtraction(
            provider="Atlas Healthcare",
            scheme_name="Corporate Plus",
            annual_excess=Decimal("100.00"),
        )
    )
