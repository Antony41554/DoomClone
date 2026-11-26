"""Read-only byte verification of declared Evolution outputs after Editor jobs finish."""
from pathlib import Path
import hashlib
import json

ROOT = Path(__file__).resolve().parents[1]
ACTIVE = ROOT / "Temp/UkubolaAutomation/active.json"
if ACTIVE.exists():
    print(json.dumps({"success": False, "deferred": True,
                      "error": "An Editor job is active; wait for its settings/output restoration."}))
    raise SystemExit(2)

errors = []
checked = 0
manifests = ["Assets/_UKUBOLA/Art/Evolution/SOURCE_MANIFEST.json",
             "Assets/_UKUBOLA/Generated/Evolution/GENERATION_MANIFEST.json"]
for name in manifests:
    manifest = json.loads((ROOT / name).read_text(encoding="utf-8-sig"))
    seen = set()
    for entry in manifest["files"]:
        relative = entry["path"]
        path = (ROOT / relative).resolve()
        owned = (relative.startswith("Assets/_UKUBOLA/Art/Evolution/Textures/")
                 or relative.startswith("Assets/_UKUBOLA/Generated/Evolution/")
                 or relative in ("Assets/_UKUBOLA/Scenes/Evolution/ArtRoom.unity",
                                 "Assets/_UKUBOLA/Scenes/Evolution/ArtRoom.unity.meta")
                 or relative.startswith("Assets/_UKUBOLA/Scenes/Evolution/ArtRoom/")
                 or relative in ("Assets/_UKUBOLA/Scenes/Evolution/OutpostSlice.unity",
                                 "Assets/_UKUBOLA/Scenes/Evolution/OutpostSlice.unity.meta")
                 or relative.startswith("Assets/_UKUBOLA/Scenes/Evolution/OutpostSlice/"))
        if not owned or not path.is_relative_to(ROOT) or relative in seen:
            errors.append("Unowned or duplicate output: " + relative)
            continue
        seen.add(relative)
        checked += 1
        if not path.is_file() or hashlib.sha256(path.read_bytes()).hexdigest() != entry["sha256"]:
            errors.append("Changed or missing output: " + relative)
    if "generatorSha256" in manifest:
        source = ROOT / "Tools/Generate-UkubolaArt.py"
        if hashlib.sha256(source.read_bytes()).hexdigest() != manifest["generatorSha256"]:
            errors.append("Texture source differs from its recorded generator hash.")
    for source in manifest.get("normalizedSources", []):
        path=(ROOT / source["path"]).resolve()
        if not path.is_relative_to(ROOT) or not path.is_file():
            errors.append("Missing or external generator source: " + source["path"])
        elif hashlib.sha256(path.read_text(encoding="utf-8-sig").replace("\r\n", "\n").encode("utf-8")).hexdigest() != source["sha256"]:
            errors.append("Generator source differs from recorded content: " + source["path"])

print(json.dumps({"success": not errors, "checkedFiles": checked, "errors": errors}, indent=2))
raise SystemExit(bool(errors))
