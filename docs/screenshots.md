# Screenshots

README screenshots are published from `docs/assets/screenshots/`:

- `swagger-overview.png` - Swagger/OpenAPI overview from the local RIAYA API.
- `appointments-workflow.png` - Appointment endpoints including create, confirm, cancel, check-in, complete, and no-show workflow routes.
- `medical-complex-structure.png` - Departments, clinic rooms, and doctor clinic assignments.
- `billing-payments.png` - Medical services, invoices, and payments.
- `auth-endpoints.png` - Auth register, login, refresh, and revoke endpoints.
- `dashboard-endpoint.png` - Dashboard overview endpoint.

Legacy screenshots remain in `docs/screenshots/` and were visually inspected before selected copies were moved into the new asset path.

Not captured in this environment:

- `tests-passed.png` - No real terminal screenshot facility was used. The test result is documented in `README.md` and the final audit report instead.
- `docker-running.png` - Docker was installed, but the Docker daemon was not running, so no real `riaya-api` / `riaya-sqlserver` screenshot was captured.

Publication checklist:

- Screenshots must show RIAYA only.
- Screenshots must not show bearer tokens, refresh tokens, secrets, or production credentials.
- Screenshots must not show real patient data.
- README image links must point only to existing files under `docs/assets/`.
- Docker and test screenshots should be added only after capturing real, current local evidence.
