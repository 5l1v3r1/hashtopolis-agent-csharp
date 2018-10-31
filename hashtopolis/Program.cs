﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Web.Script.Serialization;
using System.Diagnostics;
namespace hashtopolis
{



    public struct Packets
    {
        public Dictionary<string, double> statusPackets;
        public List<string> crackedPackets;
    }

    public class testProp
    {
        public string action = "testConnection";
    }

    public class config_data
    {
        public string url { get; set; }
        public string uuid { get; set; }
        public string voucher { get; set; }
        public string token { get; set; }
    }


    class Program
    {
      
         
        public static string AppPath = AppDomain.CurrentDomain.BaseDirectory;

        //Read the settings file here
        
        private static string urlPath = Path.Combine(AppPath, "URL");
        private static string serverURL = "";

        static void initDirs()
        {

            string[] createDirs = new String[] { "files", "hashlists", "tasks", "hashcat" };

            foreach (string dir in createDirs)
            {
                string enumDir = Path.Combine(AppPath, dir);
                try
                {
                    if (!Directory.Exists(enumDir))
                    {
                        Console.WriteLine("Creating {0} directory", dir);
                        Directory.CreateDirectory(enumDir);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Data);
                    Console.WriteLine("Unable to create dir {0}", dir);
                    Console.WriteLine("Client now terminating");
                    Environment.Exit(0);
                }

            }

        }



        public static bool loadURL()
            {
                if (serverURL == "")
                {
                    if (File.Exists(urlPath))
                    {
                        serverURL = File.ReadAllText(urlPath);
                        if (serverURL == "")
                        {
                            File.Delete(urlPath);
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                return true;

            }


        public static void writeSettings(config_data config)
        {
            string jsonPath = Path.Combine(AppPath, "config.json");
            jsonClass jsonProcessor = new jsonClass();
            string saveSettings = jsonProcessor.toJson(config);

            File.WriteAllText(jsonPath, saveSettings);
        }

        public static void getSettings(config_data config)
        {
            JavaScriptSerializer jss = new JavaScriptSerializer();
            string jsonPath = Path.Combine(AppPath, "config.json");
            config.url = "";
            config.uuid = "";
            config.token = "";
            config.voucher = "";

            if (File.Exists(jsonPath))
            {
                string jsonContent = File.ReadAllText(jsonPath);
                Dictionary<string, dynamic> dict = jss.Deserialize<Dictionary<string, dynamic>>(jsonContent);
                if (dict.ContainsKey("url"))
                {
                    config.url = dict["url"];
                }
                if (dict.ContainsKey("voucher"))
                {
                    config.voucher = dict["voucher"];
                }
                if (dict.ContainsKey("uuid"))
                {
                    config.uuid = dict["uuid"];
                }
                if (dict.ContainsKey("token"))
                {
                    config.token = dict["token"];
                }

            }
            else
            {
                Console.WriteLine("Not found");
            }
        }

        public static Boolean initConnect(config_data config)
        {
            jsonClass testConnect = new jsonClass { debugFlag = DebugMode };
            testProp tProp = new testProp();
            string urlMsg = "Please enter server connect URL (https will be used unless specified):";
            Console.WriteLine(config.url);
            while (config.url == "")
            {
                Console.WriteLine(urlMsg);
                string url = Console.ReadLine();
                if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    url = "https://" + url;
                }
                Console.WriteLine("Testing connection to " + url);
                testConnect.connectURL = url;
                string jsonString = testConnect.toJson(tProp);
                string ret = testConnect.jsonSendOnce(jsonString);
                if (ret != null)
                {
                    if (testConnect.isJsonSuccess(ret))
                    {
                        //File.WriteAllText(urlPath, url);
                        Console.WriteLine("Connecction successful");
                        config.url = url;
                        writeSettings(config);
                    }
                }
                else
                {
                    urlMsg = "Test connect failed, please enter server connect URL:";
                }

            }

            Console.WriteLine("Connecting to server {0}",serverURL);
            return true;
        }

        public static Boolean DebugMode;

        static void Main(string[] args)
        {


                if (Console.LargestWindowWidth > 94 && Console.LargestWindowHeight > 24)
                {
                    Console.SetWindowSize(95, 25);
                }
                
     
            
            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;

            string tokenSwitch = "";

            foreach (string arg in args)
            {
                switch (arg.Substring(0, 2))
                {
               
                    case "-t":
                        tokenSwitch = arg.Substring(3);
                        break;
                    case "-u":
                        serverURL = arg.Substring(3);
                        break;
                    case "-d":
                        DebugMode = true;
                        break;
                }
            }


            string AppVersion = "0.52.6";
            Console.WriteLine("Client Version " + AppVersion);

            config_data config = new config_data();
           
            getSettings(config);
            Console.Write(config.url);
            initConnect(config);
            initDirs();

            registerClass client = new registerClass { connectURL = serverURL, debugFlag = DebugMode,tokenID = tokenSwitch};
            Boolean legacy = false; //Defaults to legacy STATUS codes
            client.setPath( AppPath);
            if (client.loginAgent())
            {
                Console.WriteLine("Logged in to server");
            }

            updateClass updater = new updateClass
            {
                htpVersion = AppVersion,
                parentPath = AppPath,
                arguments = args,
                connectURL = serverURL,
                debugFlag = DebugMode,
                tokenID = client.tokenID

            };
            updater.runUpdate();
            //Run code to self-update

            _7zClass zipper = new _7zClass
            {
                tokenID = client.tokenID,
                osID = client.osID,
                appPath = AppPath,
                connectURL = serverURL
            };

            if (!zipper.init7z())
            {
                Console.WriteLine("Failed to initialize 7zip, proceeding without. \n The client may not be able to extract compressed files");
            }

            taskClass tasks = new taskClass
            {
                sevenZip = zipper,
                debugFlag = DebugMode,
                client = client,
                legacy =  legacy
                
            };
            
            tasks.setOffset(); //Set offset for STATUS changes in hashcat 3.6.0
            tasks.setDirs(AppPath);
            
            int backDown = 5;
            while(true) //Keep waiting for 5 seconds and checking for tasks
            {
                Thread.Sleep(backDown * 1000);

                if (tasks.getTask())
                {
                    backDown = 5;
                }
                if (backDown <30)
                {
                    backDown++;
                }
            }

        }
    }
}
