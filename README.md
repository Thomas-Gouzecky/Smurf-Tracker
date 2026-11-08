# Smurf Tracking Application

This is a simple application for tracking smurfs.

## Configuring OpenIddict scopes

The auth service reads scopes and their token resources from the `OpenIddict:Scopes`
configuration section. The defaults are in
[`apps/auth/appsettings.json`](./apps/auth/appsettings.json), and can be overridden
in a mounted configuration file or with Docker environment variables. For example:

```text
OpenIddict__Scopes__0__Name=smurftracker-read
OpenIddict__Scopes__0__DisplayName=Read SmurfTracker data
OpenIddict__Scopes__0__Resources__0=smurftracker-api
OpenIddict__Scopes__1__Name=smurftracker-write
OpenIddict__Scopes__1__DisplayName=Write SmurfTracker data
OpenIddict__Scopes__1__Resources__0=smurftracker-api
```

Restart the auth container after changing the configuration. Scope changes are
applied to the OpenIddict database during startup.

The API gateway validates the configured resource audience through
`Authentication:Audience` (for Docker, use
`Authentication__Audience=smurftracker-api`).
