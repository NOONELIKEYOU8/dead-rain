# dead-rain

## Quick start

- Unity version: 2022.3.52f1 (see ProjectSettings/ProjectVersion.txt)
- If after cloning you only see simple pink cubes and no ground/player/enemies, the most common causes are:
	1. Scene (.unity) files and Prefab (.prefab) files are missing from the repository.
	2. Unity `.meta` files were ignored and not tracked — Unity needs `.meta` files so references (prefabs, sprites, scenes) keep working.

### Quick fixes

1. Ensure `.gitignore` does not ignore `*.meta` (already updated in this repo). If you previously ignored `.meta`, re-add them and commit:

```bash
git add .gitignore
git commit -m "Allow Unity .meta files"
git add -A
git commit -m "Add Unity metadata and missing assets (scenes, prefabs, sprites)"
git push
```

2. If scene or prefab binary files were never committed, restore them from your local copy or backup and commit the `.unity` and `.prefab` files together with their `.meta` files.

3. For large binary assets (art/audio), consider using Git LFS: https://git-lfs.github.com/

See `Assets/Docs/SETUP_SCENE.md` for manual steps to recreate the sample scene if needed.