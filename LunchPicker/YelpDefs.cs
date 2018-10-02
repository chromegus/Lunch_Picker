using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LunchPickerClient
{
    public class YelpCategory
    {
        public string alias { get; set; }
        public string title { get; set; }
    }

    public class YelpCoordinates
    {
        public double latitude { get; set; }
        public double longitude { get; set; }
    }

    public class YelpLocation
    {
        public string address1 { get; set; }
        public string address2 { get; set; }
        public string address3 { get; set; }
        public string city { get; set; }
        public string zip_code { get; set; }
        public string country { get; set; }
        public string state { get; set; }
        public List<string> display_address { get; set; }
    }

    public class YelpBusiness
    {
        public string id { get; set; }
        public string alias { get; set; }
        public string name { get; set; }
        public string image_url { get; set; }
        public bool is_closed { get; set; }
        public string url { get; set; }
        public int review_count { get; set; }
        public List<YelpCategory> categories { get; set; }
        public double rating { get; set; }
        public YelpCoordinates coordinates { get; set; }
        public List<object> transactions { get; set; }
        public string price { get; set; }
        public YelpLocation location { get; set; }
        public string phone { get; set; }
        public string display_phone { get; set; }
        public double distance { get; set; }
    }

    public class YelpCenter
    {
        public double longitude { get; set; }
        public double latitude { get; set; }
    }

    public class YelpRegion
    {
        public YelpCenter center { get; set; }
    }

    public class YelpRootObject
    {
        public List<YelpBusiness> businesses { get; set; }
        public int total { get; set; }
        public YelpRegion region { get; set; }
    }
}
