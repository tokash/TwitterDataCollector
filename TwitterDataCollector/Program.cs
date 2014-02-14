using System;
using System.Collections.Generic;
using TweetinCore.Interfaces;
using TwitterToken;
using System.IO;
using SqlCE2CSV;


namespace UserSearch1
{
    class Program
    {
        static void Main(string[] args)
        {
            TwitterUserTree userTree = new TwitterUserTree();

            int NumOfCompaniesToSearch = 1;
            string ConsumerKey1 = System.Configuration.ConfigurationManager.AppSettings["ConsumerKey1"];
            string ConsumerSecret1 = System.Configuration.ConfigurationManager.AppSettings["ConsumerSecret1"];
            string AccessToken1 = System.Configuration.ConfigurationManager.AppSettings["AccessToken1"];
            string AccessTokenSecret1 = System.Configuration.ConfigurationManager.AppSettings["AccessTokenSecret1"];

            string ConsumerKey2 = System.Configuration.ConfigurationManager.AppSettings["ConsumerKey2"];
            string ConsumerSecret2 = System.Configuration.ConfigurationManager.AppSettings["ConsumerSecret2"];
            string AccessToken2 = System.Configuration.ConfigurationManager.AppSettings["AccessToken2"];
            string AccessTokenSecret2 = System.Configuration.ConfigurationManager.AppSettings["AccessTokenSecret2"];

            string CSVFilesDirectory = System.Configuration.ConfigurationManager.AppSettings["CSVFilesDirectory"];

            //Generate DB name
            string dbName = string.Format("TwitterData_{0}", DateTime.Now.ToString("dd.MM.yyyy.HH.mm.ss.ffff"));
            string csvDirectory = Path.Combine(CSVFilesDirectory, dbName);
            dbName += ".sdf";

            //Get DB path from .config file
            string DBCreationPAth = System.Configuration.ConfigurationManager.AppSettings["DBCreationPAth"];

            //Combine DB name with DB path
            string DBFullPath = Path.Combine(DBCreationPAth, dbName);

            //Set Datasource
            string connectionString = string.Format("DataSource={0}", DBFullPath);

            string fileContents = string.Empty;
            List<String> companyNames;            

            //IToken token = new Token(AccessToken1, AccessTokenSecret1, ConsumerKey1, ConsumerSecret1);
            IToken token = new Token(AccessToken2, AccessTokenSecret2, ConsumerKey2, ConsumerSecret2);
            ErrorCodes rcUsers = ErrorCodes.OK;

            //string currentRunningDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            //SqlCeConnectionStringBuilder sqlCeBuilder = new SqlCeConnectionStringBuilder(connectionString);

            //Console.WriteLine(string.Format("Path to DB: {0} ", Path.Combine(currentRunningDirectory, sqlCeBuilder.DataSource)));
            Console.WriteLine(string.Format("Path to DB: {0} ", DBFullPath));
            //rcUsers = userTree.CreateDB(Path.Combine(currentRunningDirectory, sqlCeBuilder.DataSource), connectionString);
            rcUsers = userTree.CreateDB(DBFullPath, connectionString);

            ErrorCodes rcUsersTable = userTree.CreateTable("Users", TwitterUserTree.UsersTableSchema, connectionString);
            ErrorCodes rcFollowersTable = userTree.CreateTable("Followers", TwitterUserTree.UsersTableSchema, connectionString);
            ErrorCodes rcTweetsTable = userTree.CreateTable("Tweets", TwitterUserTree.TweetsTableSchema, connectionString);
            ErrorCodes rcReTweetsTable = userTree.CreateTable("ReTweets", TwitterUserTree.ReTweetsTableSchema, connectionString);

            rcUsers = userTree.ReadTxtFile(System.Configuration.ConfigurationManager.AppSettings["UsersListFilePath"], ref fileContents);
            companyNames = userTree.ParseString(fileContents);
            companyNames = TwitterUserTree.RemoveStringFromName(companyNames);

            int depth = int.Parse(System.Configuration.ConfigurationManager.AppSettings["SearchDepth"]);
            int maxFollowersToGetForFirstLevel = int.Parse(System.Configuration.ConfigurationManager.AppSettings["MaxFollowersToGetForFirstLevel"]);
            int maxFollowersToGetAfterFirstLevel = int.Parse(System.Configuration.ConfigurationManager.AppSettings["MaxFollowersToGetAfterFirstLevel"]);

            int numFollowersToGetForFirstLevel = maxFollowersToGetForFirstLevel;

            foreach (var company in companyNames)
            {
                List<IUser> twitterUsers = TwitterUserTree.SearchUser(token, company);//Search user
                userTree.InitiateUserToDB(twitterUsers[0], connectionString);

                if (maxFollowersToGetForFirstLevel == 0)
                {
                    numFollowersToGetForFirstLevel = (int)twitterUsers[0].FollowersCount;
                }

                if (numFollowersToGetForFirstLevel > (int)twitterUsers[0].FollowersCount)
                {
                    numFollowersToGetForFirstLevel = (int)twitterUsers[0].FollowersCount;
                }

                //List<IUser> companyFollowers = userTree.GetFollowers(twitterUsers[0], token, connectionString, numFollowersToGetForFirstLevel, false);
                userTree.GetFollowers(twitterUsers[0], token, connectionString, numFollowersToGetForFirstLevel, false);
                List<String> companyFollowers = userTree.GetFollowersIDs(twitterUsers[0].IdStr, connectionString);

                //userTree.GetTweets(twitterUsers[0], token, connectionString, true);
                userTree.GetTweetsBetweenDates(twitterUsers[0], token, connectionString, true, true, true, DateTime.Now.Subtract(new System.TimeSpan(7,0,0,0)), DateTime.Now);

                if (depth > 1)
                {
                    int numFollowersToGet = maxFollowersToGetAfterFirstLevel;
                    foreach (string follower in companyFollowers)
                    {
                        List<long> ids = new List<long>();
                        ids.Add(long.Parse(follower));
                        List<IUser> dummyCurrFollower = Tweetinvi.UserUtils.Lookup(ids, null, token);
                        if (dummyCurrFollower != null && dummyCurrFollower.Count > 0)
                        {
                            IUser currFollower = dummyCurrFollower[0];

                            if (maxFollowersToGetAfterFirstLevel == 0)
                            {
                                numFollowersToGet = (int)currFollower.FollowersCount;
                            }

                            userTree.GetFollowerInformationRec(currFollower, token, depth - 1, numFollowersToGet, connectionString); 
                        }
                    }
                }

                //Convert company data to csv files
                string query = string.Format("Select * from Users where UserID= \'{0}\'", twitterUsers[0].IdStr);
                string filename = string.Format("{0}.csv", company);
                userTree.ConvertDBDataToCSV(query, new string[]{"UserID",
                                                                "Name",
                                                                "Follows", 
                                                                "Description",
                                                                "Screen_Name",
                                                                "Followers_Count",
                                                                "Friends_Count",
                                                                "Favourites_Count",
                                                                "Total_Tweet_Count",
                                                                "Weekly_Tweet_Count",
                                                                "Weekly_Retweet_Count",
                                                                "Location",
                                                                "Twitter_Name",
                                                                "Time_Zone",
                                                                "Created_At",
                                                                "Time_Stamp"},
                                                                connectionString,
                                                                filename,
                                                                csvDirectory);

                query = string.Format("Select * from Followers where follows= \'{0}\'", twitterUsers[0].IdStr);
                userTree.ConvertDBDataToCSV(query , new string[]{"UserID",
                                                                "Name",
                                                                "Follows", 
                                                                "Description",
                                                                "Screen_Name",
                                                                "Followers_Count",
                                                                "Friends_Count",
                                                                "Favourites_Count",
                                                                "Total_Tweet_Count",
                                                                "Weekly_Tweet_Count",
                                                                "Weekly_Retweet_Count",
                                                                "Location",
                                                                "Twitter_Name",
                                                                "Time_Zone",
                                                                "Created_At",
                                                                "Time_Stamp"},
                                                                connectionString,
                                                                filename,
                                                                csvDirectory);
            }

            
            
        }

        
    }
}

//for (int CurrentCompany = 0; CurrentCompany < NumOfCompaniesToSearch; CurrentCompany++)
//{
//    twitterUsers = TwitterUserTree.SearchUser(token, companyNames[CurrentCompany]);      //Search user

//    userTree.InitiateUserToDB(twitterUsers[0], connectionStringUsers, twitterUsers[0].Id.ToString());    //Initiate user to data base

//    if (twitterUsers.Count > 0)
//    {
//        try
//        {
//            userTree.GetTweets(twitterUsers[0], token, connectionStringUsers);   //Get Tweets and ReTweets and initiate to data base
//        }
//        catch (WebException wex)
//        {
//            Console.WriteLine(wex.Message);
//        }

//        try
//        {
//            userTree.GetFollowers(twitterUsers[0], token, connectionStringUsers);       //Get Followers and initiate to data base
//            List<string> CurrFollowers = userTree.GetCurrFollowersFromDB(twitterUsers[0], connectionStringUsers);
//        }
//        catch (WebException wex)
//        {
//            Console.WriteLine(wex.Message); ;
//        }
//    }
//}