"""Hash every file of a completed local build without changing the build."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess

parser = argparse.ArgumentParser()
parser.add_argument("build", type=Path)
parser.add_argument("output", type=Path)
args = parser.parse_args()
root = Path(__file__).resolve().parents[1]
build, output = args.build.resolve(), args.output.resolve()
if not build.is_relative_to(root / "Builds") or not output.is_relative_to(root):
    raise SystemExit("Build must be inside Builds and evidence inside the project.")
if output.is_relative_to(build) or output.exists():
    raise SystemExit("Evidence must be a new file outside the build.")
if not (build / "BUILD_COMPLETE.json").is_file() or not (build / "UKUBOLA.exe").is_file():
    raise SystemExit("A completed UKUBOLA build is required.")
if (root / "Temp/UkubolaAutomation/active.json").exists():
    raise SystemExit("Wait for the active Editor job before recording evidence.")

def digest(path):
    value = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            value.update(block)
    return value.hexdigest()

files = []
for path in sorted(build.rglob("*")):
    if not path.is_file():
        continue
    if not path.resolve().is_relative_to(build):
        raise SystemExit("Build contains an external linked file.")
    files.append({"path": path.relative_to(build).as_posix(),
                  "bytes": path.stat().st_size, "sha256": digest(path)})
sources = [path for path in (root / "Assets/_UKUBOLA").rglob("*")
           if path.is_file() and path.suffix in (".cs", ".shader", ".asmdef")]
source_records = [{"path": path.relative_to(root).as_posix(), "sha256": digest(path)}
                  for path in sorted(sources)]
commit = subprocess.run(["git", "rev-parse", "HEAD"], cwd=root, text=True,
                        capture_output=True, check=True).stdout.strip()
data = {"kind": "Local build inventory; not final product acceptance", "commit": commit,
        "build": build.relative_to(root).as_posix(), "files": files,
        "sourceFilesAtValidation": source_records,
        "sourceDigest": hashlib.sha256(json.dumps(source_records, sort_keys=True).encode()).hexdigest(),
        "editorResult": json.loads((build / "BUILD_COMPLETE.json").read_text(encoding="utf-8-sig"))}
output.parent.mkdir(parents=True, exist_ok=True)
with output.open("x", encoding="utf-8", newline="\n") as stream:
    json.dump(data, stream, indent=2)
print(json.dumps({"success": True, "files": len(files), "bytes": sum(f["bytes"] for f in files),
                  "evidence": str(output)}, indent=2))
