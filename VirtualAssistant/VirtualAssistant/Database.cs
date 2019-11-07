using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;

namespace VirtualAssistant
{


    public class User
    {
        public UserData[] value { get; set; }
    }

    public class UserData
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTime Timestamp { get; set; }
        public string Class { get; set; }
        public string Classification { get; set; }
        public string EmailAdress { get; set; }
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
    }


    public class Database
    {
        const string uriUsers = "https://virtualassistantlmo3sv4.table.core.windows.net/UserData?st=2019-10-31T21%3A29%3A36Z&se=2020-01-01T16%3A37%3A00Z&sp=raud&sv=2018-03-28&tn=userdata&sig=69KOp%2FBJcdTUJHjpNyI%2FuSlY5bUFFF9Hq9UN0PvPaPE%3D";

        //static HttpClient client = new HttpClient();
        HttpClient client = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate });


        public User getUserFromEmail(string searchEmail)
        {
            //client.BaseAddress = new Uri(StorageAccountUriUsers);
            string relativeUri = $"&$filter=EmailAdress%20eq%20%27{searchEmail}%27";

            //set up the HttpClient w/URI and response type
            client.BaseAddress = new Uri(uriUsers + relativeUri);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json;odata=nometadata");

            //make GET request
            HttpResponseMessage response = client.GetAsync("").Result;
            try
            {
                //throw exception if GET not successful
                response.EnsureSuccessStatusCode(); 

                //convert results to josn string
                String result = response.Content.ReadAsStringAsync().Result;

                //deserialize json string into User object
                var userData = JsonConvert.DeserializeObject<User>(result);

                //return the user object
                return userData;
            }
            catch
            {
                return null;
            }
        }



        //This doesnt work.
        public void postNewUser(User newUserObject)
        {
            //client.BaseAddress = new Uri(StorageAccountUriUsers);
            //string relativeUri = $"&$filter=EmailAdress%20eq%20%27{searchEmail}%27";

            //set up the HttpClient w/URI and response type
            client.BaseAddress = new Uri(uriUsers);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json;odata=nometadata");

            //make GET request
            HttpResponseMessage response = client.PostAsJsonAsync("", newUserObject).Result;
            try
            {
                //throw exception if POST not successful
                response.EnsureSuccessStatusCode();

                //convert results to json string
                String result = response.Content.ReadAsStringAsync().Result;
            }
            catch
            {
                throw;
            }
        }

        /*        static void Main(string[] args)
                {

                    var database = new Database();
                    User newUser = database.getUserFromEmail("RJackson@mail.usf.edu");
                    Console.WriteLine(newUser.value[0].Name);

                }
        */




    }
}