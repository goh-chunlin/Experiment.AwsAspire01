namespace Experiment.AwsAspire01.ApiService.Services;

public interface IS3Service
{
    string GeneratePreSignedUrl(string bucketName, string objectKey, int expirationInMinutes = 3);
}