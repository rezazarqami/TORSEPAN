#!/usr/bin/env python3
"""Opt-in build-host workaround; preserves all WebAssembly generation tasks.
Run after restore, then set TORSEPAN_WASM_TARGETS to the generated targets path.
"""
import json
from pathlib import Path
import sys
from xml.sax.saxutils import quoteattr

repo = Path(__file__).resolve().parents[1]
assets = json.loads((repo / "TORSEPAN.Panel.Offline/obj/project.assets.json").read_text())
pack = next((v["path"] for k, v in assets["libraries"].items()
             if k.lower().startswith("microsoft.net.sdk.webassembly.pack/")), None)
if pack is None:
    raise SystemExit("Restore the offline project before preparing the workaround.")
assembly = next((Path(root) / pack / "tools/net10.0/Microsoft.NET.Sdk.WebAssembly.Pack.Tasks.dll"
                 for root in assets["packageFolders"]
                 if (Path(root) / pack / "tools/net10.0/Microsoft.NET.Sdk.WebAssembly.Pack.Tasks.dll").is_file()), None)
if assembly is None:
    raise SystemExit("Restored WebAssembly task assembly was not found.")
output = Path(sys.argv[1]).resolve()
names = ["GenerateWasmBootJson", "ComputeWasmBuildAssets", "ComputeWasmPublishAssets", "ConvertDllsToWebCil"]
output.write_text("<Project>\n" + "".join(
    '<UsingTask TaskName="Microsoft.NET.Sdk.WebAssembly.' + name + '" AssemblyFile=' +
    quoteattr(str(assembly)) + ' Override="true" />\n' for name in names) + "</Project>\n")
print(output)
