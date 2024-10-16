using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();
builder.Services.AddAWSService<IAmazonS3>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", (ILogger<Program> logger) =>
{
    logger.LogInformation("Weather forecast endpoint hit at {Time}.", DateTime.UtcNow);
    
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
});

app.MapGet("/images", async (ILogger<Program> logger, IConfiguration configuration, IAmazonS3 s3Client) =>
{
    logger.LogInformation("Images endpoint hit at {Time}.", DateTime.UtcNow);

    var imageUrls = new List<GalleryImage>();
    
    var bucketName = configuration.GetValue<string>("AWS:Resources:S3BucketName") ?? 
                      throw new Exception("S3 Bucket not found in the configuration.");
    
    var listRequest = new ListObjectsRequest
    {
        BucketName = bucketName
    };

    // Make a single request to list objects
    var response = await s3Client.ListObjectsAsync(listRequest);

    // Process the returned objects
    foreach (var entry in response.S3Objects)
    {
        // Generate a pre-signed URL for the object
        var url = s3Client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = bucketName,
            Key = entry.Key,
            Expires = DateTime.UtcNow.AddMinutes(1)
        });

        imageUrls.Insert(0, new GalleryImage(url));
    }

    return imageUrls;
});

app.MapDefaultEndpoints();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

record GalleryImage(string Url)
{
    
}