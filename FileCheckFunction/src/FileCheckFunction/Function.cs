using System.Text.RegularExpressions;
using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Amazon.SQS;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace FileCheckFunction;

public class Function
{
    IAmazonS3 S3Client { get; set; }
    private IAmazonSQS SQS { get; set; }

    /// <summary>
    /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
    /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
    /// region the Lambda function is executed in.
    /// </summary>
    public Function()
    {
        S3Client = new AmazonS3Client();
        SQS = new AmazonSQSClient();
    }

    /// <summary>
    /// Constructs an instance with a preconfigured S3 client. This can be used for testing outside of the Lambda environment.
    /// </summary>
    /// <param name="s3Client"></param>
    public Function(IAmazonS3 s3Client, IAmazonSQS sqs)
    {
        this.S3Client = s3Client;
        SQS = sqs;
    }

    /// <summary>
    /// This method is called for every Lambda invocation. This method takes in an S3 event object and can be used 
    /// to respond to S3 notifications.
    /// </summary>
    /// <param name="evnt"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task FunctionHandler(S3Event evnt, ILambdaContext context)
    {
        var eventRecords = evnt.Records ?? new List<S3Event.S3EventNotificationRecord>();
        foreach (var record in eventRecords)
        {
            var s3Event = record.S3;
            if (s3Event == null)
            {
                continue;
            }

            try
            {
                // regex and failing here would be a good check but we will do this quickly.
                var date = s3Event.Object.Key.Substring(s3Event.Object.Key.IndexOf("_") + 1, s3Event.Object.Key.IndexOf(".") - 1 - s3Event.Object.Key.IndexOf("_"));
                var names = new List<string>()
                {
                    $"clients_{date}.csv",
                    $"accounts_{date}.csv",
                    $"portfolios_{date}.csv",
                    $"transactions_{date}.csv"
                };
                var responses = new List<GetObjectMetadataResponse>();
                foreach (var name in names)
                {
                    context.Logger.LogInformation($"Checking for file {name}");
                    try
                    {
                        responses.Add(await S3Client.GetObjectMetadataAsync(s3Event.Bucket.Name, name));
                    }
                    catch
                    {
                        context.Logger.LogInformation($"File {name} doesnt exist");
                        break;
                    }
                }

                if (responses.Count == 4)
                {
                    context.Logger.LogInformation($"4 files with date {date} found, passing to processing");
                    await SQS.SendMessageAsync("https://sqs.eu-west-1.amazonaws.com/658546495467/processing", $"{date}",
                        CancellationToken.None);
                }
                else
                {
                    context.Logger.LogInformation($"Not all files with date {date} found, doing nothing");
                }
            }
            catch (Exception e)
            {
                context.Logger.LogError(e.Message);
                context.Logger.LogError(e.StackTrace);
                throw;
            }
        }
    }
}