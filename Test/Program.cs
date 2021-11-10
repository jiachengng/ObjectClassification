using Emgu.CV;
using MecWise.Blazor.Api.DataAccess;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Test
{
    public static class Program 
    {
        private static string publishedModelName = "Iteration1";

        // You can obtain these values from the Keys and Endpoint page for your Custom Vision resource in the Azure Portal.
        private static string trainingEndpoint = "https://imageclassificationjc.cognitiveservices.azure.com/";
        private static string trainingKey = "d9178858eeda4d63ab7399f375825cfd";
        // You can obtain these values from the Keys and Endpoint page for your Custom Vision Prediction resource in the Azure Portal.
        private static string predictionEndpoint = "https://imageclassificationjc-prediction.cognitiveservices.azure.com/";
        private static string predictionKey = "203e7e3581994ec8847f3584d609339a";
        private static string predictionResourceId = "/subscriptions/27042756-0171-4971-a639-aedb809ec487/resourceGroups/MecWiseRGCS/providers/Microsoft.CognitiveServices/accounts/ImageClassificationJC";


        private static CustomVisionTrainingClient trainingApi = AuthenticateTraining(trainingEndpoint, trainingKey);
        private static CustomVisionPredictionClient predictionApi = AuthenticatePrediction(predictionEndpoint, predictionKey);

        private static Project project = GetProject(trainingApi);
        private static Tag newTag;

        private static int weighingScaleStatus = 1;
        private static string tag;

        private static List<string> newImages;

        private static Iteration iteration;
        private static SqlConnection conn;

        public static void Main()
        {
            Console.WriteLine("Getting Connection ...");

            var datasource = @"DESKTOP-NTJ2LA6\SQLEXPRESS2016";//your server
            var database = "FnB_Pos_New"; //your database name
            var username = "sa"; //username of server to connect
            var password = "p@ssw0rd"; //password

            //your connection string 
            string connString = @"Data Source=" + datasource + ";Initial Catalog="+ database + ";Persist Security Info=True;User ID=" + username + ";Password=" + password;

            //create instanace of database connection
            conn = new SqlConnection(connString);


            try
            {
                Console.WriteLine("Openning Connection ...");

                //open connection
                conn.Open();

                Console.WriteLine("Connection successful!");
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }

            Console.Read();

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
            const string V = "b0ebc9a4-47de-4de8-86a8-06454404a42e";
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
            Console.WriteLine("9) Add Tag, Image taking, Add to Model");
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
                case "9":
                    AddTags(trainingApi, project);
                    TakePhotoThreading();
                    UploadImages(trainingApi, project);
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

            string x = "INSERT INTO dbo.MF_JC_GRP(COMP_CODE, PART_GRP, AC_TYPE, DEPT_CODE) VALUES('Test', '" + tag +
                "', 'T','T')";
            SqlCommand command = new SqlCommand(x, conn);
            command.ExecuteNonQuery();
            Console.WriteLine("query executed.");
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
            string predicted = "";
            //Only get the first item (Item with the highest probability)
            for (int i = 0; i < 1; i++)
            {
                //Display the item with the highest probability
                predicted = result.Predictions[i].TagName;
                Console.WriteLine(predicted);

                //Display the item AND the probability in %
                //Console.WriteLine($"\t{result.Predictions[i].TagName}: {result.Predictions[i].Probability:P1}");
            }

            //Console.WriteLine("Getting database tag");
            //string y = "select RUN_NO from MF_ITEM_TAG where TAG= '" + predicted + "'"; 

            //SqlCommand command = new SqlCommand(y, conn);
            //command.CommandTimeout = 0;

            //var dataSet = new DataSet();
            //var dataAdapter = new SqlDataAdapter { SelectCommand = command };
            //dataAdapter.Fill(dataSet);
            //string output = dataSet.Tables[0].Rows[0][0].ToString();
            //Console.WriteLine(output);



        }
        public static void ChildThread1()
        {
            Console.WriteLine("Child 1 thread starts");
            VideoCapture capture1 = new VideoCapture(0); //create a camera capture
            Console.WriteLine("Child thread 1 created camera capture");
            Bitmap image1 = capture1.QueryFrame().ToBitmap(); //take a picture
            Console.WriteLine("Child thread 1 took a photo");
            //Saving photos into folder
            string FileName = System.IO.Path.Combine(@"C:\Users\Admin\Downloads", tag, DateTime.Now.ToString("yyy-MM-dd-hh-mm-ssss"));
            FileName = FileName + "1" + ".jpg";
            image1.Save(FileName);
            Console.WriteLine("Child thread 1 saved an image in the folder");

            string x = "INSERT INTO dbo.MF_JC(COMP_CODE, PART_GRP, PART_NO, PART_DESC) VALUES('Test', '" + tag +
                "', '" + FileName + "','" + tag +"')";
            SqlCommand command = new SqlCommand(x, conn);
            command.CommandTimeout = 0;
            command.ExecuteNonQuery();
            Console.WriteLine("query executed.");
        }
        public static void ChildThread2()
        {
            Console.WriteLine("Child 2 thread starts");
            VideoCapture capture2 = new VideoCapture(1); //create a camera capture
            Console.WriteLine("Child thread 2 created camera capture");
            Bitmap image2 = capture2.QueryFrame().ToBitmap(); //take a picture
            Console.WriteLine("Child thread 2 took a photo");

            string FileName = System.IO.Path.Combine(@"C:\Users\Admin\Downloads", tag, DateTime.Now.ToString("yyy-MM-dd-hh-mm-ssss"));
            FileName = FileName + "2" + ".jpg";
            image2.Save(FileName);
            Console.WriteLine("Child thread 2 saved an image in the folder");

            string x = "INSERT INTO dbo.MF_JC(COMP_CODE, PART_GRP, PART_NO, PART_DESC) VALUES('Test', '" + tag +
                "', '" + FileName + "', '" + tag + "')";
            SqlCommand command = new SqlCommand(x, conn);
            command.CommandTimeout = 0;
            command.ExecuteNonQuery();
            Console.WriteLine("query executed.");
        }

        public static void ChildThread3()
        {
            Console.WriteLine("Child 3 thread starts");
            VideoCapture capture3 = new VideoCapture(2); //create a camera capture
            Console.WriteLine("Child thread 3 created camera capture");
            Bitmap image3 = capture3.QueryFrame().ToBitmap(); //take a picture
            Console.WriteLine("Child thread 3 took a photo");

            string FileName = System.IO.Path.Combine(@"C:\Users\Admin\Downloads", tag, DateTime.Now.ToString("yyy-MM-dd-hh-mm-ssss"));
            FileName = FileName + "3" + ".jpg";
            image3.Save(FileName);
            Console.WriteLine("Child thread 3 saved an image in the folder");

            string x = "INSERT INTO dbo.MF_JC(COMP_CODE, PART_GRP, PART_NO, PART_DESC) VALUES('Test', '" + tag +
                "', '" + FileName + "','" + tag + "')";
            SqlCommand command = new SqlCommand(x, conn);
            command.CommandTimeout = 0;
            command.ExecuteNonQuery();
            Console.WriteLine("query executed.");
        }

        public static void ChildThread4()
        {
            Console.WriteLine("Child 4 thread starts");
            VideoCapture capture4 = new VideoCapture(3); //create a camera capture
            Console.WriteLine("Child thread 4 created camera capture");
            Bitmap image4 = capture4.QueryFrame().ToBitmap(); //take a picture
            Console.WriteLine("Child thread 4 took a photo");

            string FileName = System.IO.Path.Combine(@"C:\Users\Admin\Downloads", tag, DateTime.Now.ToString("yyy-MM-dd-hh-mm-ssss"));
            FileName = FileName + "4" + ".jpg";
            image4.Save(FileName);
            Console.WriteLine("Child thread 4 saved an image in the folder");

            string x = "INSERT INTO dbo.MF_JC(COMP_CODE, PART_GRP, PART_NO, PART_DESC) VALUES('Test', '" + tag +
                "', '" + FileName + "','" + tag + "')";
            SqlCommand command = new SqlCommand(x, conn);
            command.CommandTimeout = 0;
            command.ExecuteNonQuery();
            Console.WriteLine("query executed.");
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
            string x = "newPublished";
            trainingApi.PublishIteration(project.Id, iteration.Id, x, predictionResourceId);
            Console.WriteLine("Done!\n");

            // Now there is a trained endpoint, it can be used to make a prediction
        }

        public static void AddTagNew(string compcode, string runno, int tag, string price, string doctype, string deptcode, string srlno, string lineno)
        {
            DBParameterCollection Params = new DBParameterCollection();
            Params.Add("COMP_CODE", compcode);
            Params.Add("RUN_NO", runno);
            Params.Add("TAG", tag);
            Params.Add("PRICE", price);
            Params.Add("DOC_TYPE", doctype);
            Params.Add("DEPT_CODE", deptcode);
            Params.Add("SRL_NO", srlno);
            Params.Add("LINE_NO", lineno);
            //DB.ExecProcedure()
        }




    }
}