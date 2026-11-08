# Smurf Tracking Application

This is a simple application for tracking smurfs.

## Configuring OpenIddict scopes

The migration command reads scope definitions from `OpenIddict:Definitions`,
including display names and token resources. The defaults are in
[`apps/auth/appsettings.json`](./apps/auth/appsettings.json), and can be overridden
in a mounted configuration file or with Docker environment variables. For example:

```text
```

The auth service dynamically loads scope names from the OpenIddict database at
startup. Run the migration container with the `OpenIddict:Definitions`
configuration to update the database, then restart the auth container.

The API gateway validates the configured resource audience through
`Authentication:Audience` (for Docker, use
`Authentication__Audience=smurftracker-api`).
