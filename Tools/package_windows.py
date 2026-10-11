"""Package every companion file of the verified Windows build, then validate it."""
from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED
import hashlib
import json

ROOT = Path(__file__).resolve().parents[1]
BUILD = ROOT / "Builds" / "CoreboundWindows"
OUTPUT = ROOT / "Downloads" / "Corebound-Windows.zip"
OUTPUT.parent.mkdir(parents=True, exist_ok=True)
assert (BUILD / "Corebound.exe").is_file(), "Windows build is missing"
files = sorted(p for p in BUILD.rglob("*") if p.is_file())
with ZipFile(OUTPUT, "w", ZIP_DEFLATED, compresslevel=6) as archive:
    for p in files:
        archive.write(p, "Corebound-Windows/" + p.relative_to(BUILD).as_posix())
with ZipFile(OUTPUT) as archive:
    assert archive.testzip() is None
    names = archive.namelist()
    assert len(names) == len(files)
    for p in files:
        name = "Corebound-Windows/" + p.relative_to(BUILD).as_posix()
        assert hashlib.sha256(archive.read(name)).digest() == hashlib.sha256(p.read_bytes()).digest()
    assert "Corebound-Windows/Corebound.exe" in names
    assert "Corebound-Windows/UnityPlayer.dll" in names
    assert any(n.startswith("Corebound-Windows/Corebound_Data/") for n in names)
    assert any(n.startswith("Corebound-Windows/MonoBleedingEdge/") for n in names)
print(json.dumps({"archive": str(OUTPUT), "files": len(files), "bytes": OUTPUT.stat().st_size,
                  "sha256": hashlib.sha256(OUTPUT.read_bytes()).hexdigest()}, indent=2))
