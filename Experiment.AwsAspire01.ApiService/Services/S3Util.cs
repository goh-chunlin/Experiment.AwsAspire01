using Amazon.S3;
using Amazon.S3.Model;

namespace Experiment.AwsAspire01.ApiService.Services;

public class S3Service(IAmazonS3 s3Client) : IS3Service
{
    public string GeneratePreSignedUrl(string bucketName, string objectKey, int expirationInMinutes = 3)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucketName,
            Key = objectKey,
            Verb = HttpVerb.PUT,
            ContentType = "application/octet-stream",
            Expires = DateTime.UtcNow.AddMinutes(expirationInMinutes)
        };

        // Generate the pre-signed URL
        var url = s3Client.GetPreSignedURL(request);
        return url;
    }
}