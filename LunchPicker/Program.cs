using System;
using System.Configuration;
using System.Threading.Tasks;

using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;
using System.Text;


/*
 * CREATED BY: Carlos Diaz - 09/28/2018
 * 
 * Queries Zomato API to get restaurant info around the area and semi-randomly pick one. 
 * 
 * */

/*
* MODIFIED BY: Carlos Diaz - 10/02/2018
* 
* Changed API call to Yelp!'s. Results should improve.
* 
* */

/*
* MODIFIED BY: Carlos Diaz - 10/04/2018
* 
* Put whole thing into a loop. Added simple result caching with file I/O.
* 
* */


namespace LunchPickerClient
{


    class Program
    {
        //Location hardcoded to district office
        private const double lat = 34.051868;
        private const double lon = -117.749901;

        private static YelpRootObject search_results;
        private static string choice;

        private static string systemPath = System.Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        private static string completePath = Path.Combine(systemPath, "LunchPickerCache.csv");

        private static bool debugMode;

        //Reusing the same client throughout application, prevents socket errors
        private static HttpClient client = new HttpClient();

        static void Main(string[] args)
        {
            //Setup client
            client.BaseAddress = new Uri("https://api.yelp.com/");
            var apiKey = ConfigurationManager.AppSettings["YelpKey"];
            client.DefaultRequestHeaders.Add("authorization", "Bearer " + apiKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            Console.WriteLine("Welcome to Lunch-O-Picker v 1.4.1 :D POWERED BY YELP!\u2122 (I have to write that in or I get sued, apparently)\n");

            //TODO: Call API to get geolocation from IP
            Console.WriteLine("Current City: Pomona, CA");

            //Main loop
            debugMode = false;
            do
            {
                Console.WriteLine("\nWhat would you like to try today? (ie. American, Chinese, Mexican, etc.) You may choose Random if you aren't sure :)" +
                    "\n(Type -o for options, -q to quit)");
                choice = Console.ReadLine();

                switch (choice)
                {
                    case "-o":
                        Console.WriteLine("Available options: -o Shows this menu\n" +
                                            "\t\t-d Toggle debug mode, displays full restaurant list used to choose from\n" +
                                            "\t\t-q Quits application\n");
                        break;
                    case "-d":
                        if (!debugMode)
                        {
                            debugMode = true;
                            Console.WriteLine("Debug mode enabled");
                        } else{
                            debugMode = false;
                            Console.WriteLine("Debug mode disabled");
                        }
                        break;
                    case "-q":
                        System.Environment.Exit(0);
                        break;
                    default:
                        ProcessChoice();
                        Console.WriteLine("DONE. TRY AGAIN? (y/n)");
                        choice = Console.ReadLine();
                        if(!choice.ToLower().Equals("y"))
                        {
                            System.Environment.Exit(0);
                        }
                        break;

                }
            } while (true);
        }

        static void ProcessChoice()
        {
            string uri;
            //Maybe I should do something like soundex
            choice = choice.ToLower().Replace(",", string.Empty);
            if (!choice.Equals("random"))
            {
                uri = string.Format("v3/businesses/search?term={0}&latitude={1}&longitude={2}&radius=9656&categories={3}&limit=10&sort_by=rating", choice,
                    lat, lon, "Restaurants");
            }
            else
            {
                uri = string.Format("v3/businesses/search?latitude={0}&longitude={1}&radius=9656&categories={2}&limit=30&sort_by=rating",
                    lat, lon, "Restaurants");
            }



            //Get list of restaurants from cache, or online if not found/is expired
            search_results = new YelpRootObject();
            if (File.Exists(completePath))
            {

                ReadFromCache();
            }
            if(search_results.businesses == null)
            {
                RunYelpRestaurantsAsync(uri).GetAwaiter().GetResult();

            }


            Random randomGen = new Random();
            string finalresult;
            if (search_results.businesses.Count > 0)
            {
                int restaurantIndex = randomGen.Next(0, search_results.businesses.Count - 1);
                finalresult = string.Format("\nYou should go to: {0}  Located at: {1}, {2}", search_results.businesses[restaurantIndex].name,
                    search_results.businesses[restaurantIndex].location.address1, search_results.businesses[restaurantIndex].location.city);
            }
            else
            {
                finalresult = "\nNo Nearby Locations Available :(";
            }
            Console.WriteLine(finalresult);
        }

        static void ReadFromCache()
        {
            List<YelpBusiness> businesses = new List<YelpBusiness>();
            string csv = File.ReadAllText(completePath);
            DateTime expire_date = DateTime.Parse(csv.Split('\n')[0]);
            //I could use last date modified but if someone keeps running it, well...
            if ((DateTime.Now - expire_date).TotalDays < 1)
            {
                //I wonder if a big enough file crashes, would stream be better then?
                foreach (string row in csv.Split('\n'))
                {
                    if (!string.IsNullOrEmpty(row))
                    {
                        if (row.Split(',')[0].Equals(choice))
                        {
                            businesses.Add(new YelpBusiness()
                            {
                                name = row.Split(',')[1],
                                location = new YelpLocation()
                                {
                                    address1 = row.Split(',')[2],
                                    city = row.Split(',')[3]
                                }
                            });
                        }
                    }
                }
                if(businesses.Count > 0)
                {
                    search_results.businesses = businesses;
                    if (debugMode)
                    {
                        Console.WriteLine("From the cache:");
                        ShowYelpRestaurants(search_results);
                    }
                }
            }
            //Reset cache
            else
            {
                File.WriteAllText(completePath, DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss") + '\n');
            }
        }

        static void WriteToCache()
        {
            if (!File.Exists(completePath))
            {
                File.WriteAllText(completePath, DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss") + '\n');
            }

            var records = new StringBuilder();
            foreach(var business in search_results.businesses)
            {
                //Commas in our values will definitely cause issues, this is only a workaround
                var newrow = string.Format("{0},{1},{2},{3}", choice, business.name.Replace(",", string.Empty), business.location.address1.Replace(",", string.Empty),
                    business.location.city.Replace(",", string.Empty));
                records.AppendLine(newrow);
            }
            File.AppendAllText(completePath, records.ToString());
        }


            //Async methods for retrieving & showing data from API and deseralizing JSON to objects
            private static async Task RunYelpRestaurantsAsync(string url)
        {
            try
            {
                search_results = await GetYelpRestaurantsAsync(url);
                if (debugMode)
                {
                    ShowYelpRestaurants(search_results);
                }
                WriteToCache();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        static async Task<YelpRootObject> GetYelpRestaurantsAsync(string path)
        {
            YelpRootObject businesslist = null;
            HttpResponseMessage response = await client.GetAsync(path);
            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                //Console.WriteLine(responseString);
                businesslist = JsonConvert.DeserializeObject<YelpRootObject>(responseString);
            }
            return businesslist;
        }

        static void ShowYelpRestaurants(YelpRootObject businesslist)
        {
            foreach (var business in businesslist.businesses)
            {
                Console.WriteLine($"Name: {business.name}\tAddress: " + $"{business.location.address1}\tCity: " +
                    $"{business.location.city}");
            }

        }



    }




}
