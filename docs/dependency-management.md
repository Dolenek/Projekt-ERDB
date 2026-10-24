# Dependency management

Python puzzle tools pin their dependencies in `tools/puzzle/requirements.txt`.
Use Python 3.12 and install this file into an isolated environment. Pillow 13.0.0
provides the image decoding, encoding and rendering used by these tools.
NuGet dependencies are declared in the four C# project files.

## Automated updates

`.github/dependabot.yml` checks pip, all four NuGet project directories (including
PuzzleReplay), and GitHub Actions every Monday at 09:00 Europe/Prague time.
Updates target the repository's default branch, `main`. Each ecosystem
configuration permits up to five open version-update pull requests.

Dependabot security updates are enabled in the GitHub repository settings.
Security fixes can open pull requests independently of the weekly version-update
schedule and its open-pull-request limit. Pull requests require manual merging.

## Verification

Run from the repository root in Windows PowerShell:

```powershell
py -3.12 -m venv .venv
.\.venv\Scripts\python.exe -m pip install -r tools/puzzle/requirements.txt
.\.venv\Scripts\python.exe -m pip check
.\.venv\Scripts\python.exe -m unittest discover -s tools/puzzle/tests -v
```

For an image-library update, also check decoding PNG, JPEG and WebP, converting
color modes, resizing images and rendering a review sheet from synthetic inputs.
Use [development checks](development.md) for C# builds, tests and the CI workflow.

After a security fix reaches `main`, check the repository's Dependabot alerts
after dependency analysis finishes. Alerts should close through patched versions;
do not dismiss them to substitute for an actual fix.
