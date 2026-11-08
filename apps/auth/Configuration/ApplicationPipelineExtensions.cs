public static class ApplicationPipelineExtensions
{
    public static WebApplication UseApplicationPipeline(
        this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint(
                    "/swagger/v1/swagger.json",
                    "Auth API v1");
                options.OAuthClientId("swagger-ui");
                options.OAuthUsePkce();
                var oauthScopes = new[] { "openid" }
                    .Concat(OpenIddictScopes.GetRequired(app.Configuration)
                        .Scopes
                        .Select(scope => scope.Name))
                    .ToArray();
                options.OAuthScopes(oauthScopes);
            });
        }

        app.UseDeveloperExceptionPage();
        app.UseExceptionHandler();
        app.UseHttpsRedirection();

        app.UseCors(options =>
        {
            options.WithOrigins("http://localhost:5118")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });

        app.UseAuthentication();
        app.UseMiddleware<SessionValidationMiddleware>();
        app.UseAuthorization();

        return app;
    }
}
