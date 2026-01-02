#:sdk Aspire.AppHost.Sdk@13.1.0

var builder = DistributedApplication.CreateBuilder(args);

// Add the Citus container instance.
var citus = builder
    .AddContainer("citus", "citusdata/citus:latest")
    .WithEndpoint(6543, 5432)
    .WithEnvironment("POSTGRES_PASSWORD", "password");

builder.Build().Run();
