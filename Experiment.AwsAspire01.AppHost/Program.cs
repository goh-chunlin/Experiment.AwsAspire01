using Amazon;

var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.Experiment_AwsAspire01_ApiService>("apiservice");

// Setup a configuration for the AWS .NET SDK.
var awsConfig = builder.AddAWSSDKConfig()
                        .WithProfile("default")
                        .WithRegion(RegionEndpoint.USEast1);

var awsResources = builder
    // Provision app-level resources defined in the CloudFormation template file
    .AddAWSCloudFormationTemplate("aspire01-us-east-1", "Infrastructure/main.yaml")
    // Add the SDK configuration so the AppHost
    // knows what account/region to provision the resources.
    .WithReference(awsConfig);

builder.AddProject<Projects.Experiment_AwsAspire01_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(awsResources)
    .WithReference(awsConfig)
    .WithReference(apiService);

builder.Build().Run();