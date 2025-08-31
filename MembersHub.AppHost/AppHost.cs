var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL database
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin();

var postgresdb = postgres.AddDatabase("membershubdb");

// Add Redis cache for session state
var redis = builder.AddRedis("redis");

// Add services with references to database and cache
var apiService = builder.AddProject<Projects.MembersHub_ApiService>("apiservice")
    .WithReference(postgresdb)
    .WithReference(redis);

var worker = builder.AddProject<Projects.MembersHub_Worker>("worker")
    .WithReference(postgresdb)
    .WithReference(redis);

builder.AddProject<Projects.MembersHub_Web>("web")
    .WithReference(postgresdb)
    .WithReference(redis)
    .WithReference(apiService)
    .WithExternalHttpEndpoints();

builder.Build().Run();
