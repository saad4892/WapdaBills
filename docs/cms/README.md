# CMS operations (WAPDA Bills)

## Stack

- ASP.NET Core 9 + EF Core SQLite (`App_Data/wapdabill.db`)
- Admin at `/admin` (cookie auth, hashed `AdminUsers`)
- Public pages remain server-rendered Razor

## First deploy / local reset

1. Backup existing `App_Data/wapdabill.db` and `wwwroot/media`.
2. Ensure writable `App_Data` and `wwwroot/media` on the host.
3. On startup the app runs `Database.MigrateAsync()`, then seeds content + bootstrap admin.
4. Default bootstrap admin username comes from `Admin:Username` (default `admin`).
5. Password comes from `Admin:BootstrapPassword` or `Admin:Password` on **first** empty `AdminUsers` table only. Change it after login.

Example `appsettings` / env:

```json
"Admin": {
  "Username": "admin",
  "BootstrapPassword": "set-a-strong-password"
}
```

Do **not** commit production passwords.

## Modules

| URL | Purpose |
|-----|---------|
| `/admin/settings` | Site name, default SEO, GA4/GTM/GSC (Admin role) |
| `/admin/nav` | Primary navigation items |
| `/admin/footer` | Disclaimer, copyright, social URLs |
| `/admin/pages` | Legal/tool/info pages |
| `/admin/discos` | DISCO landings (PITC fields Admin-only) |
| `/admin/homepage` | Homepage section items |
| `/admin/guides` | Existing guide CRUD |
| `/admin/media` | Image library (`wwwroot/media`) |
| `/admin/redirects` | 301/302 + 404 log |
| `/admin/export` | JSON content export |
| `/admin/import` | Content-page upsert import |

## Migrations

```bash
dotnet ef database update --project WapdaBill.csproj
```

Startup already calls `MigrateAsync()`. Never run destructive `EnsureDeleted` in production.

## Backup

- Copy `App_Data/wapdabill.db*`
- Copy `wwwroot/media/`
- Optionally download `/admin/export` JSON (metadata/paths only; not binary files)

## Smoke QA checklist

1. `dotnet run` — startup migrates + seeds without errors.
2. `/` — hero/sections/footer match prior design; EN/UR toggle still works.
3. `/lesco-bill` (and one region hub) — meta + intro from CMS when seeded.
4. `/privacy-policy` — CMS body (or LegalContent fallback).
5. `/admin/login` — bootstrap admin from `Admin:BootstrapPassword`; plaintext config password unused after seed.
6. `/admin/settings`, `/admin/nav`, `/admin/pages`, `/admin/media`, `/admin/redirects`, `/admin/export`.
7. `/sitemap.xml` — includes published pages/guides; `/robots.txt` Disallow `/admin`.
8. Unknown path → 404 page + row in NotFoundLogs (admin redirects screen).

## Known soft gaps (non-blocking)

- Import currently upserts **content pages** primarily; use export JSON + manual edits for full restore.
- WebP conversion is not forced on upload (original format stored).
- Quill CDN loads only on admin page editors (`/admin/pages/edit`).
