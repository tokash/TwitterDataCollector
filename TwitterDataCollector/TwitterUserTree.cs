using System;
using System.IO;
using System.Data.SqlServerCe;
using System.Collections.Generic;
using System.Data;
using Tweetinvi;
using TweetinCore.Interfaces;

namespace UserSearch1
{

    class TwitterUserTree
    {
        private const int MaxChars = 4000;

        public static string UsersTableSchema = @"(UserID bigint, Name nvarchar (30) not null, Follows nvarchar (4000), Description nvarchar (4000), 
                                        Screen_Name nvarchar (25), Followers_Count int, Friends_Count int, Favourites_Count int, Tweet_Count int, Location nvarchar (30), 
                                        Twitter_Name nvarchar (30), Time_Zone nvarchar (40), Created_At nvarchar (40), Time_Stamp nvarchar (40))";

        public static string TweetsTableSchema = @"(TweetID nvarchar (25) PRIMARY KEY, UserID nvarchar (25), Tweet nvarchar (4000), TimeOfTweet nvarchar (40))";
        public static string ReTweetsTableSchema = @"(TweetID nvarchar (25) PRIMARY KEY, UserID nvarchar (25), SourceTweetID nvarchar (25), TimeOfReTweet nvarchar (40))";
        
        SqlCeConnection mDBConnection = null;

        // TO DO: 
        // (1)-(a)Get companies user info from twitter->(b)get the Nth order tree of followers
        // (2)-Read user names from file
        // (3)-Create users tables (DB)
        // (4)-(a)Get Tweets from Users->(b)Get Tweets from Followers
        // (5)-(a)Get ReTweets from Tweets of Users->(b)Get ReTweets from Tweets of Followers
        // (6)-Check For New Followers->Do (4b) Again 
        
        //--------------------------------------------------------------------------------------

        public ErrorCodes CreateDB(string iFullPath, string iConnectionString)
        {

            ErrorCodes rc = ErrorCodes.OK;

            if (Directory.Exists(Path.GetDirectoryName(iFullPath)))
            {

                if (!File.Exists(Path.GetFileName(iFullPath)))
                {
                    try
                    {
                        SqlCeEngine db = new SqlCeEngine(iConnectionString);
                        db.CreateDatabase();
                        //CreateTable(Name, Schema, ConnectionString);
                    }
                    catch (Exception)
                    {
                        rc = ErrorCodes.ErrorOnCreating;
                    } 
                }
                else
                {
                    rc = ErrorCodes.AlreadyExists;
                }
            }
            else
            {
                throw new DirectoryNotFoundException();
            }

            return rc;

        }

        public ErrorCodes CreateTable(string Name, string Schema, string ConnectionString)
        {
            if (mDBConnection == null)
	        {
		        mDBConnection = new SqlCeConnection(ConnectionString); 
	        }

            ErrorCodes rc = ErrorCodes.OK;
 
            if (mDBConnection.State==ConnectionState.Closed)
            {
                mDBConnection.Open();
            }
 
            SqlCeCommand cmd;

            string sql = "create table " + Name + Schema;
 
            cmd = new SqlCeCommand(sql, mDBConnection);
 
            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                rc = ErrorCodes.ErrorOnCreating;
            }
            finally
            {
                mDBConnection.Close();
            }

            return rc;
        }

        //--------------------------------------------------------------------------------------
        
        public ErrorCodes ReadTxtFile(string FilePath, ref string  FileContent)
        {
            string Line;
            string fileContent;
            int Counter = 0;
            System.IO.StreamReader file;
            ErrorCodes rc = ErrorCodes.OK;
            bool isRead = false;

            // Read the file and display it line by line.

            if (File.Exists(FilePath))
            {
                file = new System.IO.StreamReader(FilePath);

                fileContent = file.ReadToEnd();
                FileContent = fileContent;
                isRead = true;

                file.Close();
            }
            else
            {
                rc = ErrorCodes.FileDoesntExist;
            }

            if (!isRead)
            {
                FileContent = string.Empty;
            }

            return rc;
            //while ((Line = file.ReadLine()) != null)
            //{
            //    Counter++;
            //    return Line;
            //}

            
        }

        public static List<string> RemoveStringFromName(List<string> Names)
        {
            string[] StringToRemove = { "INC", " INC", " GROUP", "CORP", " CORP", "FAC", " FAC", "NY", " NY", "CO", " CO", "MA", " MA", "PA", " PA", "&" };

            for (int i = 0; i < Names.Count; i++)
            {
                for (int j = 0; j < StringToRemove.Length; j++)
                {
                    Names[i] = Names[i].Replace(StringToRemove[j], "");
                }
            }

            return Names;
        }

        public List<String> ParseString(string FileContent)
        {
            int count = 1;
            string[] fileLines;

            char[] delimiterChars = { '\t' };
            fileLines = FileContent.Split('\n');
            
            List<String> companyNames = new List<string>();

            for (int i = 1; i < fileLines.Length; i++ )
            {
                string[] currLineSplit = fileLines[i].Split('\t');

                if (currLineSplit != null)
                {
                    if (currLineSplit.Length > 0)
                    {
                        companyNames.Add(currLineSplit[0]);
                    }
                }
            }

            return companyNames;
        }

        //--------------------------------------------------------------------------------------

        public static List<IUser> SearchUser(IToken token, string searchQuery)
        {
            int Results = 1;    //Number of Results to List
            int Result = 1;     //The N-Th Result from Results which Will Be Initiated in Data Base

                IUserSearchEngine searchEngine = new UserSearchEngine(token);
                List<IUser> searchResult = searchEngine.Search(searchQuery, Results, Result);

            return searchResult;
            //foreach (var user in searchResult)
            //{
            //    Console.Write(user.Screen_Name);
            //}
        }

        //--------------------------------------------------------------------------------------

        //public bool AdjustNumberOfCalls(int Bulk, int UsersCount, int Limit)
        //{
        //    int TotalCalls = UsersCount / Bulk;

        //    if (TotalCalls > Limit)
        //    {
        //        return true;
        //    }
        //    else
        //    {
        //        return false;
        //    }
        //}

        public ErrorCodes InitiateUserToDB(IUser user, string connectionstring)
        {
            ErrorCodes rc = ErrorCodes.OK;

            if (mDBConnection == null)
            {
                mDBConnection = new SqlCeConnection(connectionstring); 
            }

            string sqlCommand = "INSERT INTO "
                              + "Users(UserID, Name, Follows, Description, Screen_Name, Followers_Count, Friends_Count, Favourites_Count, Tweet_Count, Location, Twitter_Name, Time_Zone, Created_At)" +
                            "VALUES(@UserID, @Name, @Follows, @Description, @Screen_Name, @Followers_Count, @Friends_Count, @Favourites_Count, @Tweet_Count,  @Location, @Twitter_Name, @Time_Zone, @Created_At)";
            
            if (mDBConnection.State == ConnectionState.Closed)
            {
                mDBConnection.Open();
            }

            SqlCeCommand cmd;

            cmd = new SqlCeCommand(sqlCommand, mDBConnection);
            cmd.Parameters.AddWithValue("Follows", ""); 
            cmd.Parameters.AddWithValue("UserID", user.Id);
            cmd.Parameters.AddWithValue("Name", user.Name);
            cmd.Parameters.AddWithValue("Description", user.Description);
            cmd.Parameters.AddWithValue("Screen_Name", user.ScreenName);
            cmd.Parameters.AddWithValue("Followers_Count", user.FollowersCount);
            cmd.Parameters.AddWithValue("Friends_Count", user.FriendsCount);
            cmd.Parameters.AddWithValue("Favourites_Count", user.FavouritesCount);
            cmd.Parameters.AddWithValue("Tweet_Count", user.StatusesCount);
            cmd.Parameters.AddWithValue("Location", user.Location);
            cmd.Parameters.AddWithValue("Twitter_Name", user.Name);
            cmd.Parameters.AddWithValue("Time_Zone", (user.TimeZone != null) ? user.TimeZone : "");
            cmd.Parameters.AddWithValue("Created_At", user.CreatedAt.ToString());

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                rc = ErrorCodes.ErrorOnCreating;
            }
            finally
            {
                mDBConnection.Close();
            }

            return rc;
        }

        public ErrorCodes InitiateFollowerToDB(IUser user, string connectionstring, string Id)
        {
            ErrorCodes rc = ErrorCodes.OK;

            if (mDBConnection == null)
            {
                mDBConnection = new SqlCeConnection(connectionstring);
            }

            string sqlCommand = "INSERT INTO "
                              + "Followers(UserID, Name, Follows, Description, Screen_Name, Followers_Count, Friends_Count, Favourites_Count, Tweet_Count, Location, Twitter_Name, Created_At, Time_Stamp)" +
                            "VALUES(@UserID, @Name, @Follows, @Description, @Screen_Name, @Followers_Count, @Friends_Count, @Favourites_Count, @Tweet_Count, @Location, @Twitter_Name, @Created_At, @Time_Stamp)";

            if (mDBConnection.State == ConnectionState.Closed)
            {
                mDBConnection.Open();
            }

            SqlCeCommand cmd;

            cmd = new SqlCeCommand(sqlCommand, mDBConnection);

            if (Id == string.Empty)     //If user is a company initiate ""
            {
                cmd.Parameters.AddWithValue("Follows", "");
            }
            else
            {
                cmd.Parameters.AddWithValue("Follows", Id);
            }
            cmd.Parameters.AddWithValue("UserID", user.Id);
            cmd.Parameters.AddWithValue("Name", user.Name);
            cmd.Parameters.AddWithValue("Description", user.Description);
            cmd.Parameters.AddWithValue("Screen_Name", user.ScreenName);
            cmd.Parameters.AddWithValue("Followers_Count", user.FollowersCount);
            cmd.Parameters.AddWithValue("Friends_Count", user.FriendsCount);
            cmd.Parameters.AddWithValue("Favourites_Count", user.FavouritesCount);
            cmd.Parameters.AddWithValue("Tweet_Count", user.StatusesCount);
            cmd.Parameters.AddWithValue("Location", user.Location);
            cmd.Parameters.AddWithValue("Twitter_Name", user.Name);
            //cmd.Parameters.AddWithValue("Time_Zone", user.TimeZone);
            cmd.Parameters.AddWithValue("Created_At", user.CreatedAt.ToString());
            cmd.Parameters.AddWithValue("Time_Stamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                rc = ErrorCodes.ErrorOnCreating;
            }
            finally
            {
                mDBConnection.Close();
            }

            return rc;
        }

        public void InitiateFollowersListToDB(String iParentID, List<IUser> users, string connectionstring)
        {
            foreach (var user in users)
	        {
                InitiateFollowerToDB(user, connectionstring, iParentID);
	        }
        }

        public ErrorCodes InitiateTweetToDB(ITweet Tweet, string connectionstring, string UserId)
        {
            ErrorCodes rc = ErrorCodes.OK;

            if (mDBConnection == null)
            {
                mDBConnection = new SqlCeConnection(connectionstring);
            }

            string sqlCommand = "INSERT INTO Tweets(TweetId, UserId, Tweet, TimeOfTweet) VALUES(@TweetId, @UserId, @Tweet, @TimeOfTweet)";

            if (mDBConnection.State == ConnectionState.Closed)
            {
                mDBConnection.Open();
            }

            SqlCeCommand cmd;

            cmd = new SqlCeCommand(sqlCommand, mDBConnection);

            if (Tweet == null)     //If there are no Tweets initiate ""
            {
                rc = ErrorCodes.TweetsNotFound;
            }
            else
            {
                string[] TweetParsed = Tweet.ToString().Split(',');

                //if (TweetParsed[3].StartsWith(" 'RT "))
                //{
                //    cmd.Parameters.AddWithValue("ReTweet", "1");
                //    cmd.Parameters.AddWithValue("Tweet", string.Empty);
                //}
                //else
                //{
                //    cmd.Parameters.AddWithValue("ReTweet", string.Empty);
                //    cmd.Parameters.AddWithValue("Tweet", "1");
                //}
                cmd.Parameters.AddWithValue("TweetId", Tweet.IdStr);//TweetParsed[0]);
                cmd.Parameters.AddWithValue("UserId", UserId);
                cmd.Parameters.AddWithValue("Tweet", Tweet.Text);
                cmd.Parameters.AddWithValue("TimeOfTweet", Tweet.CreatedAt.ToString());
            }
            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                rc = ErrorCodes.ErrorOnCreating;
            }
            finally
            {
                mDBConnection.Close();
            }

            return rc;
        }

        public void InitiateTweetslistToDB(List<ITweet> Tweets, string connectionstring)
        {
            foreach (var Tweet in Tweets)
            {
                InitiateTweetToDB(Tweet, connectionstring, Tweet.IdStr);
            }
        }

        public ErrorCodes InitiateReTweetsToDB(ITweet Tweet, string connectionstring, string UserId)
        {
            ErrorCodes rc = ErrorCodes.OK;

            if (mDBConnection == null)
            {
                mDBConnection = new SqlCeConnection(connectionstring);
            }

            string sqlCommand = "INSERT INTO ReTweets(TweetId, UserId, SourceTweetID, TimeOfReTweet) VALUES(@TweetId, @UserId, @SourceTweetID, @TimeOfReTweet)";

            if (mDBConnection.State == ConnectionState.Closed)
            {
                mDBConnection.Open();
            }

            SqlCeCommand cmd;

            cmd = new SqlCeCommand(sqlCommand, mDBConnection);

            if (Tweet == null)     //If there are no Tweets initiate ""
            {
                rc = ErrorCodes.TweetsNotFound;
            }
            else
            {
                string[] TweetParsed = Tweet.ToString().Split(',');

                //if (TweetParsed[3].StartsWith(" 'RT "))
                //{
                //    cmd.Parameters.AddWithValue("ReTweet", "1");
                //    cmd.Parameters.AddWithValue("Tweet", string.Empty);
                //}
                //else
                //{
                //    cmd.Parameters.AddWithValue("ReTweet", string.Empty);
                //    cmd.Parameters.AddWithValue("Tweet", "1");
                //}
                cmd.Parameters.AddWithValue("TweetId", Tweet.IdStr);//TweetParsed[0]);
                cmd.Parameters.AddWithValue("UserId", Tweet.Creator.IdStr);
                cmd.Parameters.AddWithValue("SourceTweetID", Tweet.Retweeting.IdStr);
                cmd.Parameters.AddWithValue("TimeOfReTweet", Tweet.CreatedAt.ToString());
            }
            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                rc = ErrorCodes.ErrorOnCreating;
            }
            finally
            {
                mDBConnection.Close();
            }

            return rc;
        }

        //--------------------------------------------------------------------------------------
        
        public SqlCeDataReader ExecuteSQLQuery(string cmd, string ConnectionString)
        {
            if (mDBConnection == null)
            {
                mDBConnection = new SqlCeConnection(ConnectionString);
            }

            ErrorCodes rc = ErrorCodes.OK;

            if (mDBConnection.State == ConnectionState.Closed)
            {
                mDBConnection.Open();
            }

            SqlCeCommand cmd1 = new SqlCeCommand(cmd, mDBConnection);

            SqlCeDataReader Response = null;

            try
            {
               Response = cmd1.ExecuteReader();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                rc = ErrorCodes.ErrorOnCreating;
            }

            return Response;    
        }

        public static void API_Timer()
        {
            System.Threading.Thread.Sleep(900000); //Wait 15 minutes
        }

        //--------------------------------------------------------------------------------------
        
        //public void CompareNewToPrev_Followers(string userId, List<IUser> NewFollowers, List<int> OldFollowersIds, string connectionString)
        //{
        //    for (int i = 0; i < NewFollowers.Count; i++)
        //    {
        //        if (NewFollowers[i].Id == OldFollowersIds[i])
        //        {
        //            InitiateFollowerToDB(NewFollowers[i], connectionString, userId); //Add Follower with new time stamp 
        //            //Add_TimeStampToDB(NewFollowersIds[i], connectionString); //Add time stamp ONLY (with comma)
        //        }
        //    }
        //}

        //public void Add_TimeStampToDB(int ID, string connectionString)
        //{
        //    ErrorCodes rc = ErrorCodes.OK;

        //    if (mDBConnection == null)
        //    {
        //        mDBConnection = new SqlCeConnection(connectionString);
        //    }

        //    string sqlCommand = "INSERT INTO Followers(Time_Stamp) VALUES(@Time_Stamp)";

        //    if (mDBConnection.State == ConnectionState.Closed)
        //    {
        //        mDBConnection.Open();
        //    }

        //    SqlCeCommand cmd;

        //    cmd = new SqlCeCommand(sqlCommand, mDBConnection);

        //    cmd.Parameters.AddWithValue("Time_Stamp", ", ");
        //    cmd.Parameters.AddWithValue("Time_Stamp", ID.ToString());

        //    try
        //    {
        //        cmd.ExecuteNonQuery();
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.ToString());
        //        rc = ErrorCodes.ErrorOnCreating;
        //    }
        //    finally
        //    {
        //        mDBConnection.Close();
        //    }
        //}

        //public void CheckForNewFollowersByLIFO(IUser NewUser, IUser OldUser, IToken token, string connectionString)
        //{
        //    int NumOfNewFoll = (int)(NewUser.FollowersCount - OldUser.FollowersCount);

        //    List<long> NewFollowersIDList = NewUser.GetFollowerIds(token, false, 0, 10);

        //    List<IUser> NewFollowersList = Tweetinvi.UserUtils.Lookup(NewFollowersIDList, new List<string>(), token);   //Get followers general info by id

        //    if(NumOfNewFoll != 0)
        //    {
        //        for (int i = NumOfNewFoll; i != 0; i--)
        //        {
        //            InitiateFollowerToDB(NewFollowersList[i], connectionString, NewUser.IdStr);
        //        }
        //    }
        //}            

        public void CheckForNewTweets(IUser User, List<ITweet> NewTweets, string OldTweetId, string connectionString)
        {
            string NewTweetId = NewTweets[0].IdStr; 

            if (NewTweetId == OldTweetId) //Check if there are any new Tweets
            {
                int NumOfNewTweets = 1;

                foreach (var Tweet in NewTweets)    //If New Tweets exists, Count how many
                {
                    if (Tweet.IdStr != OldTweetId)
                    {
                        NumOfNewTweets++; //Counter of new Tweets
                    }
                    else
                    {
                        break;
                    }
                }

                for (int i = NumOfNewTweets; i != 0; i--)   //initiate New Tweets to Data Base
                {
                    InitiateTweetToDB(NewTweets[i], connectionString, User.IdStr);

                    GetRetweetsFromTweet(NewTweets[i], connectionString, User); //Initiate Retweets of New Tweets to Data Base
                }
            }
        }

        //--------------------------------------------------------------------------------------

        //public void GetFollowers(IUser User, IToken token, string connectionStringUsers, int iNumberOfFollowersToGet)
        //{
        //    List<long> followersIDs = new List<long>();

        //    if (iNumberOfFollowersToGet <= User.FollowersCount)
        //    {
        //        followersIDs = User.GetFollowerIds(token, false, 0, iNumberOfFollowersToGet);

        //        List<IUser> currFollowers = UserUtils.Lookup(followersIDs, new List<string>(), token);

        //    }
        //    else
        //    {
        //        throw new Exception("Error: Number of followers to get is greater than the user actual followers number");
        //    }

        //    //if (followersIDs.Count > 0)
        //    //{
        //    //    List<IUser> currFollowers = Tweetinvi.UserUtils.Lookup(followersIDs, new List<string>(), token);   //Get followers general info by id

        //    //    foreach (var follower in currFollowers)
        //    //    {
        //    //        InitiateFollowerToDB(follower, connectionStringUsers, User.IdStr);   //Initiate followers to data base
        //    //        //if (follower.FollowersCount <= 50)
        //    //        //{
        //    //        //GetTweets(follower, token, connectionStringUsers);
        //    //        //GetFollowers(follower, token, connectionStringUsers);
        //    //        //}
        //    //    }
        //    //}
        //}

        public List<IUser> GetFollowers(IUser iUser, IToken iToken, string iConnectionString, int iNumberOfFollowersToGet)
        {
            List<long> followersIDs = null;
            List<IUser> currFollowers = null;

            if (iNumberOfFollowersToGet <= iUser.FollowersCount)
            {
                //Get Users from twitter
                followersIDs = iUser.GetFollowerIds(iToken, iNumberOfFollowersToGet, false, 0);
                currFollowers = UserUtils.Lookup(followersIDs, new List<string>(), iToken);

                //Add users to DB
                InitiateFollowersListToDB( iUser.IdStr, currFollowers, iConnectionString);
            }
            else
            {
                throw new Exception("Error: Number of followers to get is greater than the user actual followers number");
            }

            return currFollowers;
        }

        List<IUser> GetFollowersList(IUser User, IToken token)
        {
            //List<long> followersIDs = User.GetFollowerIds(token, false, 0, 50);  //Create current user's followers ids list
            List<long> followersIDs = User.GetFollowerIds(token, false, 0);  //Create current user's followers ids list

            List<IUser> currFollowers = Tweetinvi.UserUtils.Lookup(followersIDs, new List<string>(), token);   //Get followers general info by id

            return currFollowers;
        }

        public void GetTweets(IUser User, IToken token, string connectionStringUsers, bool iGetRetweets)
        {
            List<ITweet> Tweets = null;

            try
            {
                Tweets = User.GetUserTimeline(true, token);       //Get tweets from user
            }
            catch (Exception ex)
            {
                if (!ex.ToString().Contains("401"))
                {
                    throw;
                }
            }

            SqlCeDataReader Response = ExecuteSQLQuery("SELECT MAX(TweetID) FROM TWEETS where UserID='" + User.IdStr + "'", connectionStringUsers); //Get Last Id From Data Base (after collecting data)
            string currUserMaxID = string.Empty;
            if (Response != null)
            {
                if (Response.Read())
                {
                    //currUserMaxID = Response["UserID"].ToString();
                    currUserMaxID = Response["Column1"].ToString();
                }
            }
            

            if (currUserMaxID != string.Empty)
            {
                CheckForNewTweets(User, Tweets, currUserMaxID, connectionStringUsers); //Check if there are any new tweets, If new Tweets exist, initiate them to Data Base (RetWeets Are Being Initiated Here Also)
            }        
            else
            {
                foreach (var Tweet in Tweets)
                {
                    InitiateTweetToDB(Tweet, connectionStringUsers, User.IdStr);   //Initiate Tweets to data base FOR FIRST TIME

                    if (iGetRetweets)
                    {
                        GetRetweetsFromTweet(Tweet, connectionStringUsers, User); //Get Retweets From Tweet And Initiate To Data Base FOR FIRST TIME 
                    }
                }
            }
        }

        List<ITweet> GetTweetsList(IUser User, IToken token, string connectionStringUsers)
        {
            List<ITweet> Tweets = null;

            try
            {
                Tweets = User.GetUserTimeline(true, token);       //Get tweets from user
            }
            catch (Exception)
            {
                throw;
            }

            return Tweets;
        }

        public void GetRetweetsFromTweet(ITweet Tweet, string connectionStringUsers, IUser User)
        {

            if (Tweet.RetweetCount > 0)
            {
                List<ITweet> ReTweets = Tweet.GetRetweets();    //Get ReTweets

                foreach (var ReTweet in ReTweets)
                {
                    InitiateReTweetsToDB(ReTweet, connectionStringUsers, User.IdStr);   //Initiate ReTweets to data base
                }
            }
            
        }

        //--------------------------------------------------------------------------------------

        public List<string> GetCurrFollowersFromDB(IUser User, string ConnectionString)
        {
            SqlCeDataReader response = ExecuteSQLQuery("SELECT * FROM FOLLOWERS WHERE FOLLOWS=" + User.IdStr, ConnectionString);

            return null; ;
        }

        //--------------------------------------------------------------------------------------
        
        public void GetTwitterTreeRec(int iDepth, IUser iUser, IToken iToken, string iConnectionString)
        {

        //This is a Recursive data collection function
        //The parameter depth specifies the order of depth of the network to retrieve

            if (iDepth == 1) //if depth = 1, collect only the initiated User's followers and tweets
            {
                GetTweets(iUser, iToken, iConnectionString, true);

                if ((int)iUser.FollowersCount <= 6000)
                {
                    GetFollowers(iUser, iToken, iConnectionString, (int)iUser.FollowersCount);
                }
                else
                {
                    GetFollowers(iUser, iToken, iConnectionString, 6000);
                }
            }	
            else //If depth > 1, dive to depth and go back while collecting from each stair (depth = 1)
            {
                GetTweets(iUser, iToken, iConnectionString, true);
                
                List<IUser> Followers = null;
                if ((int)iUser.FollowersCount <= 6000)
                {
                    Followers = GetFollowers(iUser, iToken, iConnectionString, (int)iUser.FollowersCount);
                }
                else
                {
                    Followers = GetFollowers(iUser, iToken, iConnectionString, 6000);
                }

	            foreach (var Follower in Followers)
	            {
                    GetTwitterTreeRec(iDepth - 1, Follower, iToken, iConnectionString);
	            }
            }
        }        

        public void GetFollowerInformationRec(IUser iUser, IToken iToken, int iDepth, int iMaxFollowersToGet, string iConnectionString)
        {
            int numFollowersToGet = 0;

            if (iMaxFollowersToGet < (int)iUser.FollowersCount)
            {
                numFollowersToGet = iMaxFollowersToGet;
            }
            else
            {
                numFollowersToGet = (int)iUser.FollowersCount;
            }

            if (numFollowersToGet > (int)iUser.FollowersCount)
            {
                numFollowersToGet = (int)iUser.FollowersCount;
            }

            if (iDepth == 1)
            {
                GetFollowers(iUser, iToken, iConnectionString, numFollowersToGet);
            }
            else
            {

                List<IUser> currentUserFollowers = GetFollowers(iUser, iToken, iConnectionString, numFollowersToGet);
                foreach (IUser follower in currentUserFollowers)
                {
                    GetFollowerInformationRec(follower, iToken, iDepth - 1, numFollowersToGet, iConnectionString);
                }
            }

        }
    }
}
