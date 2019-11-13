using System;
using Microsoft.Azure.Cosmos.Table;
using System.Collections.Generic;
using System.Linq;

namespace VirtualAssistant
{
    public class User : TableEntity
    {
        public User()
        {
            PartitionKey = "University of South Florida";
            Timestamp = DateTime.UtcNow;
        }
        public string Key => PartitionKey;
        public string EmailAdress => RowKey;
        public DateTimeOffset Time => Timestamp;
        public string Class { get; set; }
        public string Classification { get; set; }
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
    }

    public class Availability : TableEntity
    {
        public string Key => PartitionKey;
        public string Email => RowKey;
        public DateTimeOffset Time => Timestamp;
        public string AM0000 { get; set; }
        public string AM0100 { get; set; }
        public string AM0200 { get; set; }
        public string AM0300 { get; set; }
        public string AM0400 { get; set; }
        public string AM0500 { get; set; }
        public string AM0600 { get; set; }
        public string AM0700 { get; set; }
        public string AM0800 { get; set; }
        public string AM0900 { get; set; }
        public string AM1000 { get; set; }
        public string AM1100 { get; set; }
        public string PM0100 { get; set; }
        public string PM0200 { get; set; }
        public string PM0300 { get; set; }
        public string PM0400 { get; set; }
        public string PM0500 { get; set; }
        public string PM0600 { get; set; }
        public string PM0700 { get; set; }
        public string PM0800 { get; set; }
        public string PM0900 { get; set; }
        public string PM1000 { get; set; }
        public string PM1100 { get; set; }
        public string PM1200 { get; set; }
        public Dictionary<string, string> getAvailabilityMap()
        {
            var map = new Dictionary<string, string>
            {
                { "T8", AM0800 },
                { "T9", AM0900 },
                { "T10", AM1000 },
                { "T11", AM1100 },
                { "T12", PM1200 },
                { "T13", PM0100 },
                { "T14", PM0200 },
                { "T15", PM0300 },
                { "T16", PM0400 },
                { "T17", PM0500 },
                { "T18", PM0600 },
                { "T19", PM0700 },
                { "T20", PM0800 },
                { "T21", PM0900 }
            };
            return map;
        }
    }

    public class Database
    {
        const string tableName = "virtualassistantlmo3sv4";
        const string tableKey = "hzDFC2IKa6K3DEf+pG1rluOPDqmeFemzDP+GC7k4v3AbhtyMEDZL6hyAMydO5VhpOBaqLDkWt56LWQWHIxpAbA==";

        StorageCredentials creds;
        CloudStorageAccount account;
        CloudTableClient tableClient;

        public Database()
        {
            creds = new StorageCredentials(tableName, tableKey);
            account = new CloudStorageAccount(creds, true);
            tableClient = account.CreateCloudTableClient();
        }

        public void postNewUser(User newUserObject)
        {
            CloudTable table = tableClient.GetTableReference("UserAccounts");
            table.ExecuteAsync(TableOperation.InsertOrReplace(newUserObject));
        }

        public User getUserFromEmail(string email)
        {
            CloudTable table = tableClient.GetTableReference("UserAccounts");
            var tableResult = table.ExecuteAsync(TableOperation.Retrieve<User>("University of South Florida", email));
            return (User)tableResult.Result.Result;
        }

        public User getUserFromName(string searchName)
        {
            CloudTable table = tableClient.GetTableReference("UserAccounts");
            var condition = TableQuery.CombineFilters(
               TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "University of South Florida"),
               TableOperators.And,
               TableQuery.GenerateFilterCondition("Name", QueryComparisons.Equal, searchName)
               );

            var query = new TableQuery<User>().Where(condition);
            foreach (User entity in table.ExecuteQuery(query))
            {
                return entity;
            }

            return null;
        }

        public Dictionary<string, string> getAvailabilityFromEmail(string searchEmail)
        {
            CloudTable table = tableClient.GetTableReference("TutorAvailability");
            var condition = TableQuery.CombineFilters(
               TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "University of South Florida"),
               TableOperators.And,
               TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, searchEmail)
               );

            var query = new TableQuery<Availability>().Where(condition);
            foreach (Availability entity in table.ExecuteQuery(query))
            {
                return entity.getAvailabilityMap();
            }

            return null;
        }

        public List<User> findTutors_SubjectTime(string time, string subject)
        {
            CloudTable table = tableClient.GetTableReference("TutorAvailability");
            CloudTable table2 = tableClient.GetTableReference("UserData");

            var condition = TableQuery.CombineFilters(
               TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "University of South Florida"),
               TableOperators.And,
               TableQuery.CombineFilters(TableQuery.GenerateFilterCondition("Class", QueryComparisons.Equal, subject),
               TableOperators.And, TableQuery.GenerateFilterCondition($"{time}", QueryComparisons.Equal, "-")));

            var query = new TableQuery<Availability>().Where(condition);

            List<string> foundTutorEmails = new List<string>();
            List<User> foundTutors = new List<User>();

            foreach (Availability entity in table.ExecuteQuery(query))
            {
                var condition2 = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "University of South Florida"),
                TableOperators.And, TableQuery.GenerateFilterCondition("Email", QueryComparisons.Equal, entity.Email));
                var query2 = new TableQuery<User>().Where(condition);

                //dont worry about the complexity lol
                foreach (User entity2 in table2.ExecuteQuery(query2)) foundTutors.Add(entity2);
            }

            if (foundTutors.Count > 0)
            {
                return foundTutors;
            }
            return null;
        }


        //returns time string format: "YYYY-MM-DD,THH" (e.g. "2019-11-11,T16" where T16 = 16:00 hour)
        public string convertBotTimeToString(Microsoft.Bot.Builder.AI.Luis.DateTimeSpec[] time)
        {
            if (time.Length == 3)
            {
                return DateTime.Today.ToString() + "," + time[0].Expressions[0];
            }

            else
            {
                return time[0].Expressions[0];
            }
        }

        public string normalizeCourseName(string course)
        {
            if(new[] { "calc1", "calc 1", "calculus1", "calculus 1" }.Contains(course))
            {
                return "calculus1";
            }

            if (new[] { "calc2", "calc 2", "calculus2", "calculus 2" }.Contains(course))
            {
                return "calculus2";
            }

            if (new[] { "calc3", "calc 3", "calculus3", "calculus 3" }.Contains(course))
            {
                return "calculus3";
            }

            if (new[] { "physics1", "physics 1"}.Contains(course))
            {
                return "physics1";
            }

            if (new[] { "physics2", "physics 2"}.Contains(course))
            {
                return "physics2";
            }

            return course;
        }
    }
}