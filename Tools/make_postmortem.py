"""Render the project's factual development post-mortem to a two-page PDF."""
from pathlib import Path
from xml.sax.saxutils import escape

from reportlab.lib import colors
from reportlab.lib.enums import TA_LEFT
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.utils import ImageReader
from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, PageBreak, Image, Preformatted, KeepTogether
from pypdf import PdfReader

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "output" / "pdf" / "Corebound_PostMortem.pdf"
OUT.parent.mkdir(parents=True, exist_ok=True)
SOURCE = ROOT / "Docs" / "CoreRunner_PostMortem.md"
INK = colors.HexColor("#163247")
TEAL = colors.HexColor("#087B80")
MUTED = colors.HexColor("#526575")
BODY = ParagraphStyle("Body", fontName="Helvetica", fontSize=10.5, leading=15,
                      textColor=INK, spaceAfter=8)
HEADING = ParagraphStyle("Heading", parent=BODY, fontName="Helvetica-Bold", fontSize=12,
                         leading=16, textColor=TEAL, spaceBefore=10, spaceAfter=7)
SMALL = ParagraphStyle("Small", parent=BODY, fontSize=8.5, leading=11, textColor=MUTED)
TITLE = ParagraphStyle("Title", parent=BODY, fontName="Helvetica-Bold", fontSize=24, leading=28, spaceAfter=8)
CODE = ParagraphStyle("Code", parent=BODY, fontName="Courier", fontSize=9, leading=13, backColor=colors.HexColor("#EFF5F6"), borderPadding=8, spaceAfter=12)


def para(text, style=BODY):
    return Paragraph(escape(text), style)


def footer(canvas, doc):
    canvas.setStrokeColor(TEAL)
    canvas.setLineWidth(.8)
    canvas.line(48, 45, A4[0] - 48, 45)
    canvas.setFont("Helvetica", 8)
    canvas.setFillColor(MUTED)
    canvas.drawString(48, 31, "COREBOUND  |  UNITY 101  |  DEVELOPMENT POST-MORTEM")
    canvas.drawRightString(A4[0] - 48, 31, f"{doc.page} / 2")


sections = {}
current = None
for block in SOURCE.read_text(encoding="utf-8").strip().split("\n\n"):
    if block.startswith("## "):
        current = block[3:]
        sections[current] = []
    elif current:
        sections[current].append(block)

story = [para("Corebound", TITLE),
         para("Development Post-Mortem", ParagraphStyle("Subtitle", parent=HEADING, fontSize=16, leading=20, spaceBefore=0)),
         para("Unity 101 Final Project  |  Unity 6000.0.23f1", SMALL), Spacer(1, 10)]

for title in ["Problem: floor labels competing with modal UI", "Investigation and an incomplete hypothesis"]:
    story.append(para(title, HEADING))
    story.extend(para(p) for p in sections[title])

story.append(para("Solution", HEADING))
story.extend(para(p) for p in sections["Solution"])
story.append(PageBreak())

story.append(para("Result and lessons", TITLE))
story.append(para("Visibility follows the game state", HEADING))
story.append(Preformatted("label.enabled = !g.Paused && !g.Won;\nmessage.text = g.Paused || g.Won ? \"\" : g.Message;", CODE))

screenshot = ROOT / "Verification" / "03-pause.png"
width = 440
iw, ih = ImageReader(str(screenshot)).getSize()
story.append(Image(str(screenshot), width=width, height=width * ih / iw))
story.append(Spacer(1, 6))
story.append(para("Figure 1. Final pause state from the running Windows player. Floor labels and temporary messages are hidden; Resume, Restart, Main Menu and Quit remain available.", SMALL))

for title in ["Verification and result", "What this taught me"]:
    story.append(para(title, HEADING))
    story.extend(para(p) for p in sections[title])
story.append(para("Project evidence", HEADING))
story.extend(para(p, SMALL) for p in sections["Project evidence"])

doc = SimpleDocTemplate(str(OUT), pagesize=A4, leftMargin=48, rightMargin=48,
                       topMargin=42, bottomMargin=60, title="Corebound - Development Post-Mortem",
                       author="", subject="Resolving world label interference with modal UI")
doc.build(story, onFirstPage=footer, onLaterPages=footer)
reader = PdfReader(str(OUT))
if len(reader.pages) != 2:
    raise RuntimeError(f"Expected two pages, got {len(reader.pages)}")
print("Created", OUT, "-", len(reader.pages), "pages")
