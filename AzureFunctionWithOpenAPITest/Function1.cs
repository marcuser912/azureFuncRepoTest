using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.OpenApi.Models;

namespace AzureFunctionWithOpenAPITest
{
    public static class Function1
    {
        private static ServiceBusClient _client;

        static Function1()
        {
            string serviceBusNamespace = Environment.GetEnvironmentVariable("SERVICE_BUS_NAMESPACE");
            _client = new ServiceBusClient(serviceBusNamespace, new DefaultAzureCredential());
        }

        [FunctionName("OpenApiFunc1")]
        [OpenApiOperation(operationId: "Run", tags: new[] { "Add message to Queue" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "subject", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **Subject** parameter")]
        [OpenApiRequestBody(bodyType: typeof(string), contentType: "plain/text", Description = "Enter message")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string subject = req.Query["subject"] == StringValues.Empty ?
                "none" :
                req.Query["subject"].ToString();

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            string queueName = Environment.GetEnvironmentVariable("QUEUE_NAME");

            var sender = _client.CreateSender(queueName);
            var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(requestBody))
            {
                Subject = subject
            };

            await sender.SendMessageAsync(message);

            return new OkObjectResult("Message delivered!");
        }
    }
}

