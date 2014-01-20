using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tweetinvi;
using TweetinCore;
using oAuthConnection;
using TweetinCore.Interfaces;
using TwitterToken;
using System.Data.SqlServerCe;
using System.Net;
using System;
using System.Xml;
using System.IO;
using System.Diagnostics;
using System.Reflection;
//using Microsoft.NodeXL.ExcelTemplatePlugIns;

namespace UserSearch1
{
    class Program
    {
        static void Main(string[] args)
        {
            TwitterUserTree userTree = new TwitterUserTree();

            int NumOfCompaniesToSearch = 1;

            //string connectionStringUsers = string.Format("DataSource=\"{0}\"; Password='{1}'", "TwitterUsers.sdf", "Users");

            string connectionString = string.Format("DataSource=\"TwitterData_{0}.sdf\"", DateTime.Now.ToString("dd.MM.yyyy.HH.mm.ss.ffff"));
            //connectionString = "DataSource=TwitterData_18.01.2014.09.17.25.6807.sdf";

            string fileContents = string.Empty;
            List<String> companyNames;
            //List<IUser> twitterUsers;

            string ConsumerKey1 = System.Configuration.ConfigurationManager.AppSettings["ConsumerKey1"];
            string ConsumerSecret1 = System.Configuration.ConfigurationManager.AppSettings["ConsumerSecret1"];
            string AccessToken1 = System.Configuration.ConfigurationManager.AppSettings["AccessToken1"];
            string AccessTokenSecret1 = System.Configuration.ConfigurationManager.AppSettings["AccessTokenSecret1"];

            string ConsumerKey2 = System.Configuration.ConfigurationManager.AppSettings["ConsumerKey2"];
            string ConsumerSecret2 = System.Configuration.ConfigurationManager.AppSettings["ConsumerSecret2"];
            string AccessToken2 = System.Configuration.ConfigurationManager.AppSettings["AccessToken2"];
            string AccessTokenSecret2 = System.Configuration.ConfigurationManager.AppSettings["AccessTokenSecret2"];

            //IToken token = new Token(AccessToken1, AccessTokenSecret1, ConsumerKey1, ConsumerSecret1);
            IToken token = new Token(AccessToken2, AccessTokenSecret2, ConsumerKey2, ConsumerSecret2);
            ErrorCodes rcUsers = ErrorCodes.OK;

            string currentRunningDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            SqlCeConnectionStringBuilder sqlCeBuilder = new SqlCeConnectionStringBuilder(connectionString);
            rcUsers = userTree.CreateDB(Path.Combine(currentRunningDirectory, sqlCeBuilder.DataSource), connectionString);

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
                        IUser currFollower = dummyCurrFollower[0];

                        if (maxFollowersToGetAfterFirstLevel == 0)
                        {
                            numFollowersToGet = (int)currFollower.FollowersCount;
                        }

                        userTree.GetFollowerInformationRec(currFollower, token, depth - 1, numFollowersToGet, connectionString);
                    }
                }
                
                //userTree.GetTwitterTreeRec(depth, twitterUsers[0], token, connectionString);  
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