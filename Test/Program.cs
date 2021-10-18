using Emgu.CV;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;

namespace Test
{
    public static class Program
    {
        private static string publishedModelName = "Iteration8";
        public static void Main()
        {
            // You can obtain these values from the Keys and Endpoint page for your Custom Vision resource in the Azure Portal.
            string trainingEndpoint = "https://customvisionjc.cognitiveservices.azure.com/";
            string trainingKey = "e7c2563942b8482595e6c80a9e960892";
            // You can obtain these values from the Keys and Endpoint page for your Custom Vision Prediction resource in the Azure Portal.
            string predictionEndpoint = "https://customvisionjc-prediction.cognitiveservices.azure.com/";
            string predictionKey = "6e8dc9f09f5d4ffd9dafb9aaa33a8da2";


            CustomVisionTrainingClient trainingApi = AuthenticateTraining(trainingEndpoint, trainingKey);
            CustomVisionPredictionClient predictionApi = AuthenticatePrediction(predictionEndpoint, predictionKey);

            Project project = GetProject(trainingApi);
            int weighingScaleStatus = 1;

            //While an item is on the scale
            while (weighingScaleStatus == 1)
            {
                TestIteration(predictionApi, project);
                Console.Write("Another image? (1 or 0): ");
                weighingScaleStatus = Convert.ToInt32(Console.ReadLine());
            }

        }

        private static CustomVisionTrainingClient AuthenticateTraining(string endpoint, string trainingKey)
        {
            // Create the Api, passing in the training key
            CustomVisionTrainingClient trainingApi = new CustomVisionTrainingClient(new Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.ApiKeyServiceClientCredentials(trainingKey))
            {
                Endpoint = endpoint
            };
            return trainingApi;
        }
        private static CustomVisionPredictionClient AuthenticatePrediction(string endpoint, string predictionKey)
        {
            // Create a prediction endpoint, passing in the obtained prediction key
            CustomVisionPredictionClient predictionApi = new CustomVisionPredictionClient(new Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.ApiKeyServiceClientCredentials(predictionKey))
            {
                Endpoint = endpoint
            };
            return predictionApi;
        }

        private static Project GetProject(CustomVisionTrainingClient trainingApi)
        {
            // Create a new project
            Console.WriteLine("Getting project:");
            const string V = "4096a74a-379c-4565-8233-2231a9898555";
            return trainingApi.GetProject(Guid.Parse(V));
        }

        private static void TestIteration(CustomVisionPredictionClient predictionApi, Project project)
        {
            VideoCapture capture = new VideoCapture(); //create a camera captue
            Bitmap image = capture.QueryFrame().ToBitmap(); //take a picture

            //Saving photos into folder
            string filename = "file";
            image.Save(filename);
            string FileName = System.IO.Path.Combine(@"C:\Users\jiacheng\Downloads", DateTime.Now.ToString("yyy-MM-dd-hh-mm-ss"));
            image.Save(FileName + ".jpg");
            string imageFilePath = filename;

            //MemoryStream testImage = new MemoryStream(File.ReadAllBytes(Path.Combine(@"C:\Users\User\Downloads", "2021-10-13-02-47-14.jpg"))); ;
            MemoryStream testImage = new MemoryStream(File.ReadAllBytes(imageFilePath)); 
            // Make a prediction against the new project
            Console.WriteLine("Making a prediction.");
            var result = predictionApi.ClassifyImage(project.Id, publishedModelName, testImage);

            // Loop over each prediction and write out the results
            //foreach (var c in result.Predictions)
            //{
            //    Console.WriteLine($"\t{c.TagName}: {c.Probability:P1}");
            //}
            for (int i = 0; i < 3; i++)
            {
                Console.WriteLine($"\t{result.Predictions[i].TagName}: {result.Predictions[i].Probability:P1}");
            }
            {

            }
        }




    }
}