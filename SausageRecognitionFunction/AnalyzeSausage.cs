using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SausageRecognitionFunction
{
    public static class AnalyzeSausage
    {
        private static PredictionEndpoint predictionEndpoint = new PredictionEndpoint
        {
            ApiKey = Environment.GetEnvironmentVariable("CustomVisionPredictionKey")
        };

        [FunctionName("AnalyzeSausage")]
        public static async Task Run(
            [QueueTrigger("sausage-queue")]string recordId,
            [Blob("sausage-container/{queueTrigger}", FileAccess.Read)] Stream image,
            [CosmosDB(databaseName: "sausageData", collectionName: "sausages", ConnectionStringSetting = "CosmosDBConnection")] IAsyncCollector<Sausage> sausageData,
            TraceWriter log)
        {
            log.Info($"C# Queue trigger function processed: {recordId}");

            var result = await predictionEndpoint.PredictImageAsync(Guid.Parse(Environment.GetEnvironmentVariable("CustomVisionProjectId")), image);

            var predictions = result.Predictions
                .OrderByDescending(p => p.Probability)
                .Select(p => $"{p.TagName} : {p.Probability * 100}%");

            await sausageData.AddAsync(new Sausage
            {
                Id = recordId,
                Description = result.Predictions.OrderByDescending(p => p.Probability).Select(p => p.TagName).FirstOrDefault()
            });

            var message = string.Join(Environment.NewLine, predictions);

            log.Info(message);
        }
    }
}
