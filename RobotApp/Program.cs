﻿using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using Microsoft.Extensions.Configuration;
using System.IO;
using System;
using System.Collections.Generic;
using Dapper;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using RobotApp.Model;
using Newtonsoft.Json;
using OpenQA.Selenium.Support.UI;
using System.Threading;

namespace RobotApp
{
    class Program
    {
        private static IConfigurationRoot Configuration;
        private static FirefoxDriver driver = null;

        static void Main(string[] args)
        {
            //Get contests
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"Contests");
            DirectoryInfo d = new DirectoryInfo(@path);
            FileInfo[] Contests = d.GetFiles("*.json");
            

            foreach (FileInfo file in Contests)
            {
                var contest = GetContestDetail(file);

                if (DateTime.UtcNow <= contest.EndDate)
                {
                    //select contestants
                    foreach (var contestant in GetContestant())
                    {
                        var succeed = false;

                        try
                        {
                            FirefoxDriverService service = FirefoxDriverService.CreateDefaultService("C:\\GeckoDriver");
                            service.FirefoxBinaryPath = @"C:\Program Files (x86)\Mozilla Firefox\firefox.exe";
                            driver = new FirefoxDriver(service);

                            driver.Url = contest.Url;

                            foreach (var action in contest.Actions.ToList().OrderBy(o => o.Sequence))
                            {
                                IWebElement element = null;

                                if (action.Type == "Id")
                                    element = driver.FindElement(By.Id(action.Find));

                                if (action.Type == "Name")
                                    element = driver.FindElement(By.Name(action.Find));

                                if (action.Type == "Class")
                                    if (TryFindElement(By.ClassName(action.Find), out element))
                                    {
                                        bool visible = IsElementVisible(element);
                                        if (visible)
                                        {
                                            element = driver.FindElement(By.ClassName(action.Find));
                                        }
                                    }
                                

                                if (action.Action == "SendKey")
                                {
                                    element.Clear();
                                    if (action.Value.StartsWith("@"))
                                        element.SendKeys(GetFromContestant(contestant, action.Value));
                                    else
                                        element.SendKeys(action.Value);
                                }
                                else if (action.Action == "Click")
                                {
                                    Thread.Sleep(1000);
                                    if(element != null)
                                        element.Click();
                                }
                            }

                            //Get result
                           
                            if (contest.ContestResult.Type == "Id")
                            {
                                var result = driver.FindElement(By.Id(contest.ContestResult.Find)).Text;
                                succeed = result.Contains(contest.ContestResult.SearchFor);
                            }

                            driver.Close();
                        }
                        catch (Exception ex)
                        {
                            driver.Close();
                        }
                        
                        if (succeed)
                            Log(contest.Name, contestant.Id, "Participation enregistrée");
                        else
                            Log(contest.Name, contestant.Id, "Erreur lors de la soumission");

                        //WaitRandom();
                    }
                }
            }
        }

        public static bool TryFindElement(By by, out IWebElement element)
        {
            try
            {
                element = driver.FindElement(by);
            }
            catch (NoSuchElementException ex)
            {
                element = null;
                return false;
            }
            return true;
        }

        public static bool IsElementVisible(IWebElement element)
        {
            return element.Displayed && element.Enabled;
        }

        private static string GetFromContestant(ContestantModel contestant, string value)
        {
            switch (value)
            {
                case "@firstname":
                    return contestant.FirstName;
                case "@lastname":
                    return contestant.LastName;
                case "@address":
                    return contestant.Address;
                case "@city":
                    return contestant.City;
                case "@state":
                    return contestant.State;
                case "@zip":
                    return contestant.Zip;
                case "@email":
                    return contestant.Email;
                case "@phone":
                    return contestant.Phone;
                default:
                    return string.Empty;
            }


        }

        private static ContestModel GetContestDetail(FileInfo file)
        {
            using (StreamReader r = new StreamReader(file.FullName))
            {
                string json = r.ReadToEnd();
                return JsonConvert.DeserializeObject<ContestModel>(json);
            }
        }

        private static void Log(string siteName, int contestantId, string message)
        {
            string sql = "INSERT INTO Logs (CreatedDate, SiteName, ContestantId, Result) Values (@CreatedDate, @SiteName, @ContestantId, @Result);";

            using (var connection = new SqlConnection(GetConnectionString()))
            {
                connection.Open();

                var affectedRows = connection.Execute(sql, new { CreatedDate = DateTime.UtcNow, SiteName = siteName, ContestantId = contestantId, Result = message });
            }
        }

        private static void WaitRandom()
        {
            Random rnd = new Random();
            System.Threading.Thread.Sleep(rnd.Next(1, 2) * 60 * 1000);
        }

        private static IEnumerable<ContestantModel> GetContestant()
        {
            string sql = "SELECT * FROM Contestants;";

            using (var connection = new SqlConnection(GetConnectionString()))
            {
                connection.Open();

                var contestants = connection.Query<ContestantModel>(sql).ToList();

                return contestants;
            }
        }

        static string GetConnectionString()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            Configuration = builder.Build();

            string cn = Configuration["connectionString"];
            return cn;
        }
    }
}
