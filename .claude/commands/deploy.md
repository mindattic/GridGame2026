Deploy the GridGame2026 landing page (`mindattic.com/gridgame2026.htm`) via **MindAttic.Deploy** (sibling repo at `D:\Projects\MindAttic\MindAttic.Deploy`).

Renders this repo's `README.md` through the catalog template (`template/index.template.htm`, Cyberspace theme, MindAttic.UiUx components loaded via jsDelivr) and FTPS-uploads the single-file result. One repo owns the whole FTP pipeline — there is no per-project deploy state in this folder.

Run this command and report the result:

```
powershell -NoProfile -ExecutionPolicy Bypass -Command "cd D:\Projects\MindAttic\MindAttic.Deploy; npm run deploy -- --only gridgame2026"
```

It will:

1. Render `D:\Projects\MindAttic\GridGame2026\README.md` through the catalog template.
2. FTPS-upload `out/gridgame2026.htm` to `/mindattic.com/gridgame2026.htm`.

After running, summarize the result and flag any failures.

Notes:
- Catalog entry: `MindAttic.Deploy/projects.json` -> `projects[]` slug `gridgame2026` (theme: Cyberspace).
- Credentials: MindAttic.Vault at `%APPDATA%\MindAttic\Deploy\ftp.json` (transitional fallback: `MindAttic.Deploy/secrets/ftp.json`, gitignored).
- GridGame2026 is a Unity project — this command only ships the landing page, not the game build.
