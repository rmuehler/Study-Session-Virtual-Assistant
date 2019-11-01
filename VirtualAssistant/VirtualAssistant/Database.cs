using System;
using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Serialization;

namespace VirtualAssistant
{
    class Database
    {
        const String StorageAccountName = "virtualassistantlmo3sv4";
        const string StorageAccountUriUsers = "https://virtualassistantlmo3sv4.table.core.windows.net/UserData?st=2019-10-31T21%3A29%3A36Z&se=2020-01-01T16%3A37%3A00Z&sp=raud&sv=2018-03-28&tn=userdata&sig=69KOp%2FBJcdTUJHjpNyI%2FuSlY5bUFFF9Hq9UN0PvPaPE%3D";
        const string StorageAccountQueryString = "?st=2019-10-31T21%3A29%3A36Z&se=2020-01-01T16%3A37%3A00Z&sp=raud&sv=2018-03-28&tn=userdata&sig=69KOp%2FBJcdTUJHjpNyI%2FuSlY5bUFFF9Hq9UN0PvPaPE%3D";


        int Main(string[] args)
        {
            if (args.Length == 1)
            {
                Console.WriteLine(getEmailInfo(args[0]));
                return 1;
            }
            return 0;
        }
        
        public string getEmailInfo(string searchEmail)
        {

            var clientUsers = new RestClient(StorageAccountUriUsers);

            var emailRequest = new RestRequest();
            emailRequest.AddParameter("email", searchEmail);

            IRestResponse response = clientUsers.Get(emailRequest);

            return response.Content;

        }





    }
}