# RIAYA Asset Manifest

All visual assets in this folder were generated locally for RIAYA. No internet images, stock images, third-party logos, placeholder screenshots, AI-generated remote images, or real patient data were used.

## Graphics

SVG diagrams are stored in `docs/assets/graphics/`:

- `hero-banner.svg`
- `project-overview.svg`
- `architecture-overview.svg`
- `business-modules.svg`
- `appointment-workflow.svg`
- `visit-prescription-workflow.svg`
- `billing-payments-workflow.svg`
- `security-authorization.svg`
- `testing-quality.svg`
- `technology-stack.svg`
- `demo-seed-data.svg`
- `repository-structure.svg`

PNG copies for graphics were not generated in this environment. ImageMagick, Inkscape, `rsvg-convert`, CairoSVG, Sharp, and Canvas were unavailable, and local-file browser rendering was blocked. The SVG files are the publication assets and are readable directly on GitHub.

## Screenshots

Screenshots are stored in `docs/assets/screenshots/`:

- `swagger-overview.png`
- `appointments-workflow.png`
- `medical-complex-structure.png`
- `billing-payments.png`
- `auth-endpoints.png`
- `dashboard-endpoint.png`

`swagger-overview.png`, `auth-endpoints.png`, and `dashboard-endpoint.png` were captured from the local RIAYA Swagger UI during this documentation pass. The appointments, medical-complex, and billing screenshots were copied from existing verified RIAYA Swagger screenshots in `docs/screenshots/` after visual inspection.

Not captured:

- `tests-passed.png`: no real terminal screenshot facility was used.
- `docker-running.png`: Docker daemon was not running, so no real Docker screenshot was captured.

## Publication Safety

- No bearer tokens are shown.
- No refresh tokens are shown.
- No production secrets are shown.
- No real patient data is shown.
- No local machine paths are shown in README images.
- Screenshots show RIAYA Swagger content only.
