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
        private static string publishedModelName = "Iteration9";

        // You can obtain these values from the Keys and Endpoint page for your Custom Vision resource in the Azure Portal.
        private static string trainingEndpoint = "https://customvisionjc.cognitiveservices.azure.com/";
        private static string trainingKey = "e7c2563942b8482595e6c80a9e960892";
        // You can obtain these values from the Keys and Endpoint page for your Custom Vision Prediction resource in the Azure Portal.
        private static string predictionEndpoint = "https://customvisionjc-prediction.cognitiveservices.azure.com/";
        private static string predictionKey = "6e8dc9f09f5d4ffd9dafb9aaa33a8da2";
        private static string predictionResourceId = "/subscriptions/732595f2-0961-4acb-b6eb-5b91f9219694/resourceGroups/ComputerVision/providers/Microsoft.CognitiveServices/accounts/CustomVisionJC";


        private static CustomVisionTrainingClient trainingApi = AuthenticateTraining(trainingEndpoint, trainingKey);
        private static CustomVisionPredictionClient predictionApi = AuthenticatePrediction(predictionEndpoint, predictionKey);

        private static Project project = GetProject(trainingApi);
        private static Tag newTag;

        private static int weighingScaleStatus = 1;
        private static string tag;

        private static List<string> newImages;

        private static Iteration iteration;
        public static void Main()
        {

            bool showMenu = true;
            while (showMenu)
            {
                showMenu = MainMenu();

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

        private static bool MainMenu()
        {
            Console.Clear();
            Console.WriteLine("Choose an option:");
            Console.WriteLine("1) Add Tag");
            Console.WriteLine("2) Add Images to Model");
            Console.WriteLine("3) Train Model");
            Console.WriteLine("4) Image Taking");
            Console.WriteLine("5) Prediction");
            Console.WriteLine("6) Publish Model");
            Console.Write("\r\nSelect an option: ");

            switch (Console.ReadLine())
            {
                case "1":
                    AddTags(trainingApi, project);
                    return true;
                case "2":
                    UploadImages(trainingApi, project);
                    return true;
                case "3":
                    TrainProject(trainingApi, project);
                    return true;
                case "4":
                    TakePhotoThreading();
                    return true;
                case "5":
                    //While an item is on the scale
                    while (weighingScaleStatus == 1)
                    {
                        TestIteration(predictionApi, project);
                        Console.Write("Another image? (1 or 0): ");
                        weighingScaleStatus = Convert.ToInt32(Console.ReadLine());
                    }
                    return true;
                case "6":
                    PublishIteration(trainingApi, project);
                    return true;
                default:
                    return true;
            }
        }

        private static void AddTags(CustomVisionTrainingClient trainingApi, Project project)
        {
            //List<string> newImages;
            Console.WriteLine("Tag Name:");
            tag = Console.ReadLine();
            newTag = trainingApi.CreateTag(project.Id, tag);

            string folderName = @"C:\Users\Admin\Downloads";
            string pathString = System.IO.Path.Combine(folderName, tag);
            System.IO.Directory.CreateDirectory(pathString);
        }

        private static void LoadImagesFromDisk()
        {
            // this loads the images to be uploaded from disk into memory
            //newImages = Directory.GetFiles(Path.Combine(@"C:\Users\Admin\Downloads", "tag")).ToList();
            //MemoryStream testImage = new MemoryStream(File.ReadAllBytes(Path.Combine("Images", "Test", "test_image.jpg")));
            newImages = Directory.GetFiles(Path.Combine(@"C:\Users\Admin\Downloads", tag)).ToList();
        }

        private static void UploadImages(CustomVisionTrainingClient trainingApi, Project project)
        {
            // Add some images to the tags
            Console.WriteLine("\tUploading images");
            LoadImagesFromDisk();

            // Or uploaded in a single batch 
            var imageFiles = newImages.Select(img => new ImageFileCreateEntry(Path.GetFileName(img), File.ReadAllBytes(img))).ToList();
            trainingApi.CreateImagesFromFiles(project.Id, new ImageFileCreateBatch(imageFiles, new List<Guid>() { newTag.Id }));

        }

        private static void TestIteration(CustomVisionPredictionClient predictionApi, Project project)
        {
            VideoCapture capture = new VideoCapture(); //create a camera captue
            Bitmap image = capture.QueryFrame().ToBitmap(); //take a picture

            //Saving photos into folder
            string filename = "file";
            image.Save(filename);
            string FileName = System.IO.Path.Combine(@"C:\Users\Admin\Downloads\JC", DateTime.Now.ToString("yyy-MM-dd-hh-mm-ss"));
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
            

        }
        public static void ChildThread1()
        {
            Console.WriteLine("Child 1 thread starts");
            VideoCapture capture1 = new VideoCapture(0); //create a camera capture
            Console.WriteLine("Child thread 1 created camera capture");
            Bitmap image1 = capture1.QueryFrame().ToBitmap(); //take a picture
            Console.WriteLine("Child thread 1 took a photo");
            //Saving photos into folder
            string FileName = System.IO.Path.Combine(@"C:\Users\Admin\Downloads", tag, DateTime.Now.ToString("yyy-MM-dd-hh-mm-ss"));
            image1.Save(FileName + "1" + ".jpg");
            Console.WriteLine("Child thread 1 saved an image in the folder");
        }
        public static void ChildThread2()
        {
            Console.WriteLine("Child 2 thread starts");
            VideoCapture capture2 = new VideoCapture(1); //create a camera capture
            Console.WriteLine("Child thread 2 created camera capture");
            Bitmap image2 = capture2.QueryFrame().ToBitmap(); //take a picture
            Console.WriteLine("Child thread 2 took a photo");

            string FileName = System.IO.Path.Combine(@"C:\Users\Admin\Downloads", tag, DateTime.Now.ToString("yyy-MM-dd-hh-mm-ss"));
            image2.Save(FileName + "2" + ".jpg");
            Console.WriteLine("Child thread 2 saved an image in the folder");
        }

        public static void ChildThread3()
        {
            Console.WriteLine("Child 3 thread starts");
            VideoCapture capture3 = new VideoCapture(2); //create a camera capture
            Console.WriteLine("Child thread 3 created camera capture");
            Bitmap image3 = capture3.QueryFrame().ToBitmap(); //take a picture
            Console.WriteLine("Child thread 3 took a photo");

            string FileName = System.IO.Path.Combine(@"C:\Users\Admin\Downloads", tag, DateTime.Now.ToString("yyy-MM-dd-hh-mm-ss"));
            image3.Save(FileName + "3" + ".jpg");
            Console.WriteLine("Child thread 3 saved an image in the folder");
        }

        public static void ChildThread4()
        {
            Console.WriteLine("Child 4 thread starts");
            VideoCapture capture4 = new VideoCapture(3); //create a camera capture
            Console.WriteLine("Child thread 4 created camera capture");
            Bitmap image4 = capture4.QueryFrame().ToBitmap(); //take a picture
            Console.WriteLine("Child thread 4 took a photo");

            string FileName = System.IO.Path.Combine(@"C:\Users\Admin\Downloads", tag, DateTime.Now.ToString("yyy-MM-dd-hh-mm-ss"));
            image4.Save(FileName + "4" + ".jpg");
            Console.WriteLine("Child thread 4 saved an image in the folder");
        }

        public static void TakePhotoThreading()
        {
            int start = 1;
            while (start == 1)
            {
                System.Threading.ThreadStart childref1 = new ThreadStart(ChildThread1);
                System.Threading.ThreadStart childref2 = new ThreadStart(ChildThread2);
                System.Threading.ThreadStart childref3 = new ThreadStart(ChildThread3);
                System.Threading.ThreadStart childref4 = new ThreadStart(ChildThread4);

                //Console.WriteLine("In Main: Creating the Child threads");
                Thread childThread1 = new Thread(childref1);
                childThread1.Start();

                Thread childThread2 = new Thread(childref2);
                childThread2.Start();

                Thread childThread3 = new Thread(childref3);
                childThread3.Start();

                Thread childThread4 = new Thread(childref4);
                childThread4.Start();

                Console.Write("Another image? (1 or 0): ");
                start = Convert.ToInt32(Console.ReadLine());
            }
        }

        private static void TrainProject(CustomVisionTrainingClient trainingApi, Project project)
        {
            // Now there are images with tags start training the project
            Console.WriteLine("\tTraining");
            iteration = trainingApi.TrainProject(project.Id);

            // The returned iteration will be in progress, and can be queried periodically to see when it has completed
            while (iteration.Status == "Training")
            {
                Console.WriteLine("Waiting 10 seconds for training to complete...");
                Thread.Sleep(10000);

                // Re-query the iteration to get it's updated status
                iteration = trainingApi.GetIteration(project.Id, iteration.Id);
            }
        }

        private static void PublishIteration(CustomVisionTrainingClient trainingApi, Project project)
        {
            trainingApi.PublishIteration(project.Id, iteration.Id, publishedModelName, predictionResourceId);
            Console.WriteLine("Done!\n");

            // Now there is a trained endpoint, it can be used to make a prediction
        }



    }
}