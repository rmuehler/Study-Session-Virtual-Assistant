using System;
using Microsoft.Azure.Cosmos.Table;
using System.Collections.Generic;

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
        public Dictionary<int, string> getAvailabilityMap(Availability ava)
        {
            var map = new Dictionary<int, string>
            {
                { 8, ava.AM0800 },
                { 9, ava.AM0900 },
                { 10, ava.AM1000 },
                { 11, ava.AM1100 },
                { 12, ava.PM1200 },
                { 13, ava.PM0100 },
                { 14, ava.PM0200 },
                { 15, ava.PM0300 },
                { 16, ava.PM0400 },
                { 17, ava.PM0500 },
                { 18, ava.PM0600 },
                { 19, ava.PM0700 },
                { 20, ava.PM0800 },
                { 21, ava.PM0900 }
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

        public Dictionary<int, string> getAvailabilityFromEmail(string searchEmail)
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
                return entity.getAvailabilityMap(entity);
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
    }
}