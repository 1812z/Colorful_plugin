"""Create a global Artemis profile using only the installed Color brush.

Usage: python effects/generate_random_hold.py PATH_TO_ARTEMIS_RAINBOW_ZIP
Colors are randomized at generation time, then loop after 16 steps.
"""
import colorsys
import copy
import json
import random
import sys
import uuid
import zipfile
from pathlib import Path


def values(obj):
    return obj["$values"] if isinstance(obj, dict) else obj


def encode(obj):
    return json.dumps(obj, ensure_ascii=False, separators=(",", ":"))


def main():
    with zipfile.ZipFile(sys.argv[1]) as archive:
        profile = json.loads(archive.read("profile.json"))
        config = json.loads(archive.read("configuration.json"))

    name = "Random Color - 15s Lite"
    profile_id = str(uuid.uuid4())
    old_id = profile["Id"]

    def replace_id(obj):
        if isinstance(obj, dict):
            return {k: replace_id(v) for k, v in obj.items()}
        if isinstance(obj, list):
            return [replace_id(v) for v in obj]
        return profile_id if obj == old_id else obj

    profile = replace_id(profile)
    config = replace_id(config)
    config["IsSuspended"] = False
    profile["Name"] = config["Name"] = name
    layer = next(layer for layer in values(profile["Layers"])
                 if "LinearGradientBrush" in layer["LayerBrush"]["BrushType"])
    profile["Layers"]["$values"] = [layer]
    layer["Name"] = "All devices - hold 13s, fade 2s"
    # Import adaptation selects every connected device, including future imports.
    values(layer["Leds"]).clear()
    for hint in values(layer["AdaptionHints"]):
        if "DeviceType" in hint:
            hint["DeviceType"] = -1
    for prop in values(layer["GeneralPropertyGroup"]["Properties"]):
        if prop["Identifier"] == "BrushReference":
            ref = json.loads(prop["Value"])
            ref["BrushType"] = "SolidBrush"
            prop["Value"] = encode(ref)

    rng = random.SystemRandom()
    count = 16
    duration = count * 15
    colors = []
    hue = rng.random()
    for _ in range(count):
        # Keep neighboring hues visibly distinct and saturation/brightness stable.
        hue = (hue + rng.uniform(0.18, 0.82)) % 1
        rgb = colorsys.hsv_to_rgb(hue, 0.85, 1.0)
        colors.append("#ff" + "".join(f"{round(c * 255):02x}" for c in rgb))
    stops = []
    for i, color in enumerate(colors):
        stops.extend([
            {"Color": color, "Position": i * 15 / duration},
            {"Color": color, "Position": (i * 15 + 13) / duration},
        ])
    stops.append({"Color": colors[0], "Position": 1.0})

    brush = layer["LayerBrush"]
    # The historical profile schema stores namespace-qualified brush type names.
    brush["BrushType"] = "Artemis.Plugins.LayerBrushes.Color.SolidBrush"
    template = values(brush["PropertyGroup"]["Properties"])[0]
    props = []
    for key, value in [("ColorMode", 1), ("Colors", stops), ("AnimationSpeed", 60 / duration)]:
        prop = copy.deepcopy(template)
        prop["Identifier"] = key
        prop["Value"] = encode(value)
        props.append(prop)
    brush["PropertyGroup"]["Properties"]["$values"] = props

    output = Path(__file__).parent / "Random-Color-15s-Lite.zip"
    with zipfile.ZipFile(output, "w", zipfile.ZIP_DEFLATED) as archive:
        archive.writestr("configuration.json", encode(config))
        archive.writestr("profile.json", encode(profile))

    # Validate the exported archive and exact hold/fade boundaries.
    with zipfile.ZipFile(output) as archive:
        assert archive.testzip() is None
        saved = json.loads(archive.read("profile.json"))
        assert json.loads(archive.read("configuration.json"))["IsSuspended"] is False
        saved_layer = values(saved["Layers"])[0]
        assert not values(saved_layer["Leds"])
        assert "SolidBrush" in saved_layer["LayerBrush"]["BrushType"]
        assert "ColorBrushProvider" in saved_layer["LayerBrush"]["ProviderId"]
        assert len(values(saved["Layers"])) == 1
    assert len(stops) == count * 2 + 1
    assert all(b["Position"] - a["Position"] > 0.0011
               for a, b in zip(stops, stops[1:])), "Artemis gradient spacing limit"
    for i in range(count):
        start, hold, end = stops[i * 2:i * 2 + 3]
        assert start["Color"] == hold["Color"]
        assert abs((hold["Position"] - start["Position"]) * duration - 13) < 1e-8
        assert abs((end["Position"] - hold["Position"]) * duration - 2) < 1e-8
    print(f"Created {output}: {count} random colors, {duration}s loop, 13s hold + 2s fade.")
    print("PASS: archive integrity, global adaptation, solid brush, all hold/fade intervals.")


if __name__ == "__main__":
    main()
