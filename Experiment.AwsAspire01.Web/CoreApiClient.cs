using Microsoft.AspNetCore.Components.Forms;

namespace Experiment.AwsAspire01.Web;

public class CoreApiClient(HttpClient httpClient)
{
    public async Task<WeatherForecast[]> GetWeatherAsync(int maxItems = 10, CancellationToken cancellationToken = default)
    {
        List<WeatherForecast>? forecasts = null;

        await foreach (var forecast in httpClient.GetFromJsonAsAsyncEnumerable<WeatherForecast>("/weatherforecast", cancellationToken))
        {
            if (forecasts?.Count >= maxItems)
            {
                break;
            }
            if (forecast is not null)
            {
                forecasts ??= [];
                forecasts.Add(forecast);
            }
        }

        return forecasts?.ToArray() ?? [];
    }
    
    public async Task<GalleryImage[]> GetGalleryImagesAsync(CancellationToken cancellationToken = default)
    {
        List<GalleryImage>? images = null;

        await foreach (var image in httpClient.GetFromJsonAsAsyncEnumerable<GalleryImage>("/images", cancellationToken))
        {
            if (image is null) continue;
            
            images ??= [];
            images.Add(image);
        }

        return images?.ToArray() ?? [];
    }

    public async Task<bool> PostGalleryImageAsync(IBrowserFile file, CancellationToken cancellationToken = default)
    {
        var content = new MultipartFormDataContent();
        
        // Resize the image file
        var resizedFile = await file.RequestImageFileAsync(file.ContentType, 1024, 768);
        
        // Prepare the file content for uploading
        var fileContent = new StreamContent(resizedFile.OpenReadStream(cancellationToken: cancellationToken));
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(resizedFile.ContentType);
        
        // Add the file to the form content
        content.Add(fileContent, "image", resizedFile.Name); // "image" is the name of the form field
        
        var response = await httpClient.PostAsync("/image", content, cancellationToken);

        return response.IsSuccessStatusCode;
    }
}

public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public record GalleryImage(string Url)
{
    
}