# External Assets

This folder stores source and large production assets that do not need to be
imported by Unity immediately.

Use it for:

- `Images/`: original illustrations, source PNG/JPG, PSD/PSB, references.
- `Audio/`: WAV/MP3/OGG masters, sound design sources.
- `Video/`: trailers, captured clips, temporary video references.
- `Models/`: FBX/BLEND/model source files if the project later uses 3D.
- `References/`: mood boards, screenshots, external design references.

Rules:

- Keep final Unity-ready assets under `Assets/UniversitySimulator/` after Unity
  imports them and generates `.meta` files.
- Keep source files here until they are approved for import.
- Large binary files are tracked by Git LFS through `.gitattributes`.
- Do not store private accounts, paid asset license files, or credentials here.
