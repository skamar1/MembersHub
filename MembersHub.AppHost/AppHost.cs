var builder = DistributedApplication.CreateBuilder(args);

// Add services without database containers (for development without Docker)
var apiService = builder.AddProject<Projects.MembersHub_ApiService>("apiservice");

var worker = builder.AddProject<Projects.MembersHub_Worker>("worker");

builder.AddProject<Projects.MembersHub_Web>("web")
    .WithReference(apiService)
    .WithExternalHttpEndpoints();

builder.Build().Run();
