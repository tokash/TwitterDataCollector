﻿using System;
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
            
            string connectionStringUsers = string.Format("DataSource=\"{0}\"", "TwitterUsers.sdf");
            
            string UsersTableSchema = @"(UserID bigint, Name nvarchar (30) not null, Follows nvarchar (4000), Description nvarchar (4000), 
                                        Screen_Name nvarchar (25), Followers_Count int, Friends_Count int, Location nvarchar (30), 
                                        Twitter_Name nvarchar (30), Time_Zone nvarchar (40), Created_At nvarchar (40), Time_Stamp nvarchar (40))";
            
            string TweetsTableSchema = @"(TweetID nvarchar (25) PRIMARY KEY, UserID nvarchar (25), Tweet nvarchar (4000))";
            string ReTweetsTableSchema = @"(TweetID nvarchar (25) PRIMARY KEY, UserID nvarchar (25), SourceTweetID nvarchar (25), TimeOfTweet nvarchar (40))";
            
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

            //ErrorCodes rcUsers = userTree.CreateDB("TwitterUsers.sdf", connectionStringUsers);
            //if (rcUsers.ToString() != "AlreadyExists")
            //{
            //    Console.WriteLine(rcUsers.ToString());
            //    //Console.ReadLine();
            //}
            //ErrorCodes rcUsersTable = userTree.CreateTable("Users", UsersTableSchema, connectionStringUsers);
            //ErrorCodes rcFollowersTable = userTree.CreateTable("Followers", UsersTableSchema, connectionStringUsers);
            //ErrorCodes rcTweetsTable = userTree.CreateTable("Tweets", TweetsTableSchema, connectionStringUsers);
            //ErrorCodes rcReTweetsTable = userTree.CreateTable("ReTweets", ReTweetsTableSchema, connectionStringUsers);

            rcUsers = userTree.ReadTxtFile(System.Configuration.ConfigurationManager.AppSettings["UsersListFilePath"], ref fileContents);
            companyNames = userTree.ParseString(fileContents);
            companyNames = TwitterUserTree.RemoveStringFromName(companyNames);
            
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

            int depth = 1;


            foreach (var company in companyNames)
            {
                List<IUser> twitterUsers = TwitterUserTree.SearchUser(token, company);      //Search user
                //userTree.InitiateUserToDB(twitterUsers[0], connectionStringUsers);
                userTree.GetTwitterTreeRec(depth, twitterUsers[0], token, connectionStringUsers);  
            }
            
            

            //Console.WriteLine("userTree.CreateDB returned: " + rcUsers);
            //Console.WriteLine("userTree.CreateDB returned: " + rcTweets);
        }
    }
}
