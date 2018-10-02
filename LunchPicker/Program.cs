using System;
using System.Configuration;
using System.Threading.Tasks;

using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;


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


namespace LunchPickerClient
{


    class Program
    {
        //Location hardcoded to district office
        private const double lat = 34.051868;
        private const double lon = -117.749901;
        private static YelpRootObject search_results;
        private static String cuisineChoice;
        private static int restaurantIndex;

        private static int debugMode;

        //Reusing the same client throughout application, prevents socket errors
        static HttpClient client = new HttpClient();

        static void Main(string[] args)
        {
            //Setup client
            client.BaseAddress = new Uri("https://api.yelp.com/");
            var apiKey = ConfigurationManager.AppSettings["YelpKey"];
            client.DefaultRequestHeaders.Add("authorization", "Bearer " + apiKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            Console.WriteLine("Welcome to Lunch-O-Picker v 1.4 :D POWERED BY YELP!\u2122 (I have to write that in or I get sued, apparently)\n");

            //TODO: Call API to get geolocation from IP
            Console.WriteLine("Current City: Pomona, CA");

            Random randomGen = new Random();
            debugMode = 0;

            //Prompt for choice until we get one  or random
            do
            {
                Console.WriteLine("\nWhat would you like to try today? (ie. American, Chinese, Mexican, etc.) You may choose Random if you aren't sure :)" +
                    "\n(type debugmode to enable restaurant listing)");
                cuisineChoice = Console.ReadLine();

                if (cuisineChoice.ToLower().Equals("debugmode"))
                {
                    debugMode = 1;
                }


            } while (cuisineChoice.ToLower().Equals("debugmode") || cuisineChoice.Length < 1);

            string uri;
            if (!cuisineChoice.ToLower().Equals("random"))
            {
                uri = string.Format("v3/businesses/search?term={0}&latitude={1}&longitude={2}&radius=9656&categories={3}&limit=10&sort_by=rating", cuisineChoice,
                    lat, lon, "Restaurants");
            }
            else
            {
                uri = string.Format("v3/businesses/search?latitude={0}&longitude={1}&radius=9656&categories={2}&limit=30&sort_by=rating",
                    lat, lon, "Restaurants");
            }



            //Get list of restaurants
            RunYelpRestaurantsAsync(uri).GetAwaiter().GetResult();

            string finalresult;
            if (search_results.businesses.Count > 0)
            {
                restaurantIndex = randomGen.Next(0, search_results.businesses.Count - 1);
                finalresult = string.Format("\nYou should go to: {0}  Located at: {1}, {2}", search_results.businesses[restaurantIndex].name,
                    search_results.businesses[restaurantIndex].location.address1, search_results.businesses[restaurantIndex].location.city);
            }
            else
            {
                finalresult = "\nNo Nearby Locations Available :(";
            }



            Console.WriteLine(finalresult);

            Console.WriteLine("DONE. PRESS ANY KEY TO EXIT...");
            Console.ReadKey();

        }


        //Async methods for retrieving & showing data from API and deseralizing JSON to objects
        private static async Task RunYelpRestaurantsAsync(string url)
        {
            try
            {
                search_results = new YelpRootObject();
                search_results = await GetYelpRestaurantsAsync(url);
                if (debugMode == 1)
                {
                    ShowYelpRestaurants(search_results);
                }
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
                    $"{business.location.city}\tRating: " + $"{business.rating}");
            }

        }



    }




}
