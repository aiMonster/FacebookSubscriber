using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
/*
 * EXAMPLE OF XML FILE
 <?xml version="1.0" encoding="utf-8"?>
 <Facebook>
   <link>https://www.facebook.com/990810177624947/</link>
   <link>https://www.facebook.com/a.vigirinskiy/</link>  
 </Facebook>

  *INSTRUCTION
  *  
  * BEFORE USING THIS BOT BE SURE THAT YOUR ACCOUNT 
  * HAS CONFIRMED PHONE NUMBER
  * 
*/

namespace FacebookSubscriber
{
    class Program
    {
        static void Main(string[] args)
        {
            //timeout seconds between requests
            const int MIN_TIMEOUT = 20;
            const int MAX_TIMEOUT = 60;

            const int MAX_ERROR_COUNT = 2; //Number of errors one by one

            int Skip = 0; //will be changed while processing
            const int TAKE = 4;

            //this account is blocked by Facebook
            const string LOGIN = "developervova2018@gmail.com";
            const string PASSWORD = "gazdagazda";

            const string PATH = @"C:\Users\Student\Desktop\facebookParsedSC.xml";            

            Log("Starting our application");
            SubscribingBot bot = null;
            try
            {
                bot = new SubscribingBot(LOGIN, PASSWORD);
            }
            catch(Exception ex)
            {
                Log("ERROR - " + ex.Message);
                return;
            }

            Random rnd = new Random();
            int counter = 1;
            int errorNumber = 0;
            int waitingSeconds = 0;

            XElement root = XElement.Load(PATH);
            var facebook = root.Elements("link").Skip(Skip).Take(TAKE);
            while (facebook.Count() != 0)
            {                 
                if (facebook.Count() == 0)
                {
                    Log("Pages to following ended");
                    return;
                }

                foreach (var link in facebook)
                {
                    string logMessage = "";
                    var id = link.Value.Split('/')[3];
                    logMessage += ("Processing id - " + id + ",\t counter - " + counter++ + "\n");
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    try
                    {
                        bot.Subscribe(id);
                        errorNumber = 0;
                    }
                    catch(Exception ex)
                    {
                        logMessage += (ex.Message) + "\n";
                        errorNumber++;
                    }
                    
                    watch.Stop();
                    var elapsedMs = watch.ElapsedMilliseconds;
                    logMessage += ("Finished in " + elapsedMs + " miliseconds\n");
                    waitingSeconds = rnd.Next(MIN_TIMEOUT, MAX_TIMEOUT);
                    logMessage += ("Waiting " + waitingSeconds + " seconds\n\n");
                    Log(logMessage);                    

                    if(errorNumber >= MAX_ERROR_COUNT)
                    {
                        Log("Max error number one by one was reached, seems like something wrong, application will be stopped");
                        return;
                    }

                    Thread.Sleep(1000 * waitingSeconds);
                }
                Log("Processed " + facebook.Count() + " elements\nRE-AUTHORIZING");
                bot.Authorize();
                facebook = root.Elements("link").Skip(Skip+=TAKE).Take(TAKE);
            }
            Log("FINISHED");
        }

        static void Log(string message)
        {            
            Console.WriteLine("\n" + DateTime.UtcNow + "\n" + message);
        }

        static void SortXml()
        {
            XElement root = XElement.Load(@"C:\Users\Student\Desktop\facebookParsed.xml");
            XElement facebook = root.Elements("Facebook").FirstOrDefault();            
            var orderedtabs = root.Elements("link")
                                  .OrderBy(xtab => (string)xtab.Value)
                                  .ToArray();
            root.RemoveAll();
            foreach (XElement tab in orderedtabs)
                root.Add(tab);
            root.Save(@"C:\Users\Student\Desktop\facebookParsedS.xml");
        }
    }
}
