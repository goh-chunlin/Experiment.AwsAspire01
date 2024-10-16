using Amazon.S3;
using Amazon.S3.Model;
using Experiment.AwsAspire01.ApiService.Services;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();
builder.Services.AddAWSService<IAmazonS3>();
builder.Services.AddSingleton<IS3Service, S3Service>();

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

app.MapGet("/images", async (IConfiguration configuration, IAmazonS3 s3Client, ILogger<Program> logger) =>
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

app.MapPost("/image", async ([FromBody]IFormFile file, 
    IConfiguration configuration, IS3Service s3Service, ILogger<Program> logger) =>
{
    var bucketName = configuration.GetValue<string>("AWS:Resources:S3BucketName") ?? 
                     throw new Exception("S3 Bucket not found in the configuration.");
    
    if (file.Length > 0 && file.ContentType.StartsWith("image/"))
    {
        // Read the file content as a stream from IBrowserFile
        await using var fileStream = file.OpenReadStream();

        // Create a byte array from the stream to get its length
        byte[] fileBytes;
        using (var memoryStream = new MemoryStream())
        {
            await fileStream.CopyToAsync(memoryStream);
            fileBytes = memoryStream.ToArray();
        }
    
        // Use HttpClient to send a PUT request to upload the file
        using var httpClient = new HttpClient();
        using var content = new ByteArrayContent(fileBytes);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        content.Headers.ContentLength = fileBytes.Length;

        // Generate the pre-signed URL for the upload
        Console.WriteLine("Generating PreSigned URL...");
        var preSignedUrl = s3Service.GeneratePreSignedUrl(bucketName, file.Name);
        Console.WriteLine($"PreSigned URL is {preSignedUrl}");

        // Send the PUT request to the pre-signed URL
        var response = await httpClient.PutAsync(preSignedUrl, content);

        if (response.IsSuccessStatusCode)
        {
            return Results.Ok(new { Message = "Image is uploaded successfully.", file.FileName });
        }

        return Results.BadRequest(new { Message = "Image cannot be uploaded.", file.FileName });
    }

    return Results.BadRequest(new { Message = "No image received or invalid format." });
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