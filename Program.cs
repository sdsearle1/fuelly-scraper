using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ConsoleApplication1
{
    class BikeStatsPerYear
    {
        private int year;
        public int Year
        {
            get { return year; }
            set { year = value; }
        }

        private int total;
        public int Total
        {
            get { return total; }
            set { total = value; }
        }

        private float mpg;
        public float Mpg 
        {
            get { return mpg; }
            set { mpg = value; }
        }
    }
    
    class Bike
    {
        public Bike(string url)
        {
            MakeAndModel = url;
            Totals = new List<BikeStatsPerYear>();
        }

        private string url;
        public string MakeAndModel { 
            get
            {
                string bikeMakeAndModelUrl = url.Substring(url.IndexOf("cycle/")).Replace('_', ' ');
                //this fails if a bike make or model has a / in the name. As of now, no bike makes/models contain a / 
                string[] bikeMakeAndModelArray = bikeMakeAndModelUrl.Split('/');
                TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
                return bikeMakeAndModelArray[1].ToUpper() + " " + textInfo.ToTitleCase(bikeMakeAndModelArray[2]);
            }
            set{ url = value; }
        }

        private List<BikeStatsPerYear> totals;
        public List<BikeStatsPerYear> Totals
        {
            get { return totals; }
            set { totals = value; }
        }

        public float CalculateTotalMpg()
        {
            int totalBikes = 0;
            float totalMpg = 0;
            foreach(BikeStatsPerYear bike in Totals)
            {
                totalBikes += bike.Total;
                totalMpg += bike.Mpg * bike.Total;
            }
            return totalMpg / totalBikes;
        }

        public int GetTotalBikes()
        {
            int totalBikes = 0;
            foreach(var bikeStats in Totals)
            {
                totalBikes += bikeStats.Total;
            }
            return totalBikes;
        }

        public string GetYearsRange()
        {
            if (Totals.Count > 0)
            {
                return Totals.Select(item => item.Year).Min().ToString()
                    + " - "
                    + Totals.Select(item => item.Year).Max().ToString();
            }
            return "No years range results.";
        }

        public string GetMpgRange()
        {
            if(Totals.Count > 0)
            {
                return Totals.Select(item => item.Mpg).Min().ToString()
                    + " - "
                    + Totals.Select(item => item.Mpg).Max().ToString();
            }
            return "No MPG range results.";
        }
    }
    class Program
    {
        static void dbug(string msg)
        {
            Console.WriteLine(msg);
            Console.ReadLine();
        }

        private static List<string> scraperLoop(string url, string xPath, string attribute)
        {
            var web = new HtmlAgilityPack.HtmlWeb();
            var doc = web.Load(url);
            List<string> returnList = new List<string>();
            try
            {
                var scraped = doc.DocumentNode.SelectNodes(xPath);
                foreach (var result in scraped)
                {
                    foreach (var attr in result.Attributes)
                    {
                        if (attr.Name != attribute)
                        {
                            continue;
                        }
                        returnList.Add(attr.Value);
                        Console.WriteLine(attr.Value.ToString());
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return returnList;
        }

        static void Main(string[] args)
        {
            string url = "http://www.fuelly.com/motorcycle/";
            string xPath = "//a[@class='make-header-link']";
            string attribute = "href";
            List<string> List1 = scraperLoop(url, xPath, attribute);

            Console.WriteLine("Specify partial/full MAKE (e.g. Honda) of bike OR press enter to continue:");
            var inputBikeMake = Console.ReadLine().ToLower();

            List1 = inputBikeMake != "" ? List1.Where(stringToCheck => stringToCheck.ToLower().Contains(inputBikeMake)).ToList<string>() : List1;
            
            List<string> List2 = new List<string>();
            List<Bike> makeAndModelStats = new List<Bike>();
            foreach (string nextUrl in List1)
            {

                url = nextUrl;
                xPath = "//ul[@class='models-list']//a";
                List<string> urlList = scraperLoop(url, xPath, attribute); 
                foreach(string bikeUrl in urlList)
                {
                    List2.Add(bikeUrl);
                }
            }
            //at this point, List2 is a list of urls for each bike make (or the given make) and each bike model.

            List1.Clear();
            Console.WriteLine("Specify partial/full MODEL (e.g. CBR) of bike OR press enter to continue:");
            var inputBikeModel = Console.ReadLine().ToLower();

            List2 = inputBikeModel != "" ? List2.Where(stringToCheck => stringToCheck.ToLower().Contains(inputBikeModel)).ToList<string>() : List2;
            List<Bike> allBikes = new List<Bike>();

            //Process each bike page and compile a list of bikes with their stats
            foreach (string nextUrl in List2)
            {
                string xPathDiv = "//div[@class='model-year-item']";
                var web = new HtmlAgilityPack.HtmlWeb();
                var doc = web.Load(nextUrl);
                Bike bike = new Bike(nextUrl);
                try
                {
                    var scraped = doc.DocumentNode.SelectNodes(xPathDiv);
                    foreach(var bikeYear in scraped)
                    {
                        BikeStatsPerYear bikeStatsPerYear = new BikeStatsPerYear();
                        float thisOutMpg = 0;
                        int thisOutTotalBikes = 0;
                        int thisOutYear = 0;
                        string thisBikeYear = bikeYear.SelectSingleNode(".//li[@class='summary-year']/span").InnerText.ToString();
                        string thisBikeMpg = bikeYear.SelectSingleNode(".//li[@class='summary-avg']/span[@class='summary-avg-data']").InnerText.ToString();
                        string thisBikeTotal = Regex.Replace(bikeYear.SelectSingleNode(".//li[@class='summary-total']").InnerText.ToString(), "[^0-9.]", "");
                        if (float.TryParse(thisBikeMpg, out thisOutMpg) 
                                && Int32.TryParse(thisBikeTotal, out thisOutTotalBikes)
                                && Int32.TryParse(thisBikeYear, out thisOutYear)
                                && thisOutMpg > 0)
                        {
                            bikeStatsPerYear.Total = thisOutTotalBikes;
                            bikeStatsPerYear.Mpg = thisOutMpg;
                            bikeStatsPerYear.Year = thisOutYear;
                            bike.Totals.Add(bikeStatsPerYear);
                        }
                        else
                        {
                            Console.WriteLine("Count and/or rounded MPG did not parse to int.");
                        }
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message); 
                }
                allBikes.Add(bike);
            }

            //display bike information
            foreach(Bike bike in allBikes)
            {
                Console.WriteLine(bike.MakeAndModel);
                Console.WriteLine("Average MPG across all years: " + bike.CalculateTotalMpg().ToString() + " MPG, With a range of: " + bike.GetMpgRange());
                Console.WriteLine("Total bikes used in calculation: " + bike.GetTotalBikes().ToString());
                Console.WriteLine("Model years span from: " + bike.GetYearsRange());
                Console.ReadLine();
            }          
        }

    }
}
