using System.Text;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SQS;
using System.Text.Json;
using System.Text.Json.Serialization;
using static System.Net.Mime.MediaTypeNames;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ProcessingFunction;

public class Function
{
    IAmazonSQS SQS { get; set; }
    IAmazonS3 S3Client { get; set; }

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

    public Function(IAmazonS3 s3Client, IAmazonSQS sqs)
    {
        S3Client = s3Client;
        SQS = sqs;
    }

    /// <summary>
    /// This method is called for every Lambda invocation. This method takes in an SQS event object and can be used 
    /// to respond to SQS messages.
    /// </summary>
    /// <param name="evnt"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task FunctionHandler(SQSEvent evnt, ILambdaContext context)
    {
        foreach (var message in evnt.Records)
        {
            await ProcessMessageAsync(message, context);
        }
    }

    private async Task ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
    {
        context.Logger.LogInformation($"Processed message {message.Body}");

        IList<Portfolio> portfolios = new List<Portfolio>();
        IList<Account> accounts = new List<Account>();
        IList<Transaction> transactions = new List<Transaction>();
        IList<Client> clients = new List<Client>();
        try
        {
            clients = await ParseFile<Client>($"clients_{message.Body}.csv",
                (IList<string> line) =>
                {
                    return new Client()
                    {
                        Id = Int32.Parse(line[0]),
                        FirstName = line[1],
                        LastName = line[2],
                        Reference = Guid.Parse(line[3]),
                        TaxFreeAllowance = Decimal.Parse(line[4])
                    };
                });

            transactions = await ParseFile<Transaction>($"transactions_{message.Body}.csv",
                (IList<string> line) =>
                {
                    return new Transaction()
                    {
                        Id = Int32.Parse(line[0]),
                        AccountNumber = Int32.Parse(line[1]),
                        TransactionReference = line[2],
                        Amount = Decimal.Parse(line[3]),
                        Keyword = line[4]
                    };
                });

            portfolios = await ParseFile<Portfolio>($"portfolios_{message.Body}.csv",
                (IList<string> line) =>
                {
                    return new Portfolio()
                    {
                        Id = Int32.Parse(line[0]),
                        AccountNumber = Int32.Parse(line[1]),
                        PortfolioReference = Guid.Parse(line[2]),
                        ClientReference = Guid.Parse(line[3]),
                        AgentCode = line[4]
                    };
                });

            accounts = await ParseFile<Account>($"accounts_{message.Body}.csv",
                (IList<string> line) =>
                {
                    return new Account()
                    {
                        Id = Int32.Parse(line[0]),
                        AccountNumber = Int32.Parse(line[1]),
                        CashBalance = Decimal.Parse(line[2]),
                        Currency = line[3],
                        TaxesPaid = Decimal.Parse(line[4])
                    };
                });
        }
        catch (Exception e)
        {
            context.Logger.LogInformation($"A processed files is not valid");
        }

        var processed = portfolios.Select<Portfolio, Message>(portfolio => 
        {
            try
            {
                return new PortfolioMessage()
                {
                    PortfolioReference = portfolio.PortfolioReference,
                    NumberOfTransactions = transactions.Count(x => x.AccountNumber == portfolio.AccountNumber),
                    CashBalance = accounts.First(x => x.AccountNumber == portfolio.AccountNumber).CashBalance,
                    SumOfDeposits = transactions.Where(x =>
                            x.AccountNumber == portfolio.AccountNumber && x.Keyword == "DEPOSIT")
                        .Sum(y => y.Amount)
                };
            }
            catch (Exception e)
            {
                return new ErrorMessage()
                {
                    ClientReference = null,
                    PortfolioReference = portfolio.PortfolioReference,
                    Message = "Error processing portfolio"
                };
            }
        }).ToList();

        processed.AddRange(clients
            .Select<Client, Message>(client =>
            {
                try
                {
                    return new ClientMessage()
                    {
                        ClientReference = client.Reference,
                        TaxesPaid = portfolios.Where(pf => pf.ClientReference == client.Reference).Join(accounts, acc => acc.AccountNumber, tr => tr.AccountNumber, (portfolio, account) => account).Sum(x => x.TaxesPaid),
                        TaxFreeAllowance = client.TaxFreeAllowance
                    };
                }
                catch (Exception e)
                {
                    return new ErrorMessage()
                    {
                        ClientReference = Guid.Empty,
                        PortfolioReference = null,
                        Message = "Error processing client"
                    };
                }
            })
            .ToList());

        foreach (var msg in processed)
        {
            var json = JsonSerializer.Serialize<object>(msg, new JsonSerializerOptions() { PropertyNamingPolicy = SnakeCaseNamingPolicy.Instance });

            await SQS.SendMessageAsync("https://sqs.eu-west-1.amazonaws.com/658546495467/output", $"{json}", CancellationToken.None);
        };
        

        await Task.CompletedTask;
    }

    private async Task<IList<T>> ParseFile<T>(string filename, Func<IList<string>, T> convert) where T : class
    {
        var objs = new List<T>();
        
        using (GetObjectResponse response = await S3Client.GetObjectAsync("filestorage-test-dv123", filename, CancellationToken.None))
        {
            using (StreamReader reader = new StreamReader(response.ResponseStream, Encoding.UTF8))
            {
                string contents = reader.ReadToEnd();
                var lines = contents.Split(new[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                objs = lines.Skip(1).Select(x => convert(x.Split(","))).ToList();
            }
        }

        return objs;
    }

}


public class SnakeCaseNamingPolicy : JsonNamingPolicy
{
    public static SnakeCaseNamingPolicy Instance { get; } = new SnakeCaseNamingPolicy();

    public override string ConvertName(string text)
    {
        var sb = new StringBuilder();
        sb.Append(char.ToLowerInvariant(text[0]));
        for (int i = 1; i < text.Length; ++i)
        {
            char c = text[i];
            if (char.IsUpper(c))
            {
                sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }
    
}