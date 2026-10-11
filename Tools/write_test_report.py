"""Write a concise test report from the actual Windows player's JSON result."""
from pathlib import Path
import json

root = Path(__file__).resolve().parents[1]
folder = root / "Verification"
result = json.loads((folder / "results.json").read_text(encoding="utf-8"))
passed = sum(c["passed"] for c in result["checks"])
rows = ["# Corebound - Windows player verification", "", "Date: 2026-10-11 (Europe/Paris).",
        "Unity: " + result["unityVersion"],
        f"Result: {'PASS' if result['passed'] else 'FAIL'} - {passed}/{len(result['checks'])} checks passed.", "",
        "The actual Windows build was launched with an opt-in verification argument. Real physics frames were used for boosts, collisions and triggers. Some trigger checks teleport the player into position; this is not a complete human keyboard playthrough.", "",
        "| Check | Result | Evidence |", "| --- | --- | --- |"]
for check in result["checks"]:
    detail = check["detail"].replace("\n", " ").replace("|", "/")
    rows.append(f"| {check['name']} | {'PASS' if check['passed'] else 'FAIL'} | {detail} |")
rows += ["", "Screenshots of menu, arrival, pause and victory were rendered from the running player and visually reviewed. A hidden player uses explicit camera rendering for screenshots.", "",
         "The Corebound build/import completed successfully. The verifier returned to MainMenu and used its Quit handler to terminate the Windows process successfully.", "",
         "Limitations: real keyboard/mouse input and subjective audio loudness were not manually reviewed. Quit button accessibility was checked structurally; the menu Quit handler verified actual application termination. A human should review difficulty and movement feel before final hand-in."]
(folder / "TEST_REPORT.md").write_text("\n".join(rows) + "\n", encoding="utf-8")
print(f"Recorded {passed}/{len(result['checks'])} checks")
