from pathlib import Path

import markdown
import pymupdf

SAMPLE_DATA = Path(__file__).resolve().parents[3] / "sample-data"
SOURCES = SAMPLE_DATA / "policies"

PAGE = pymupdf.paper_rect("a4")
BODY = PAGE + (56, 74, -56, -74)
GREY = (0.32, 0.38, 0.43)

# Fixed timestamps and no new document ID keep the output byte-identical between runs.
TIMESTAMP = "D:20260101090000Z"

CSS = """
* { font-family: sans-serif; font-size: 11pt; line-height: 1.6; color: #1f2933; }
h1 { font-size: 24pt; color: #0b3c5d; margin: 0 0 4pt 0; }
h2 { font-size: 15pt; color: #0b3c5d; margin: 18pt 0 6pt 0; }
h3 { font-size: 11pt; color: #0b3c5d; margin: 10pt 0 2pt 0; }
p, li { margin: 0 0 8pt 0; }
table { border-collapse: collapse; width: 100%; margin: 4pt 0 10pt 0; }
td, th { border: 1px solid #b0bec5; padding: 3pt 6pt; text-align: left; vertical-align: top; }
th { border-bottom: 2px solid #0b3c5d; }
"""


def render(source: Path) -> Path:
    converter = markdown.Markdown(extensions=["tables", "meta"])
    html = converter.convert(source.read_text(encoding="utf-8"))
    meta: dict[str, list[str]] = getattr(converter, "Meta", {})

    output = SAMPLE_DATA / meta["output"][0]
    _write_pages(html, output)
    _finish(output, header=meta["header"][0], title=meta["title"][0])
    return output


def _write_pages(html: str, output: Path) -> None:
    story = pymupdf.Story(html=html, user_css=CSS)
    writer = pymupdf.DocumentWriter(str(output))

    more = True
    while more:
        device = writer.begin_page(PAGE)
        more, _ = story.place(BODY)
        story.draw(device)
        writer.end_page()

    writer.close()


def _finish(output: Path, header: str, title: str) -> None:
    document = pymupdf.open(output)
    page_count = document.page_count

    for index in range(page_count):
        page = document.load_page(index)
        page.insert_text((56, 50), header, fontsize=8, color=GREY)
        footer = f"Page {index + 1} of {page_count}"
        page.insert_text((PAGE.width - 106, PAGE.height - 40), footer, fontsize=8, color=GREY)

    document.set_metadata({"title": title, "creationDate": TIMESTAMP, "modDate": TIMESTAMP})
    document.subset_fonts()
    document.save(output.with_suffix(".tmp"), garbage=3, deflate=True, no_new_id=True)
    document.close()
    output.with_suffix(".tmp").replace(output)


def main() -> None:
    for source in sorted(SOURCES.glob("*.md")):
        output = render(source)
        with pymupdf.open(output) as document:
            print(f"{source.name} -> {output.name} ({document.page_count} pages)")


if __name__ == "__main__":
    main()
