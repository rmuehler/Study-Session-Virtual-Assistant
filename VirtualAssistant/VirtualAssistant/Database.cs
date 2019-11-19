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
        public string Classification { get; set; }
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
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

        public Dictionary<int, Dictionary<string, string>> getRegistrationUpdate(string tutorEmail)
        {
            Dictionary<int, Dictionary<string, string>> registrations = new Dictionary<int, Dictionary<string, string>>();
            CloudTable regTable = tableClient.GetTableReference("RegistrationData");
            int count = 1;
            var regCondition = TableQuery.GenerateFilterCondition("tutor", QueryComparisons.Equal, tutorEmail);
            var regQuery = new TableQuery<DynamicTableEntity>().Where(regCondition);
            
            foreach (DynamicTableEntity entity in regTable.ExecuteQuery(regQuery))
            {
                if (timeCompare(entity.Properties["time"].StringValue) < 0)
                {
                    IDictionary<string, EntityProperty> d = entity.WriteEntity(new OperationContext());
                    Dictionary<string, string> dic = new Dictionary<string, string>();

                    foreach (var pair in d)
                    {
                        if (pair.Key == "tutor") continue;
                        string val = pair.Value.StringValue;
                        if (pair.Key == "time") val = prettyDateTime(pair.Value.StringValue);
                        dic.Add(pair.Key, val);
                    }

                    registrations.Add(count++, dic);
                }
            }

            return registrations;
        }

        //return -1 if yyyy_mm_dd_Thh string format is later than current time
        public int timeCompare(string time)
        {

            return DateTime.Compare(DateTime.Now, new DateTime(Int32.Parse(new string(time.Substring(1).Remove(4))),
                                              Int32.Parse(new string(time.Substring(6).Remove(2))),
                                              Int32.Parse(new string(time.Substring(9).Remove(2))),
                                              Int32.Parse(new string(time.Substring(13))),
                                              0, 0
                                              ));
        }

        //convert yyyy_mm_dd_Thh format to a format you can display in chat to users
        public string prettyDateTime(string time)
        {
            return   Int32.Parse(new string(time.Substring(1).Remove(4))) + "/"
                   + Int32.Parse(new string(time.Substring(6).Remove(2))) + "/"
                   + Int32.Parse(new string(time.Substring(9).Remove(2))) + " at "
                   + Int32.Parse(new string(time.Substring(13))) + ":00 EST";
        }

        public ICollection<string> getTutorCourses(string email)
        {
            CloudTable table = tableClient.GetTableReference("TutorClasses");
            var condition = TableQuery.CombineFilters(
               TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "University of South Florida"),
               TableOperators.And,
               TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, email)
               );

            var query = new TableQuery<DynamicTableEntity>().Where(condition);
            foreach (DynamicTableEntity entity in table.ExecuteQuery(query))
            {
                IDictionary<string, EntityProperty> d = entity.WriteEntity(new OperationContext());
                return d.Keys;
            }
            return new List<string>();
        }

        public void setTutorCourses(User tutor, string courses)
        {
            List<string> myList = courses.Split(',').ToList();
            CloudTable table = tableClient.GetTableReference("TutorClasses");

            DynamicTableEntity entity = new DynamicTableEntity
            {
                PartitionKey = "University of South Florida",
                RowKey = tutor.EmailAdress
            };

            IDictionary<string, EntityProperty> d = new Dictionary<string, EntityProperty>() { };
            foreach (string s in getCurrentListOfCourses())
            {
                if (myList.Contains(s))
                {
                    d.Add(s, EntityProperty.GeneratePropertyForBool(true));
                }
                else d.Add(s, EntityProperty.GeneratePropertyForBool(false));

            }
            entity.Properties = d;
            table.ExecuteAsync(TableOperation.InsertOrReplace(entity));

        }

        public void setTutorAvailability_RESET(User tutor)
        {
            CloudTable table = tableClient.GetTableReference("TutorAvailability");
            DynamicTableEntity entity = new DynamicTableEntity
            {
                PartitionKey = "University of South Florida",
                RowKey = tutor.EmailAdress
            };

            IDictionary<string, EntityProperty> d = new Dictionary<string, EntityProperty>() { };
            foreach (string s in getCurrentListOfDateTimes()) d.Add(s, EntityProperty.GeneratePropertyForBool(true));

            entity.Properties = d;
            table.ExecuteAsync(TableOperation.InsertOrReplace(entity));
        }
        public ICollection<string> getCurrentListOfCourses()
        {
            CloudTable table = tableClient.GetTableReference("TutorClasses");
            var query = new TableQuery<DynamicTableEntity>();
            foreach (DynamicTableEntity entity in table.ExecuteQuery(query))
            {
                IDictionary<string, EntityProperty> d = entity.WriteEntity(new OperationContext());
                return d.Keys;
            }
            return new List<string>();
        }

        public ICollection<string> getCurrentListOfDateTimes()
        {
            CloudTable table = tableClient.GetTableReference("TutorAvailability");
            var query = new TableQuery<DynamicTableEntity>();
            foreach (DynamicTableEntity entity in table.ExecuteQuery(query))
            {
                IDictionary<string, EntityProperty> d = entity.WriteEntity(new OperationContext());
                return d.Keys;
            }
            return new List<string>();
        }

        public bool getAvailability(string searchEmail, string time)
        {
            CloudTable tutorAvailabilityTable = tableClient.GetTableReference("TutorAvailability");

            var condition = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "University of South Florida"),
                TableOperators.And,
                TableQuery.CombineFilters(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, searchEmail),
                TableOperators.And, TableQuery.GenerateFilterConditionForBool(time, QueryComparisons.Equal, true)));

            var query = new TableQuery<TableEntity>().Where(condition);
            foreach (TableEntity entity in tutorAvailabilityTable.ExecuteQuery(query))
            {
                return true;
            }

            return false;
        }

        public void reserveTutor(User tutor, User student, string time, string course)
        {
            CloudTable tutorAvailabilityTable = tableClient.GetTableReference("TutorAvailability");
            CloudTable registrationTable = tableClient.GetTableReference("RegistrationData");
            DynamicTableEntity entity = new DynamicTableEntity
            {
                PartitionKey = "University of South Florida",
                RowKey = DateTime.Now.Ticks.ToString(),
            };

            entity.Properties.Add("tutor", EntityProperty.GeneratePropertyForString(tutor.EmailAdress));
            entity.Properties.Add("student", EntityProperty.GeneratePropertyForString(student.Name));
            entity.Properties.Add("time", EntityProperty.GeneratePropertyForString(time));
            entity.Properties.Add("class", EntityProperty.GeneratePropertyForString(course));

            registrationTable.ExecuteAsync(TableOperation.InsertOrReplace(entity));
        }

        public List<User> findTutors_SubjectTime(string time, string subject)
        {
            List<User> availableTutors = new List<User>();
            List<User> allTutors = new List<User>();
            CloudTable tutorAvailabilityTable = tableClient.GetTableReference("TutorAvailability");
            CloudTable userAccountsTable = tableClient.GetTableReference("UserAccounts");
            CloudTable tutorClassesTable = tableClient.GetTableReference("TutorClasses");

            var condition1 = TableQuery.CombineFilters(
               TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "University of South Florida"),
               TableOperators.And,
               TableQuery.GenerateFilterCondition("Classification", QueryComparisons.Equal, "tutor"));
            var query1 = new TableQuery<User>().Where(condition1);

            foreach (User entity in userAccountsTable.ExecuteQuery(query1))
            {
                allTutors.Add(entity);
                var condition2 = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "University of South Florida"),
                TableOperators.And,
                TableQuery.CombineFilters(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, entity.EmailAdress),
                TableOperators.And, TableQuery.GenerateFilterConditionForBool(subject, QueryComparisons.Equal, true)));
                var query2 = new TableQuery<TableEntity>().Where(condition2);

                foreach (TableEntity entity2 in tutorClassesTable.ExecuteQuery(query2))
                {
                    var condition3 = TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "University of South Florida"),
                    TableOperators.And,
                    TableQuery.CombineFilters(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, entity.EmailAdress),
                    TableOperators.And, TableQuery.GenerateFilterConditionForBool(time, QueryComparisons.Equal, true)));
                    var query3 = new TableQuery<TableEntity>().Where(condition3);

                    foreach (TableEntity entity3 in tutorAvailabilityTable.ExecuteQuery(query3))
                        foreach (User u in allTutors) if (u.RowKey == entity3.RowKey) availableTutors.Add(u);
                }
            }

            if (availableTutors.Count > 0) return availableTutors;
            return new List<User>();
        }


        //returns time string format: "YYYY_MM_DD_THH" (e.g. "2019_11_11_T16" where T16 = 16:00 hour)
        public string convertBotTimeToString(Microsoft.Bot.Builder.AI.Luis.DateTimeSpec[] time)
        {
            if (time[0].Expressions[0].Length == 3)
            {
                return "D" + DateTime.Now.ToString("yyyy'-'MM'-'dd'T'HH").Remove(10).Replace('-', '_') + "_" + time[0].Expressions[0];
            }
            else
            {
                return "D" + time[0].Expressions[0].Replace('-', '_').Replace(',', '_').Insert(10, "_");
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