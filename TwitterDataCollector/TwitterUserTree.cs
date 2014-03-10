using System;
using System.IO;
using System.Data.SqlServerCe;
using System.Collections.Generic;
using System.Data;
using Tweetinvi;
using TweetinCore.Interfaces;
using SqlCE2CSV;
using System.Collections.Specialized;
using System.Configuration;
using TwitterUserTimeLine;
using System.Globalization;

namespace UserSearch1
{

    class TwitterUserTree
    {
        #region Members
        private const int MaxChars = 4000;

        public static string UsersTableSchema = @"(UserID nvarchar (25), Name nvarchar (30) not null, Follows nvarchar (4000), Description nvarchar (4000), 
                                        Screen_Name nvarchar (25), Followers_Count int, Followers_Added int, Followers_Removed int, PrevDBCompare nvarchar (4000), Friends_Count int, Favourites_Count int, Total_Tweet_Count int, Weekly_Tweet_Count int, Weekly_Retweet_Count int, Location nvarchar (30), 
                                        Twitter_Name nvarchar (30), Time_Zone nvarchar (40), Created_At nvarchar (40), Time_Stamp nvarchar (40))";

        public static string TweetsTableSchema = @"(TweetID nvarchar (25) PRIMARY KEY, UserID nvarchar (25), Tweet nvarchar (4000), Retweet_Count int, TimeOfTweet nvarchar (40))";
        public static string ReTweetsTableSchema = @"(TweetID nvarchar (25) PRIMARY KEY, UserID nvarchar (25), SourceTweetID nvarchar (25), TimeOfReTweet nvarchar (40))";

        SqlCeConnection mDBConnection = null;
        TwitterAPI _TwitterAPI;

        List<string> _Companies = new List<string>();
        public List<string> Companies
        {
            get { return _Companies; }
        }

        string _LastDBGenerated = string.Empty;
        public string LastDBGenerated
        {
            get
            {
                return _LastDBGenerated;
            }
            set
            {
                _LastDBGenerated = value;
            }
        }
        #endregion
        
        //--------------------------------------------------------------------------------------

        #region C'tor
        public TwitterUserTree()
        {
            ReadConfigurationSection("Companies", ref _Companies, false);

            _TwitterAPI = new TwitterAPI("lVVcDevyLOZcL2dqy4lL0g", "WZJxCCR2SY87SAVEJqBGBE7I5JOdGUYSlywTxMQdo");
        } 
        #endregion

        public ErrorCodes CreateDB(string iFullPath, string iConnectionString)
        {

            ErrorCodes rc = ErrorCodes.OK;

            if (Directory.Exists(Path.GetDirectoryName(iFullPath)))
            {

                if (!File.Exists(Path.GetFileName(iFullPath)))
                {
                    try
                    {
                        Console.WriteLine(string.Format("Connection string: {0}", iConnectionString));
                        SqlCeEngine db = new SqlCeEngine(iConnectionString);
                        db.CreateDatabase();
                        //CreateTable(Name, Schema, ConnectionString);
                    }
                    catch (Exception ex)
                    {
                        rc = ErrorCodes.ErrorOnCreating;
                        Console.WriteLine(ex.ToString());
                    } 
                }
                else
                {
                    rc = ErrorCodes.AlreadyExists;
                    Console.WriteLine("File already exists");
                }
            }
            else
            {
                Console.WriteLine("Directory not found");
                throw new DirectoryNotFoundException();
            }

            return rc;

        }

        public ErrorCodes CreateTable(string Name, string Schema, string ConnectionString)
        {
            
		    mDBConnection = new SqlCeConnection(ConnectionString); 

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
                              + "Users(UserID, Name, Follows, Description, Screen_Name, Followers_Count, Followers_Added, Followers_Removed, PrevDBCompare, Friends_Count, Favourites_Count, Total_Tweet_Count, Weekly_Tweet_Count, Weekly_Retweet_Count, Location, Twitter_Name, Time_Zone, Created_At, Time_Stamp)" +
                            "VALUES(@UserID, @Name, @Follows, @Description, @Screen_Name, @Followers_Count, @Followers_Added, @Followers_Removed, @PrevDBCompare, @Friends_Count, @Favourites_Count, @Total_Tweet_Count, @Weekly_Tweet_Count, @Weekly_Retweet_Count, @Location, @Twitter_Name, @Time_Zone, @Created_At, @Time_Stamp)";
            
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
            cmd.Parameters.AddWithValue("Followers_Added", -1);
            cmd.Parameters.AddWithValue("Followers_Removed", -1);
            cmd.Parameters.AddWithValue("PrevDBCompare", string.Empty);
            cmd.Parameters.AddWithValue("Friends_Count", user.FriendsCount);
            cmd.Parameters.AddWithValue("Favourites_Count", user.FavouritesCount);
            cmd.Parameters.AddWithValue("Total_Tweet_Count", user.StatusesCount);
            cmd.Parameters.AddWithValue("Weekly_Tweet_Count", -1);
            cmd.Parameters.AddWithValue("Weekly_Retweet_Count", -1);
            cmd.Parameters.AddWithValue("Location", user.Location);
            cmd.Parameters.AddWithValue("Twitter_Name", user.Name);
            cmd.Parameters.AddWithValue("Time_Zone", (user.TimeZone != null) ? user.TimeZone : "");
            cmd.Parameters.AddWithValue("Created_At", user.CreatedAt.ToString());
            cmd.Parameters.AddWithValue("Time_Stamp", DateTime.Now.ToString());

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
                              + "Followers(UserID, Name, Follows, Description, Screen_Name, Followers_Count, Followers_Added, Followers_Removed, PrevDBCompare, Friends_Count, Favourites_Count, Total_Tweet_Count, Weekly_tweet_Count, Weekly_Retweet_Count, Location, Twitter_Name, Created_At, Time_Stamp)" +
                            "VALUES(@UserID, @Name, @Follows, @Description, @Screen_Name, @Followers_Count, @Followers_Added, @Followers_Removed, @PrevDBCompare, @Friends_Count, @Favourites_Count, @Total_Tweet_Count, @Weekly_Tweet_Count, @Weekly_Retweet_Count, @Location, @Twitter_Name, @Created_At, @Time_Stamp)";

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
            cmd.Parameters.AddWithValue("Followers_Added", -1);
            cmd.Parameters.AddWithValue("Followers_Removed", -1);
            cmd.Parameters.AddWithValue("PrevDBCompare", string.Empty);
            cmd.Parameters.AddWithValue("Friends_Count", user.FriendsCount);
            cmd.Parameters.AddWithValue("Favourites_Count", user.FavouritesCount);
            cmd.Parameters.AddWithValue("Total_Tweet_Count", user.StatusesCount);
            cmd.Parameters.AddWithValue("Weekly_tweet_Count", -1);
            cmd.Parameters.AddWithValue("Weekly_Retweet_Count", -1);
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

            string sqlCommand = "INSERT INTO Tweets(TweetId, UserId, Tweet, Retweet_Count, TimeOfTweet) VALUES(@TweetId, @UserId, @Tweet, @Retweet_Count, @TimeOfTweet)";

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
                cmd.Parameters.AddWithValue("Retweet_Count", Tweet.RetweetCount);
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

        public int ExecuteSQLNonQuery(string cmd, string ConnectionString)
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

            int rowsAffected = 0;

            try
            {
                rowsAffected = cmd1.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                rc = ErrorCodes.ErrorOnCreating;
            }

            return rowsAffected;
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

        public void CheckForNewTweets(IUser User, IToken iToken, List<ITweet> NewTweets, string OldTweetId, string connectionString)
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

                    GetRetweetsFromTweet(NewTweets[i], iToken, connectionString, User); //Initiate Retweets of New Tweets to Data Base
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

        public List<IUser> GetFollowers(IUser iUser, IToken iToken, string iConnectionString, int iNumberOfFollowersToGet, bool iReturnUserList)
        {
            List<long> followersIDs = null;
            List<IUser> currFollowers = null;

            if (iNumberOfFollowersToGet <= iUser.FollowersCount)
            {
                try
                {
                    //Get Users from twitter
                    followersIDs = iUser.GetFollowerIds(iToken, iNumberOfFollowersToGet, false, 0);
                    currFollowers = UserUtils.Lookup(followersIDs, new List<string>(), iToken);

                    //Add users to DB
                    InitiateFollowersListToDB(iUser.IdStr, currFollowers, iConnectionString);
                }
                catch (Exception ex)
                {
                    if (ex.ToString().Contains("401"))
                    {
                        Console.WriteLine(string.Format("{0}: The user: {1} ({2}) doesn't allow his followers to be queried.", DateTime.Now, iUser.IdStr, iUser.Name));
                    }
                    else
                    {
                        Console.WriteLine(string.Format("{0}: {1}", DateTime.Now, ex.ToString()));
                    }
                }
            }
            else
            {
                throw new Exception("Error: Number of followers to get is greater than the user actual followers number");
            }

            if (!iReturnUserList)
            {
                currFollowers = null;
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

        public void GetTweets(IUser User, IToken iToken, string connectionStringUsers, bool iGetRetweets)
        {
            List<ITweet> Tweets = null;

            try
            {
                Tweets = User.GetUserTimeline(true, iToken);       //Get tweets from user
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
                CheckForNewTweets(User, iToken, Tweets, currUserMaxID, connectionStringUsers); //Check if there are any new tweets, If new Tweets exist, initiate them to Data Base (RetWeets Are Being Initiated Here Also)
            }        
            else
            {
                foreach (var Tweet in Tweets)
                {
                    InitiateTweetToDB(Tweet, connectionStringUsers, User.IdStr);   //Initiate Tweets to data base FOR FIRST TIME

                    if (iGetRetweets)
                    {
                        GetRetweetsFromTweet(Tweet, iToken, connectionStringUsers, User); //Get Retweets From Tweet And Initiate To Data Base FOR FIRST TIME 
                    }
                }
            }
        }

        /// <summary>
        /// Queries twitter for tweets created between iStartDate and iEndDate
        /// Only the first 200 tweets will be taken into account, the function doesn't use paging.
        /// </summary>
        /// <param name="iUser"></param>
        /// <param name="iToken"></param>
        /// <param name="iConnectionString"></param>
        /// <param name="iGetRetweets"></param>
        /// <param name="iStartTime"></param>
        /// <param name="iUntil"></param>
        public void GetTweetsBetweenDates(IUser iUser, IToken iToken, string iConnectionString, bool iGetRetweets, bool iIsCompany, bool iAddToDB, DateTime iSince, DateTime iUntil)
        {
            List<ITweet> Tweets = null;
            List<ITweet> neededTweets = null;//new List<ITweet>();

            try
            {
                List<TweetObject> tweetObjects = _TwitterAPI.GetUserTimeLine(iUser.IdStr, 3200);

                Tweets = ConvertListTweetObjectToTweet(tweetObjects);

                //Tweets = iUser.GetUserTimelineCursored(true, iToken);
            }
            catch (Exception ex)
            {
                if (!ex.ToString().Contains("401"))
                {
                    throw;
                }
            }

            if (Tweets != null)
            {
                if (Tweets.Count > 0)
                {
                    neededTweets = new List<ITweet>();
                    for (int i = 0; i < Tweets.Count; i++)
                    {
                        if (Tweets[i].CreatedAt > iSince && Tweets[i].CreatedAt < iUntil)
                        {
                            neededTweets.Add(Tweets[i]);
                        }
                    } 
                } 
            }

            SqlCeDataReader Response = ExecuteSQLQuery("SELECT MAX(TweetID) FROM TWEETS where UserID='" + iUser.IdStr + "'", iConnectionString); //Get Last Id From Data Base (after collecting data)
            string currUserMaxID = string.Empty;
            if (Response != null)
            {
                if (Response.Read())
                {
                    //currUserMaxID = Response["UserID"].ToString();
                    currUserMaxID = Response["Column1"].ToString();
                }
            }


            //if (currUserMaxID != string.Empty)
            //{
            //    CheckForNewTweets(User, Tweets, currUserMaxID, connectionStringUsers); //Check if there are any new tweets, If new Tweets exist, initiate them to Data Base (RetWeets Are Being Initiated Here Also)
            //}
            //else
            //{
                if (iAddToDB)
                {
                    foreach (var Tweet in neededTweets)
                    {
                        InitiateTweetToDB(Tweet, iConnectionString, iUser.IdStr);   //Initiate Tweets to data base FOR FIRST TIME

                        if (iGetRetweets)
                        {
                            GetRetweetsFromTweet(Tweet, iToken, iConnectionString, iUser); //Get Retweets From Tweet And Initiate To Data Base FOR FIRST TIME 
                        }
                    } 
                }

                int sumRetweetCount = 0;
                string sqlCmd = string.Empty;
                //update user retweet_count, distinguish between users and followers tables
                if (iIsCompany)
                {
                    sqlCmd = string.Format("Select Sum(Retweet_count) from tweets where userid='{0}'", iUser.IdStr);
                    Response = ExecuteSQLQuery(sqlCmd, iConnectionString); //Get Last Id From Data Base (after collecting data)
                    
                    if (Response != null)
                    {
                        if (Response.Read())
                        {
                            //currUserMaxID = Response["UserID"].ToString();
                            try
                            {
                                sumRetweetCount = Int32.Parse(Response["Column1"].ToString());
                            }
                            catch (Exception)
                            {
                            }

                            if (sumRetweetCount != 0 && neededTweets != null)
                            {
                                sqlCmd = string.Format("Update Users set Weekly_Tweet_count = {0}, Weekly_Retweet_count={1} where userid='{2}'", neededTweets.Count, sumRetweetCount, iUser.IdStr);
                                ExecuteSQLNonQuery(sqlCmd, iConnectionString);
                            }
                        }
                    }
                }
                else //handle tweets for followers
                {
                    if (neededTweets != null)
                    {
                        foreach (var Tweet in neededTweets)
                        {
                            sumRetweetCount += (int)Tweet.RetweetCount;
                        }

                        sqlCmd = string.Format("Update followers set Weekly_Tweet_count = {0}, Weekly_Retweet_count={1} where userid='{2}'", neededTweets.Count, sumRetweetCount, iUser.IdStr);
                        ExecuteSQLNonQuery(sqlCmd, iConnectionString);
                         
                    }
                }
            //}
        }

        private ITweet ConvertTweetObjectToTweet(TweetObject iTweet)
        {
            ITweet tweet;

            try
            {
                Dictionary<string, object> dTweet = new Dictionary<string, object>();

                dTweet.Add("contributors", iTweet.contributors);
                dTweet.Add("coordinates", iTweet.coordinates);
                dTweet.Add("created_at", iTweet.created_at);
                dTweet.Add("entities", iTweet.entities);
                dTweet.Add("favorite_count", iTweet.favorite_count);
                dTweet.Add("favorited", iTweet.favorited);
                dTweet.Add("geo", iTweet.geo);
                dTweet.Add("id", iTweet.id);
                dTweet.Add("id_str", iTweet.id_str);
                dTweet.Add("in_reply_to_screen_name", iTweet.in_reply_to_screen_name);
                dTweet.Add("in_reply_to_status_id", iTweet.in_reply_to_status_id);
                dTweet.Add("in_reply_to_status_id_str", iTweet.in_reply_to_status_id_str);
                dTweet.Add("in_reply_to_user_id", iTweet.in_reply_to_user_id);
                dTweet.Add("in_reply_to_user_id_str", iTweet.in_reply_to_user_id_str);
                dTweet.Add("lang", iTweet.lang);
                dTweet.Add("place", iTweet.place);
                dTweet.Add("possibly_sensitive", iTweet.possibly_sensitive);
                dTweet.Add("retweet_count", iTweet.retweet_count);
                dTweet.Add("retweeted", iTweet.retweeted);
                dTweet.Add("retweeted_status", iTweet.retweeted_status);
                dTweet.Add("source", iTweet.source);
                dTweet.Add("text", iTweet.text);
                dTweet.Add("truncated", iTweet.truncated);
                dTweet.Add("user", iTweet.user);

                tweet = new Tweet(dTweet);
            
                //tweet.ContributorsIds = (int[])iTweet.contributors;
                //tweet.CreatedAt = DateTime.ParseExact(iTweet.created_at,
                //        "ddd MMM dd HH:mm:ss zzzz yyyy", CultureInfo.InvariantCulture);
                //tweet.Id = Convert.ToInt64(iTweet.id_str);
                ////tweet.IdValue = Convert.ToInt64(iTweet.id);
                //tweet.IdStr = iTweet.id_str;
                //tweet.Text = iTweet.text;
                //tweet.Source = iTweet.source;
                //tweet.Truncated = iTweet.truncated as bool?;
                //tweet.InReplyToStatusId = iTweet.in_reply_to_status_id as int?;
                //tweet.InReplyToStatusIdStr = iTweet.in_reply_to_status_id_str as string;
                //tweet.InReplyToUserId = iTweet.in_reply_to_user_id as int?;
                //tweet.InReplyToUserIdStr = iTweet.in_reply_to_user_id_str as string;
                //tweet.InReplyToScreenName = iTweet.in_reply_to_screen_name as string;

                //TwitterUserTimeLine.User user = iTweet.user;
                //Dictionary<string, object> dUser = new Dictionary<string, object>();
                //dUser.Add("contributors_enabled", user.contributors_enabled);
                //dUser.Add("created_at", user.created_at);
                //dUser.Add("default_profile", user.default_profile);
                //dUser.Add("default_profile_image", user.default_profile_image);
                //dUser.Add("description", user.description);
                //dUser.Add("entities", user.entities);
                //dUser.Add("favourites_count", user.favourites_count);
                //dUser.Add("follow_request_sent", user.follow_request_sent);
                //dUser.Add("followers_count", user.followers_count);
                //dUser.Add("following", user.following);
                //dUser.Add("friends_count", user.friends_count);
                //dUser.Add("geo_enabled", user.geo_enabled);
                //dUser.Add("id", user.id);
                //dUser.Add("id_str", user.id_str);
                //dUser.Add("is_translation_enabled", user.is_translation_enabled);
                //dUser.Add("is_translator", user.is_translator);
                //dUser.Add("lang", user.lang);
                //dUser.Add("listed_count", user.listed_count);
                //dUser.Add("location", user.location);
                //dUser.Add("name", user.name);
                //dUser.Add("notifications", user.notifications);
                //dUser.Add("profile_background_color", user.profile_background_color);
                //dUser.Add("profile_background_image_url", user.profile_background_image_url);
                //dUser.Add("profile_background_image_url_https", user.profile_background_image_url_https);
                //dUser.Add("profile_background_tile", user.profile_background_tile);
                //dUser.Add("profile_banner_url", user.profile_banner_url);
                //dUser.Add("profile_image_url", user.profile_image_url);
                //dUser.Add("profile_image_url_https", user.profile_image_url_https);
                //dUser.Add("profile_link_color", user.profile_link_color);
                //dUser.Add("profile_sidebar_border_color", user.profile_sidebar_border_color);
                //dUser.Add("profile_sidebar_fill_color", user.profile_sidebar_fill_color);
                //dUser.Add("profile_text_color", user.profile_text_color);
                //dUser.Add("profile_use_background_image", user.profile_use_background_image);
                //dUser.Add("@protected", user.@protected);
                //dUser.Add("screen_name", user.screen_name);
                //dUser.Add("statuses_count", user.statuses_count);
                //dUser.Add("time_zone", user.time_zone);
                //dUser.Add("url", user.url);
                //dUser.Add("utc_offset", user.utc_offset);
                //dUser.Add("verified", user.verified);


                //tweet.Creator = Tweetinvi.User.Create(dUser);
                //tweet.Location = Geo.Create(iTweet.geo);

                //if (tweet.Location != null)
                //{
                //    tweet.LocationCoordinates = tweet.Location.GeoCoordinates;
                //}

                //// Create Contributors
                //var contributors = iTweet.contributors;

                //tweet.RetweetCount = iTweet.retweet_count as int?;

                ////if (iTweet.entities != null)
                ////{
                ////    tweet.Entities = new TweetEntities(iTweet.entities as Dictionary<String, object>);
                ////}

                //tweet.Favourited = iTweet.favorited as bool?;
                //tweet.Retweeted = iTweet.retweeted as bool?;
                //tweet.PossiblySensitive = iTweet.possibly_sensitive as bool?;

                ////if (iTweet.retweeted_status != null)
                ////{
                ////    var retweet = iTweet.retweeted_status as Dictionary<string, object>;

                ////    if (retweet != null)
                ////    {
                ////        tweet.Retweeting = new Tweet(retweet);
                ////    }
                ////}
            }
            catch (Exception)
            {

                throw;
            }

            return tweet;
        }

        private List<ITweet> ConvertListTweetObjectToTweet(List<TweetObject> iTweets)
        {
            List<ITweet> tweetObjects = new List<ITweet>();

            foreach (TweetObject tweet in iTweets)
            {
                ITweet t = ConvertTweetObjectToTweet(tweet);

                if (t != null)
                {
                    tweetObjects.Add(t);
                }
            }

            return tweetObjects;
        }

        public void GetTweetsBetweenDatesPaged(IUser iUser, IToken iToken, string iConnectionString, bool iGetRetweets, bool iIsCompany, bool iAddToDB, DateTime iSince, DateTime iUntil)
        {
            List<ITweet> Tweets = null;
            List<ITweet> neededTweets = null;//new List<ITweet>();

            try
            {
                Tweets = iUser.GetUserTimeline(true, iToken);       //Get tweets from user
            }
            catch (Exception ex)
            {
                if (!ex.ToString().Contains("401"))
                {
                    throw;
                }
            }

            if (Tweets != null)
            {
                if (Tweets.Count > 0)
                {
                    neededTweets = new List<ITweet>();
                    for (int i = 0; i < Tweets.Count; i++)
                    {
                        if (Tweets[i].CreatedAt > iSince && Tweets[i].CreatedAt < iUntil)
                        {
                            neededTweets.Add(Tweets[i]);
                        }
                    }
                }
            }

            SqlCeDataReader Response = ExecuteSQLQuery("SELECT MAX(TweetID) FROM TWEETS where UserID='" + iUser.IdStr + "'", iConnectionString); //Get Last Id From Data Base (after collecting data)
            string currUserMaxID = string.Empty;
            if (Response != null)
            {
                if (Response.Read())
                {
                    //currUserMaxID = Response["UserID"].ToString();
                    currUserMaxID = Response["Column1"].ToString();
                }
            }


            //if (currUserMaxID != string.Empty)
            //{
            //    CheckForNewTweets(User, Tweets, currUserMaxID, connectionStringUsers); //Check if there are any new tweets, If new Tweets exist, initiate them to Data Base (RetWeets Are Being Initiated Here Also)
            //}
            //else
            //{
            if (iAddToDB)
            {
                foreach (var Tweet in neededTweets)
                {
                    InitiateTweetToDB(Tweet, iConnectionString, iUser.IdStr);   //Initiate Tweets to data base FOR FIRST TIME

                    if (iGetRetweets)
                    {
                        GetRetweetsFromTweet(Tweet, iToken, iConnectionString, iUser); //Get Retweets From Tweet And Initiate To Data Base FOR FIRST TIME 
                    }
                }
            }

            int sumRetweetCount = 0;
            string sqlCmd = string.Empty;
            //update user retweet_count, distinguish between users and followers tables
            if (iIsCompany)
            {
                sqlCmd = string.Format("Select Sum(Retweet_count) from tweets where userid='{0}'", iUser.IdStr);
                Response = ExecuteSQLQuery(sqlCmd, iConnectionString); //Get Last Id From Data Base (after collecting data)

                if (Response != null)
                {
                    if (Response.Read())
                    {
                        //currUserMaxID = Response["UserID"].ToString();
                        try
                        {
                            sumRetweetCount = Int32.Parse(Response["Column1"].ToString());
                        }
                        catch (Exception)
                        {
                        }

                        if (sumRetweetCount != 0 && neededTweets != null)
                        {
                            sqlCmd = string.Format("Update Users set Weekly_Tweet_count = {0}, Weekly_Retweet_count={1} where userid='{2}'", neededTweets.Count, sumRetweetCount, iUser.IdStr);
                            ExecuteSQLNonQuery(sqlCmd, iConnectionString);
                        }
                    }
                }
            }
            else //handle tweets for followers
            {
                if (neededTweets != null)
                {
                    foreach (var Tweet in neededTweets)
                    {
                        sumRetweetCount += (int)Tweet.RetweetCount;
                    }

                    sqlCmd = string.Format("Update followers set Weekly_Tweet_count = {0}, Weekly_Retweet_count={1} where userid='{2}'", neededTweets.Count, sumRetweetCount, iUser.IdStr);
                    ExecuteSQLNonQuery(sqlCmd, iConnectionString);

                }
            }
            //}
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

        public void GetRetweetsFromTweet(ITweet Tweet, IToken iToken, string connectionStringUsers, IUser User)
        {

            if (Tweet.RetweetCount > 0)
            {
                List<ITweet> ReTweets = Tweet.GetRetweets(false, true, iToken);    //Get ReTweets

                if (ReTweets != null && ReTweets.Count > 0)
                {
                    foreach (var ReTweet in ReTweets)
                    {
                        InitiateReTweetsToDB(ReTweet, connectionStringUsers, User.IdStr);   //Initiate ReTweets to data base
                    } 
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

            if (iDepth > 0)
            {
                if (iDepth == 1)
                {
                    GetTweetsBetweenDates(iUser, iToken, iConnectionString, false, false, false, DateTime.Now.Subtract(new System.TimeSpan(7, 0, 0, 0)), DateTime.Now);
                    //GetFollowers(iUser, iToken, iConnectionudenString, numFollowersToGet);
                }
                else
                {

                    //List<IUser> currentUserFollowers = GetFollowers(iUser, iToken, iConnectionString, numFollowersToGet, false);
                    GetFollowers(iUser, iToken, iConnectionString, numFollowersToGet, false);

                    if (_LastDBGenerated != string.Empty)
                    {
                        //build previous DB connections string
                        string previousDBConnectionString = string.Format("DataSource={0}", _LastDBGenerated);

                        UpdateAddedRemovedFollowersFieldsForUser(iUser, previousDBConnectionString, iConnectionString, true);
                    }

                    List<string> companyFollowers = GetFollowersIDs(iUser.IdStr, iConnectionString);

                    foreach (string follower in companyFollowers)
                    {
                        List<long> ids = new List<long>();
                        ids.Add(long.Parse(follower));
                        List<IUser> dummyCurrFollower = Tweetinvi.UserUtils.Lookup(ids, null, iToken);
                        IUser currFollower = dummyCurrFollower[0];

                        GetTweetsBetweenDates(iUser, iToken, iConnectionString, false, false, false, DateTime.Now.Subtract(new System.TimeSpan(7, 0, 0, 0)), DateTime.Now);
                        GetFollowerInformationRec(currFollower, iToken, iDepth - 1, numFollowersToGet, iConnectionString);
                    }
                    //foreach (IUser follower in currentUserFollowers)
                    //{
                    //    GetTweetsBetweenDates(iUser, iToken, iConnectionString, false, false, false, DateTime.Now.Subtract(new System.TimeSpan(7, 0, 0, 0)), DateTime.Now);
                    //    GetFollowerInformationRec(follower, iToken, iDepth - 1, numFollowersToGet, iConnectionString);
                    //}
                } 
            }

        }

        public List<string> GetFollowersIDs(string iParentID, string iConnectionString)
        {
            string sqlCmd = string.Empty;
            List<string> followersIDs = null;

            sqlCmd = string.Format("Select UserID from followers where follows='{0}'", iParentID);

            SqlCeDataReader Response = ExecuteSQLQuery(sqlCmd, iConnectionString);

            if (Response != null)
            {
                followersIDs = new List<string>();

                while (Response.Read())
                {
                    try
                    {
                        followersIDs.Add(Response["UserID"].ToString());
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }

            return followersIDs;
        }

        public void ConvertDBDataToCSV(string iQuery, string[] iColumnNames, string iConnectionString, string iFilename, string iDirectory)
        {
            SqlCE2CSVConverter.ConvertDBTableToCSV(iQuery, iColumnNames, iConnectionString, iFilename, true, iDirectory);
        }

        private void ReadConfigurationSection(string iConfigurationSection, ref List<string> oContainer, bool iMakeLowercase)
        {
            NameValueCollection temp = (NameValueCollection)ConfigurationManager.GetSection(iConfigurationSection);

            foreach (string key in temp)
            {

                if (iMakeLowercase)
                {
                    oContainer.Add(temp[key].ToLower());
                }
                else
                {
                    oContainer.Add(temp[key]);
                }
            }
        }

        private bool IsFollowerExistsInPreviousDB(string iParentUserID, string iFollowerID, string iPrevDBConnectionsString, string iCurrDBConnectionString)
        {
            bool isExists = false;

            //build sql query for previous db and get response from db
            string sqlCmd = string.Empty;
            string prevDBfollowerID = string.Empty;
            string currDBfollowerID = string.Empty;

            sqlCmd = string.Format("Select UserID from followers where follows='{0}' and UserID = '{1}'", iParentUserID, iFollowerID);
            SqlCeDataReader Response = ExecuteSQLQuery(sqlCmd, iPrevDBConnectionsString);

            if (Response != null)
            {
                Response.Read();
                
                try
                {
                    prevDBfollowerID = (Response["UserID"].ToString());
                }
                catch (Exception)
                {
                    throw;
                }
                
            }

            //build sql query for current db
            sqlCmd = string.Format("Select UserID from followers where follows='{0}' and UserID = '{1}'", iParentUserID, iFollowerID);
            Response = ExecuteSQLQuery(sqlCmd, iCurrDBConnectionString);

            if (Response != null)
            {
                Response.Read();

                try
                {
                    currDBfollowerID = (Response["UserID"].ToString());
                }
                catch (Exception)
                {
                    throw;
                }

            }

            if (prevDBfollowerID == currDBfollowerID)
            {
                isExists = true;
            }

            return isExists;
        }

        public void UpdateAddedRemovedFollowersFieldsForUser(IUser iUser, string iPrevDBConnectionsString, string iCurrDBConnectionString, bool iIsCompany)
        {
            int added = 0;
            int removed = 0;
            List<string> prevDBfollowersIDs = null;
            List<string> currDBfollowersIDs = null;

            //Go over all followers of a user in the previous DB and see if they exist in the current one as well
            bool isExists = false;

            //build sql query for previous db and get response from db
            string sqlCmd = string.Empty;
            string prevDBfollowerID = string.Empty;
            string currDBfollowerID = string.Empty;

            if (_LastDBGenerated != string.Empty)
            {
                sqlCmd = string.Format("Select UserID from followers where follows='{0}'", iUser.IdStr);
                SqlCeDataReader Response = ExecuteSQLQuery(sqlCmd, iPrevDBConnectionsString);

                if (Response != null)
                {
                    prevDBfollowersIDs = new List<string>();

                    while (Response.Read())
                    {
                        try
                        {
                            prevDBfollowersIDs.Add(Response["UserID"].ToString());
                        }
                        catch (Exception)
                        {
                            throw;
                        }
                    }
                }

                sqlCmd = string.Format("Select UserID from followers where follows='{0}'", iUser.IdStr);
                Response = ExecuteSQLQuery(sqlCmd, iCurrDBConnectionString);

                if (Response != null)
                {
                    currDBfollowersIDs = new List<string>();

                    while (Response.Read())
                    {
                        try
                        {
                            currDBfollowersIDs.Add(Response["UserID"].ToString());
                        }
                        catch (Exception)
                        {
                            throw;
                        }
                    }
                }


                //Go over previous DB and look for users that were removed
                foreach (string userID in prevDBfollowersIDs)
                {
                    if (!currDBfollowersIDs.Contains(userID))
                    {
                        removed++;
                    }
                }

                //Go over current DB and look for users that were added
                foreach (string userID in currDBfollowersIDs)
                {
                    if (!prevDBfollowersIDs.Contains(userID))
                    {
                        added++;
                    }
                }

                //Write these values in the current DB
                if (iIsCompany)
                {
                    sqlCmd = string.Format("Update Users Set Followers_added={0}, Followers_removed={1}, PrevDBCompare='{2}' where UserID='{3}'", added, removed, _LastDBGenerated, iUser.IdStr);
                }
                else
                {
                    sqlCmd = string.Format("Update Followers Set Followers_added={0}, Followers_removed={1}, PrevDBCompare='{2}' where UserID='{3}'", added, removed, _LastDBGenerated, iUser.IdStr);
                }
                int res = ExecuteSQLNonQuery(sqlCmd, iCurrDBConnectionString);
            }
        }
    }
}

//public void GetTwitterTreeRec(int iDepth, IUser iUser, IToken iToken, string iConnectionString)
//        {

//        //This is a Recursive data collection function
//        //The parameter depth specifies the order of depth of the network to retrieve

//            if (iDepth == 1) //if depth = 1, collect only the initiated User's followers and tweets
//            {
//                GetTweets(iUser, iToken, iConnectionString, true);

//                if ((int)iUser.FollowersCount <= 6000)
//                {
//                    GetFollowers(iUser, iToken, iConnectionString, (int)iUser.FollowersCount);
//                }
//                else
//                {
//                    GetFollowers(iUser, iToken, iConnectionString, 6000);
//                }
//            }	
//            else //If depth > 1, dive to depth and go back while collecting from each stair (depth = 1)
//            {
//                GetTweets(iUser, iToken, iConnectionString, true);
                
//                List<IUser> Followers = null;
//                if ((int)iUser.FollowersCount <= 6000)
//                {
//                    Followers = GetFollowers(iUser, iToken, iConnectionString, (int)iUser.FollowersCount);
//                }
//                else
//                {
//                    Followers = GetFollowers(iUser, iToken, iConnectionString, 6000);
//                }

//                foreach (var Follower in Followers)
//                {
//                    GetTwitterTreeRec(iDepth - 1, Follower, iToken, iConnectionString);
//                }
//            }
//        }        