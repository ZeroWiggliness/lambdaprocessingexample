using Xunit;
using Amazon.Lambda.TestUtilities;
using Amazon.Lambda.SQSEvents;
using Amazon.S3;
using Amazon.S3.Model;
using Castle.DynamicProxy.Internal;
using Moq;
using Newtonsoft.Json.Linq;
using System.Text;
using Amazon.Runtime.SharedInterfaces;
using Amazon.SQS;

namespace ProcessingFunction.Tests;

public class FunctionTest
{
    [Fact]
    public async Task TestSQSEventLambdaFunction()
    {
        var mockS3Client = new Mock<IAmazonS3>();
        var mockSqsClient = new Mock<IAmazonSQS>();


        var clients = "record_id,first_name,last_name client_reference,tax_free_allowance\r\n1,Frida,Müller,9e40659b-8b9f-4fc4-814b-5a7b5a23b64d,801\r\n2,Fritz,Maier,f4a0cc2c-d0b4-4f14-b202-c8a5e45e90e7,0";
        var portfolios = "record_id,accout_number,portfolio_reference,client_reference,agent_code\r\n1,12345678,90755e32-7438-4354-ad37-ad900e297844,9e40659b-8b9f-4fc4-814b-5a7b5a23b64d,EREZBT\r\n2,12345679,439695b4-508d-4562-8576-670e70024627,f4a0cc2c-d0b4-4f14-b202-c8a5e45e90e7,SFOJFK\r\n1,12345690,e0755e32-7438-4354-ad37-ad900e297844,9e40659b-8b9f-4fc4-814b-5a7b5a23b64d,EREZBT";
        var transactions = "record_id,accout_number,transaction_reference,amount,keyword\r\n1,12345678,14e56786,5000,DEPOSIT\r\n2,12345679,dfeb5fe13cd4,-789.56,TAX\r\n1,12345678,14e56786,5000,INTEREST";
        var accounts = "record_id,accout_number,cash_balance,currency,taxes_paid\r\n1,12345678,15000.00,EUR,0.00\r\n2,12345679,-56.00,EUR,789.56\r\n1,12345690,15000.00,EUR,5.00";

        mockS3Client
            .Setup(x => x.GetObjectAsync(It.IsAny<string>(), "clients_20230421.csv", It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new GetObjectResponse() { ResponseStream = new MemoryStream(Encoding.UTF8.GetBytes(clients)) }));

        
        mockS3Client
            .Setup(x => x.GetObjectAsync(It.IsAny<string>(), "portfolios_20230421.csv", It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new GetObjectResponse() { ResponseStream = new MemoryStream(Encoding.UTF8.GetBytes(portfolios)) }));

        
        mockS3Client
            .Setup(x => x.GetObjectAsync(It.IsAny<string>(), "transactions_20230421.csv", It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new GetObjectResponse() { ResponseStream = new MemoryStream(Encoding.UTF8.GetBytes(transactions)) }));

        
        mockS3Client
            .Setup(x => x.GetObjectAsync(It.IsAny<string>(), "accounts_20230421.csv", It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new GetObjectResponse() { ResponseStream = new MemoryStream(Encoding.UTF8.GetBytes(accounts)) }));

        
        var sqsEvent = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new SQSEvent.SQSMessage
                {
                    Body = "20230421"
                }
            }
        };

        var logger = new TestLambdaLogger();
        var context = new TestLambdaContext
        {
            Logger = logger
        };

        var function = new Function(mockS3Client.Object, mockSqsClient.Object);
        await function.FunctionHandler(sqsEvent, context);

        Assert.Contains("Processed message foobar", logger.Buffer.ToString());
    }
}