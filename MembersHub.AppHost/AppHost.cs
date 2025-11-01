var builder = DistributedApplication.CreateBuilder(args);

// Add Redis cache
var cache = builder.AddRedis("cache");

// Add PostgreSQL database with persistent volume
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()  // Persists data between container restarts
    .WithPgAdmin();    // Optional: adds pgAdmin for database management

var membershubdb = postgres.AddDatabase("membershubdb");

// Add services with database and cache references
var apiService = builder.AddProject<Projects.MembersHub_ApiService>("apiservice")
    .WithReference(cache)
    .WithReference(membershubdb);

var worker = builder.AddProject<Projects.MembersHub_Worker>("worker");

builder.AddProject<Projects.MembersHub_Web>("web")
    .WithReference(apiService)
    .WithReference(cache)
    .WithReference(membershubdb)
    .WithExternalHttpEndpoints();

builder.Build().Run();
