using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using Transaction.Models;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Transaction.Services
{
    /// <summary>
    /// This class is for processing transaction
    /// </summary>
    public class TransactionService : ITransactionService
    {
        private HttpClient client;

        //This dictionary is used to as a in-memory cache to speed up the searching.
        private SortedDictionary<string, string> cardTypeDict;
       
        //Logger
        private readonly ILogger<TransactionService> _logger;
        private readonly IConfiguration _config;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="log"></param>
        /// <param name="config"></param>
        public TransactionService(ILogger<TransactionService> log, IConfiguration config)
        {
            //Init logger and config
            this._logger = log;
            this._config = config;

            //Init HtppClient
            if (client == null)
            {
                client = new HttpClient()
                {
                    BaseAddress = new Uri(this.GetLookUpAPI())
                };

                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }

            //Init cache dictionary
            cardTypeDict = new SortedDictionary<string, string>();   
        }

        /// <summary>
        /// Get Card Type
        /// </summary>
        /// <param name="binNumber"></param>
        /// <returns></returns>
        public async Task<string> GetCardTypeAsync(string binNumber)
        {
           
            //Validate BIN Number
            var validResult = this.ValidateBINNumber(binNumber);

            if(validResult.Contains("Error"))
            {
                return validResult;
            }
            else
            {
               // Verify wheather local dictionary contains BIN Number, if it does, get its type from dictionray first.
               // If dictionary does not contain BIN Number, then get it using web API.
                if (cardTypeDict.ContainsKey(binNumber) && cardTypeDict[binNumber] != string.Empty)
                {
                    return cardTypeDict[binNumber];
                }
                else
                {
                    try
                    {
                        //Get card type
                        string cardType = await GetCardTypeByBIN(binNumber);

                        //If the result is empty, re-try to call the API according to configuration
                        if (cardType == string.Empty)
                        {
                            int i = 0;
                            while (i < this.GetRepeatTimes())
                            {
                                //Wait 
                                Thread.Sleep(this.GetWaitTime());

                                //Get card type again
                                cardType = await GetCardTypeByBIN(binNumber);

                                //If it gets card type, then stop looping
                                if (cardType != string.Empty) break;

                                i++;
                            }
                        }

                        return cardType;
                    }
                    catch (Exception e)
                    {
                        this._logger.LogError($"{this.GetType()} GetCardTypeAsync: { e.InnerException.Message}");
                        return string.Format($"Error:{e.InnerException.Message}");
                    }


                }
            }
        }

        /// <summary>
        /// Validate BIN Number
        /// </summary>
        /// <param name="binNumber"></param>
        /// <returns></returns>
        private string ValidateBINNumber(string binNumber)
        {
            //Trim BIN Number to remove whitespaces at beginning, the end and the middle.
            binNumber = this.TrimString(binNumber);

            //Get BIN Number
            binNumber = binNumber.Substring(0, 6);

            //Try to parse BIN Number to int as to filter out invalid negative BIN Number for performance improving.
            int number = 0;
            bool success = Int32.TryParse(binNumber, out number);

            //Verify BIN Number
            // 1. BIN Number lenghth is 6 or more
            // 2. BIN number should be all digits
            // 3. BIN Number is positive
            if (binNumber.Length < 6)
            {
                //return string.Empty;
                return string.Format($"Error: {binNumber} length is invalid");
            }
            else if (!binNumber.All(Char.IsDigit))
            {
                //return string.Empty;
                return string.Format($"Error:{binNumber} contains non-digital characters");
            }
            else if (success && number <= 0)
            {
                return string.Format($"Error: {binNumber} is not a postive number and invalid");
            }
            else
            {
                return String.Format($"Success:{binNumber} is valid");
            }
        }

        /// <summary>
        /// Get cardtype by BIN Number
        /// </summary>
        /// <param name="bNumber"></param>
        /// <returns></returns>

        private async Task<string> GetCardTypeByBIN(string bNumber)
        {
            try
            {
                var response = client.GetAsync(bNumber).Result;

                //Verify whether the Web API successed
                if (response.IsSuccessStatusCode)
                {
                    //Get response then convert response JSON to CreditCard Object
                    var result = await response.Content.ReadAsStreamAsync();
                    var cardDetails = await JsonSerializer.DeserializeAsync<CreditCard>(result);

                    //If the cardtype is valid, then add into local dictionary
                    if (!String.IsNullOrEmpty(cardDetails.Scheme) && !String.IsNullOrEmpty(cardDetails.Type))
                        cardTypeDict.Add(bNumber, $"{cardDetails.Scheme} {cardDetails.Type}");

                    return cardDetails.ToString();
                }
                else
                {
                    // If web API call fails, then return empty string, then try again.
                    return string.Empty;
                }
            }
            catch (HttpRequestException e)
            {
                this._logger.LogError($"{this.GetType()} GetCardTypeByBIN: { e.InnerException.Message}");
                return string.Empty;
            }

        }

        /// <summary>
        ///This method is used to remove whitespace between digitals in BIN Numbers, the beginning, the end and the middle.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private string TrimString(string str)
        {
            if (str.Contains(' ') && str.Length > 0)
                return String.Concat(str.Where(c => !Char.IsWhiteSpace(c)));

            return str;
        }


        /// <summary>
        /// This function is to process input transaction files, then save the results into output file
        /// </summary>
        /// <param name="transFile"></param>
        /// <param name="outputFile"></param>
        /// <returns></returns>
        public async Task ProcessTranactionAsync(string inputFile, string outputFile)
        {
            if (!File.Exists(inputFile))
            {
                this._logger.LogError($"{inputFile} does not exist");
                //this._logger.LogDebug($"{this.GetType()} ProcessTranactionAsync: {inputFile} does not exist")
            }
            else
            {
                //Reader 
                var reader = new StreamReader(inputFile);

                // Writer for output 
                StreamWriter writer = new StreamWriter(outputFile);

                try
                {
                    //Get CSV file hader
                    string header = File.ReadLines(inputFile).First();

                    //Get BIN Number index as to get BIN number for each line
                    string[] headerFields = header.Split(',');
                    int bIndex = GetStringIndex(headerFields, "Bin Number");

                    if (bIndex == -1)
                    {
                        this._logger.LogError($"{inputFile} header:{header} does not contain BIN Number Column");
                    }
                    else
                    {
                       

                        //Loop to process transactions line by line
                        while (!reader.EndOfStream)
                        {
                            //Read each line 
                            var line = reader.ReadLine();
                            string[] lineDetails = line.Split(',');

                            //Verify whether the line contain BIN Number
                            if (lineDetails.Length > bIndex)
                            {
                                //Get BIN Number
                                string bNumber = lineDetails[bIndex];

                                //Get card type
                                string cardType = await this.GetCardTypeAsync(bNumber);

                                //Output result
                                string result = string.Empty;

                                //If BIN Number contains "BIN", it is the header
                                if (bNumber.Contains("BIN"))
                                {
                                    result = $"{line}, Card Type";
                                }
                                else
                                {
                                    //Save the result
                                    result = $"{line} {cardType}";
                                }

                                //If the result contains error, logging 
                                if(result.Contains("Error"))
                                {
                                    this._logger.LogError(result);
                                   
                                }
                                else
                                {
                                    //Only save the line with valid card type
                                    await SaveCardTypes(writer, result);

                                    //logging for user
                                    if (!result.Contains("BIN"))
                                        this._logger.LogInformation($"{line} is processed successfully");
                                }
                            }
                            else
                            {
                                this._logger.LogError($"{line} does not contain BIN Number column");
                            }

                        }

                        //close reader and writer
                        reader.Close();
                        writer.Close();
                    }
                }
                catch (Exception e)
                {
                    //close reader and writer
                    reader.Close();
                    writer.Close();

                    this._logger.LogError($"{this.GetType()} ProcessTranactionAsync: { e.InnerException.Message}");

                }
            }
        }

        /// <summary>
        /// Get index for sub in a string which ignores letters.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="sub"></param>
        /// <returns></returns>
        private int GetStringIndex(string[] str, string sub)
        {
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i].Trim().ToUpper() == sub.Trim().ToUpper())
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Save results
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private async Task SaveCardTypes(StreamWriter writer, string result)
        {
            try
            {
                await writer.WriteLineAsync(result);
            }
            catch (IOException e)
            {
                this._logger.LogError($"{this.GetType()} SaveCardTypes: { e.InnerException.Message}");
            }
        }

        /// <summary>
        /// Get Web API to search for card type by BIN Number
        /// </summary>
        /// <returns></returns>
        private string GetLookUpAPI()
        {
            var result = this._config.GetValue<string>("LookupWebAPI:URL");

            if(string.IsNullOrEmpty(result))
            {
                this._logger.LogError($"Transaction Lookup API is empty");
                return string.Empty;

            }
            else
            {
                //this._logger.LogInformation($"Transaction Lookup API is: {result}");
                return result;
            }         
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private int GetRepeatTimes()
        {
            var result = this._config.GetValue<int>("Transaction:RepeatTimes");
            
            if(result <= 0)
            {
                return 3;
            }

            return result;
        }

        /// <summary>
        /// Get the wait time if the service failes to get card type using LookUpApi, then try again.
        /// </summary>
        /// <returns>
        /// If there is no configuration in appsettings.json file, by default it returns 500 miliseconds.
        /// </returns>
        private int GetWaitTime()
        {
            var result = this._config.GetValue<int>("Transaction:WaitTime");

            if (result <= 0)
            {
                return 500;
            }

            return result;

        }
    }
}
