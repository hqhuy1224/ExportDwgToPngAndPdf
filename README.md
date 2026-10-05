# DwgExportPro

Batch-export AutoCAD drawings (DWG/DXF) to **PDF** or **PNG** — pick a layout per file, queue up a whole folder, and let it run unattended.

DwgExportPro opens each drawing through AutoCAD's COM automation, computes the real drawing extents (filtering stray/outlier geometry so the output isn't thrown off by a misplaced block or an infinite line), centers the plot window, and exports to PDF or PNG with automatic retries if a plot comes back blank.

> Built on top of the original **ScriptPro** batch-processing engine released by Autodesk Developer Network, rewritten with a dedicated PDF/PNG export pipeline and a more reliable AutoCAD COM automation layer. See [Credits](#credits).

---

## Features

- **Batch PDF / PNG export** — queue any number of DWG/DXF files and export them in one run
- **Per-drawing layout selection** — pick Model or any paper space layout independently for each file
- **Smart plot-area detection** — computes the drawing's real bounding box per file instead of relying on a fixed `Extents`/`Display` setting:
  - Collects bounding boxes of all visible, plottable entities
  - Filters outliers with a median + MAD (median absolute deviation) statistical filter, so a stray block or misplaced dimension far from the drawing doesn't shrink everything else down
  - Keeps large entities (title blocks, borders) even if their center is off from the main cluster
  - Adds a small margin and centers the result in the plot window
- **Automatic fallback** — if a plot comes back blank (checked via pixel-ink ratio for PNG), retries with `Extents`, then `Display`, before giving up
- **Reliable AutoCAD automation** — runs on a dedicated STA thread with a COM message filter registered, eliminating the classic `RPC_E_SERVERCALL_RETRYLATER` / "application is busy" errors during long batch runs
- **File-verified success** — a drawing is only marked "Done" when the output file actually exists and has content, not just because AutoCAD returned without throwing
- **Attach-or-launch AutoCAD** — reuses an already-running AutoCAD instance when present (and leaves it open afterward), or launches and owns its own instance for unattended runs
- **Auto-restart AutoCAD** every N drawings to keep memory usage under control on large batches
- **Reusable project files** (`.bpl`) — save a drawing list + settings and re-run it later, including from the command line
- **Command-line / unattended mode** — run a saved project from a batch file or Task Scheduler, with silent exit
- **Legacy ScriptPro project import** — can read older ScriptPro (`.scp`) project files for migration

## Requirements

- Windows 10/11, 64-bit
- AutoCAD 2024 or later (officially tested range; older COM-compatible versions may work but are unsupported)
- .NET 8.0 Desktop Runtime (x64)
- A licensed, installed copy of AutoCAD on the machine running the tool

## Installation

**Installer (recommended):** run the MSI installer; it installs to Program Files and adds a Start Menu shortcut.

**Portable:** extract the build output and run `DwgExportProUI.exe` directly — no installation needed.

## Usage

1. Add drawings via **Add** (individual files) or **Add From Folder** (optionally recursive)
2. For each drawing, pick the layout (or `Model`) to export in the grid
3. Choose **PDF** or **PNG** as the export format
4. (Optional) Configure timeout, AutoCAD restart interval, startup script, output folder etc. under **Options**
5. Click **Run → Checked** (or **Selected** / **Failed**)
6. Review the log — each run writes a summary log and a detailed AutoCAD command-line log

### Command line

```bat
DwgExportProUI.exe "C:\Projects\MyProject.bpl" run
DwgExportProUI.exe "C:\Projects\MyProject.bpl" run exit   :: exits silently when done
```

Useful for scheduled/unattended batch runs via Task Scheduler.

## How plot-area detection works

For drawings plotted from Model space, DwgExportPro doesn't just use AutoCAD's `Extents` or `Display` plot area — both can be thrown off by geometry far outside the "real" drawing (a stray imported block, an accidental point placed at huge coordinates, etc.). Instead it:

1. Walks every entity in the active space and collects its bounding box via COM, skipping infinite entities (`XLINE`/`RAY`), hidden/frozen/non-plottable layers, and invalid bounding boxes
2. Computes the median center of all entities and the median absolute deviation (MAD) — a measurement of spread that isn't skewed by a few extreme outliers
3. Drops entities whose center is more than ~20× the MAD away from the median, *unless* that entity is large (e.g. a title block or border), in which case it's kept
4. Builds the final bounding box from the surviving entities, adds a small margin, and sets it as the plot window with the plot centered on the page/image
5. If the resulting plot comes back blank, falls back to `Extents`, then `Display`, trying again

This keeps batch exports consistent even across drawings with messy geometry, without needing per-file manual adjustment.

## Building from source

- Visual Studio 2022
- .NET 8.0 SDK
- Open the solution, restore NuGet packages, build

## Project structure

| Project | Description |
|---|---|
| `DwgExportProUI` | WPF host application (ribbon UI, entry point) |
| `DrawingListUC` | Drawing list user control — project management, AutoCAD COM automation, PDF/PNG export engine |

## Credits

DwgExportPro is a derivative work built on **ScriptPro**, originally released by Autodesk Developer Network:

- **Original ScriptPro 2.0** — Virupaksha Aithal, with input from Kean Walmsley
- **.NET 8.0 modernization (base engine)** — Madhukar Moogala
- **DwgExportPro** — PDF/PNG batch export pipeline, per-drawing layout picker, outlier-filtered plot-area detection, and AutoCAD COM reliability fixes (STA thread + message filter, file-verified export status)

Icons: [FatCow Free Icons](http://www.fatcow.com/free-icons)

## License

This project is a derivative of ScriptPro source code originally released by Autodesk, Inc. under a permissive "use, copy, modify, and distribute in object code form" grant, provided the original copyright notice and disclaimer are preserved. See [LICENSE](LICENSE) for the full text, which covers both the inherited ScriptPro code and the modifications in this repository.

**Original copyright © 2010–2026 Autodesk, Inc.** Provided "AS IS", without warranty of any kind.
Modifications and the DwgExportPro name are © 2026 this project's author, distributed under the same terms.
