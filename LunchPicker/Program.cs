using System;
using System.Configuration;
using System.Linq;
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

namespace LunchPickerClient
{


    class Program
    {
        //Location hardcoded to district office
        private const double lat = 34.051868;
        private const double lon = -117.749901;
        private static RootLocation mainlocation;
        private static RootCuisines pomona_cuisines;
        private static RootRestaurant restaurants;
        private static String cuisineChoice;
        private static int cuisineIndex;
        private static int restaurantIndex;

        private static int debugMode;

        //Reusing the same client throughout application, prevents socket errors
        static HttpClient client = new HttpClient();

        static void Main(string[] args)
        {
            //Setup client
            client.BaseAddress = new Uri("https://developers.zomato.com/api/");
            var apiKey = ConfigurationManager.AppSettings["ZomatoKey"];
            client.DefaultRequestHeaders.Add("user-key", apiKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            Console.WriteLine("Welcome to Lunch-O-Picker v 1.3 :D\n");

            //Get current city info (can be used with another API to get geolocation based on IP)
            Console.WriteLine("Current City:");
            string uri = string.Format("v2.1/cities?lat={0}&lon={1}&count=1", lat, lon);
            RunLocationAsync(uri).GetAwaiter().GetResult();

            //Get cuisine types
            Console.WriteLine("Available Cuisines:");
            uri = string.Format("v2.1/cuisines?city_id={0}&lat={1}&lon={2}", mainlocation.location_suggestions[0].id, lat, lon);
            RunCuisinesAsync(uri).GetAwaiter().GetResult();

            Random randomGen = new Random();
            debugMode = 0;

            //Prompt for choice until we get one from the list or random
            do
            {
                Console.WriteLine("\nWhich would you like to try today? You may choose Random if you aren't sure :) \n(type debugmode to enable restaurant listing)");
                cuisineChoice = Console.ReadLine();

                if (cuisineChoice.ToLower().Equals("debugmode"))
                {
                    debugMode = 1;
                }


            } while (!cuisineChoice.ToLower().Equals("random") && !pomona_cuisines.cuisines.Any(i => i.cuisine.cuisine_name.ToLower().Equals(cuisineChoice.ToLower())));


            if (!cuisineChoice.ToLower().Equals("random"))
            {
                cuisineIndex = pomona_cuisines.cuisines.FindIndex(i => i.cuisine.cuisine_name.ToLower().Equals(cuisineChoice.ToLower()));

                if (debugMode == 1)
                {
                    Console.WriteLine("ID & Name:" + " " + pomona_cuisines.cuisines[cuisineIndex].cuisine.cuisine_id + " "
                    + pomona_cuisines.cuisines[cuisineIndex].cuisine.cuisine_name);
                }

            }
            else
            {
                cuisineIndex = randomGen.Next(0, pomona_cuisines.cuisines.Count - 1);
                cuisineChoice = pomona_cuisines.cuisines[cuisineIndex].cuisine.cuisine_name;

                if (debugMode == 1)
                {
                    Console.WriteLine("computer choice:" + " " + cuisineChoice);
                }
            }


            //Get list of restaurants
            uri = string.Format("v2.1/search?entity_id={0}&count=15&lat={1}&lon={2}&radius=12875&cuisines={3}&sort=rating&order=desc", mainlocation.location_suggestions[0].id, lat, lon,
                pomona_cuisines.cuisines[cuisineIndex].cuisine.cuisine_id);
            RunRestaurantsAsync(uri).GetAwaiter().GetResult();

            //Manually clean up the list of shitty values, have to use regular loop to modify collection while iterating
            string[] strarr = { "pomona", "ontario", "la verne", "montclair", "walnut", "chino", "san dimas", "chino hills", "claremont" };
            for (int i = restaurants.restaurants.Count - 1; i >= 0; i--)
            {
                if (!strarr.Contains(restaurants.restaurants[i].restaurant.location.city.ToLower()))
                {
                    restaurants.restaurants.RemoveAt(i);
                }
            }

            string finalresult;
            if (restaurants.restaurants.Count > 0)
            {
                restaurantIndex = randomGen.Next(0, restaurants.restaurants.Count - 1);
                finalresult = string.Format("\nYou should go to: {0}  Located at: {1}", restaurants.restaurants[restaurantIndex].restaurant.name,
                    restaurants.restaurants[restaurantIndex].restaurant.location.address);
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
        private static async Task RunLocationAsync(string url)
        {
            try
            {
                mainlocation = new RootLocation();
                mainlocation = await GetLocationAsync(url);
                ShowLocation(mainlocation);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        static async Task<RootLocation> GetLocationAsync(string path)
        {
            RootLocation locationlist = null;
            HttpResponseMessage response = await client.GetAsync(path);
            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                //Console.WriteLine(responseString);
                locationlist = JsonConvert.DeserializeObject<RootLocation>(responseString);
            }
            return locationlist;
        }

        static void ShowLocation(RootLocation locationlist)
        {
            foreach (var location in locationlist.location_suggestions)
            {
                Console.WriteLine(location.name);
            }

        }

        private static async Task RunCuisinesAsync(string url)
        {
            try
            {
                pomona_cuisines = new RootCuisines();
                pomona_cuisines = await GetCuisinesAsync(url);
                ShowCuisines(pomona_cuisines);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        static async Task<RootCuisines> GetCuisinesAsync(string path)
        {
            RootCuisines cuisinelist = null;
            HttpResponseMessage response = await client.GetAsync(path);
            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                //Console.WriteLine(responseString);
                cuisinelist = JsonConvert.DeserializeObject<RootCuisines>(responseString);
            }
            return cuisinelist;
        }

        static void ShowCuisines(RootCuisines cuisinelist)
        {
            int counter = 0;
            foreach (var cuisine in cuisinelist.cuisines)
            {
                if ((counter % 6) == 0 && counter != 0)
                {
                    Console.WriteLine();
                }
                //Console.Write($"ID: {cuisine.cuisine.cuisine_id}, Name: " + $"{cuisine.cuisine.cuisine_name}\t");
                if (counter != (cuisinelist.cuisines.Count - 1))
                {
                    Console.Write($"{cuisine.cuisine.cuisine_name}, ");
                }
                else
                {
                    Console.Write($"{cuisine.cuisine.cuisine_name}");
                }

                counter++;
            }
            Console.WriteLine();
        }

        private static async Task RunRestaurantsAsync(string url)
        {
            try
            {
                restaurants = new RootRestaurant();
                restaurants = await GetRestaurantsAsync(url);
                if (debugMode == 1)
                {
                    ShowRestaurants(restaurants);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        static async Task<RootRestaurant> GetRestaurantsAsync(string path)
        {
            RootRestaurant cuisinelist = null;
            HttpResponseMessage response = await client.GetAsync(path);
            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                cuisinelist = JsonConvert.DeserializeObject<RootRestaurant>(responseString);
            }
            return cuisinelist;
        }

        static void ShowRestaurants(RootRestaurant restaurantlist)
        {
            foreach (var restaurant in restaurantlist.restaurants)
            {
                Console.WriteLine($"Name: {restaurant.restaurant.name}\tAddress: " + $"{restaurant.restaurant.location.address}\tCity: " +
                    $"{restaurant.restaurant.location.city}\tRating: " + $"{restaurant.restaurant.user_rating.aggregate_rating}");
            }

        }


    }




}
