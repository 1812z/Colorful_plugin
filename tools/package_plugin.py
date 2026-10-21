"""Package the Release output for upload to the Artemis workshop."""
import json
from pathlib import Path
import zipfile

root = Path(__file__).resolve().parents[1]
source = root / "src/Artemis.Plugins.Colorful/bin/Release/net10.0"
metadata = json.loads((source / "plugin.json").read_text(encoding="utf-8-sig"))
output = root / f"Artemis.Plugins.Colorful-{metadata['Version']}.zip"
required = [
    "plugin.json",
    metadata["Main"],
    "Artemis.Plugins.Colorful.deps.json",
    "Artemis.Plugins.Colorful.runtimeconfig.json",
    "HidSharp.dll",
]
for name in required:
    if not (source / name).is_file():
        raise FileNotFoundError(source / name)
with zipfile.ZipFile(output, "w", zipfile.ZIP_DEFLATED) as archive:
    for name in required:
        archive.write(source / name, name)
with zipfile.ZipFile(output) as archive:
    assert archive.testzip() is None
    assert set(archive.namelist()) == set(required)
    assert json.loads(archive.read("plugin.json"))["Version"] == "1.0.3.0"
print(f"Created: {output}")
print(f"Version: {metadata['Version']}; {len(required)} files; {output.stat().st_size:,} bytes")
print("PASS: ZIP integrity, root plugin.json, main assembly and runtime dependencies.")
