using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Transaction.Services;
using System.IO;
using Serilog;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Transaction
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Print Instructions to User
            PrintInstructions();

            //Verify running paramaters
            if ( args.Length == 0 || args == null)
            {
                //Polish Error message.
                Log.Error("Input file paramater is missing");
            }
            else
            {
                string inputFile = args[0];

                //Verify input file path is relative or full path.
                if (!Path.IsPathRooted(inputFile))
                    inputFile = Path.Combine(Directory.GetCurrentDirectory(), inputFile);

                //verify input exists or not and whether it is CSV file
                if (!File.Exists(inputFile))
                {
                    //Log error
                    Log.Error($"Input file {inputFile} does not exist");
                }
                else if(Path.GetExtension(inputFile) !=".csv")
                {
                    Log.Error($"{inputFile} is not csv file");
                }
                else
                {
                   
                    //Setup configuration
                    var host = AppStartup();

                    //Create output file
                    string outputFile = CreateOutputFile();
                  
                    // verify output file
                    if (outputFile == string.Empty)
                    {
                        //Log error
                        Log.Error($"Output file does not exist ");
                    }
                    else
                    {
                        //Output file exists, then run the service 
                        Log.Information("Application starting running");

                        //Record starting time
                        DateTime sTime = DateTime.Now;

                        var service = ActivatorUtilities.CreateInstance<TransactionService>(host.Services);

                        // process the transactions and save output into output files
                        await service.ProcessTranactionAsync(inputFile, outputFile);

                        //Record end time
                        DateTime eTime = DateTime.Now;

                        Log.Information("Application is finished");

                        //Print total running time    
                        Log.Information($"Total running time is: {eTime - sTime}");

                        Log.Information($"The output is located: {outputFile}");
                        Log.Information($"The log file is located: {Directory.GetCurrentDirectory()}\\logs");
                                  
                    }
                }
            }
        }

        /// <summary>
        /// Create output file path
        /// </summary>
        /// <returns></returns>
        static string CreateOutputFile()
        { 
            string outputFile = Path.Combine(Directory.GetCurrentDirectory(), GetCurrentTime() + "_output.csv");
            return outputFile;
        }


        /// <summary>
        /// Setup configuration to read setting from appsetting.json file
        /// </summary>
        /// <param name="builder"></param>
        static void ConfigurationSetup(IConfigurationBuilder builder)
        {
            builder.SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddEnvironmentVariables();
        }

        /// <summary>
        /// Setup configuration for logger
        /// </summary>
        /// <returns></returns>
        static IHost AppStartup()
        {
            var builder = new ConfigurationBuilder();
            ConfigurationSetup(builder);

            //Define Serilog configuration
            Log.Logger = new LoggerConfiguration()
                            .ReadFrom.Configuration(builder.Build())
                            .Enrich.FromLogContext()
                            .WriteTo.Console()
                            .WriteTo.File($"logs/TransactionService_{GetCurrentTime()}.log")
                            .CreateLogger();

            //Init dependency injection container
            var host = Host.CreateDefaultBuilder()
                       .ConfigureServices((context, services) => {
                           services.AddTransient<ITransactionService, TransactionService>();
                       })
                       .UseSerilog()
                       .Build();

            return host;
        }

        /// <summary>
        /// Get current local timestamp
        /// </summary>
        /// <returns></returns>
        static string GetCurrentTime()
        {
            string timeStamp = new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds().ToString();
            return DateTime.Now.ToString("ddMMyyyy") + "_" + timeStamp ;
        }

        /// <summary>
        /// Print user instructions
        /// </summary>
        static void PrintInstructions()
        {
            string instructions =   "Welcome to transaction application which only runs on windows \n"
                                    + "Please follow the below instruction to run the application \n"
                                    + "1. Open Command Prompt by inputing cmd in search bar \n"
                                    + "2. Run the application with parameter like this:\n"
                                    + @"3. D:\Jennifer\TransactionProcessor\TransactionProcessor.exe  D:\Jennifer\data\transactions.csv " + "\n"
                                    + "4. the first path is the application full path, the second part is the data file's full path \n"
                                    + "5. Note: the csv should need to contain BIN Number column in the line, otherwise the application couldnot identify BIN Number\n";

            Console.WriteLine(instructions);
        }
    }
}
