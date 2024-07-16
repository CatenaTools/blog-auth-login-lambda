using System.Text.Json.Serialization;
using Models;
using MySql.Data.MySqlClient;

namespace ServerlessAPI.Database;


public class AccountsDB(string server, string databaseName, string userID, string password)
{
    private string _server = server;
    private string _dbName = databaseName;
    private string _userID = userID;
    private string _password = password;
    
    private string DBConnectionString = $"Server={server};Uid={userID};Pwd={password};Database={databaseName}";

    /// <summary>
    /// ConnectAndInitialize is designed to do the first time initialization of the database, it will create the tables and database
    /// This function will connect without a dbname since it theoretically may not exist. This is to work around a limitation in cloudformation
    /// which does not allow you to create or manage the database schema directly from cloudformation. 
    /// </summary>
    public bool ConnectAndInitialize(string url)
    {

        var CreateTables = """
                           
                                   CREATE DATABASE IF NOT EXISTS accounts;
                           
                                   USE accounts;
                           
                                   CREATE TABLE IF NOT EXISTS accounts
                                   (
                                       account_id VARCHAR(255),
                                       username VARCHAR(255),
                                       PRIMARY KEY (account_id)
                                   );
                           
                                   CREATE TABLE IF NOT EXISTS
                                       auth_providers
                                   (
                                       account_id          VARCHAR(255),
                                       provider_account_id VARCHAR(255) UNIQUE,
                                       provider            VARCHAR(255),
                                       PRIMARY KEY (account_id),
                                       FOREIGN KEY(account_id) REFERENCES accounts (account_id)
                                   );
                                   
                                   CREATE TABLE IF NOT EXISTS
                                       configuration
                                   (
                                       config_key VARCHAR(255),
                                       config_value VARCHAR(255),
                                       PRIMARY KEY (config_key)
                                   );
                                   
                                   CREATE TABLE IF NOT EXISTS 
                                        sessions
                                    (
                                        session_id VARCHAR(255),
                                        account_id VARCHAR(255),
                                        FOREIGN KEY(account_id) REFERENCES accounts (account_id)
                                    );
                           
                                   INSERT INTO configuration (config_key, config_value) VALUES ('url', @url);
                                   
                           """;

        var DBConnectionString = $"Server={_server};Uid={_userID};Pwd={_password}";

        using var connection = new MySqlConnection(DBConnectionString);

        connection.Open();
        var tx = connection.BeginTransaction();

        var command = connection.CreateCommand();
        command.CommandText = CreateTables;
        command.Parameters.AddWithValue("@dbname", _dbName);
        command.Parameters.AddWithValue("@url", url);
        
        var r = command.ExecuteNonQuery();

        tx.Commit();

        return true;
    }

    public string GetPublicUrl()
    {
        using var connection = new MySqlConnection(DBConnectionString);
        
        connection.Open();
        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT config_key,config_value FROM configuration";
        var reader = cmd.ExecuteReader();

        var res = new Dictionary<string, string>();
        while (reader.Read())
        {
            res.Add(reader["config_key"].ToString(), reader["config_value"].ToString());
        }

        reader.Close();

        return res.GetValueOrDefault("url", "");
    }

    /// <summary>
    /// GetAccountByProvider returns the account associated with the provider string and provider
    /// account id passed as parameters. This method will return a null value if no account is found, which
    /// indicates we should create an account instead.
    /// 
    /// </summary>
    /// <param name="provider_account_id">the unique id for the account on the 3rd party provider</param>
    /// <param name="provider">a string identifying the provider</param>
    /// <returns></returns>
    public Account? GetAccountByProvider(string provider_account_id, string provider)
    {
        using var connection = new MySqlConnection(DBConnectionString);
        
        connection.Open();

        var query = """
                    SELECT accounts.account_id as account_id, accounts.username
                    FROM accounts
                             JOIN auth_providers ON accounts.account_id = auth_providers.account_id
                    WHERE auth_providers.provider_account_id = @providerID
                      AND auth_providers.provider = @provider
                    
                    """;

        var command = connection.CreateCommand();

        command.CommandText = query;
        command.Parameters.AddWithValue("providerID", provider_account_id);
        command.Parameters.AddWithValue("provider", provider);

        var reader = command.ExecuteReader();

        var username = "";
        var accountID = "";
        while (reader.Read())
        {
            username = reader["username"].ToString();
            accountID = reader["account_id"].ToString();
        }
        
        if (username == "" || accountID == "")
        {
            return null;
        }
        else
        {
            return new Account(accountID, username);
        }
    }

    public string InsertNewAccount(string username, string provider_account_id, string provider)
    {
        var account_id = Guid.NewGuid().ToString();
        using var connection = new MySqlConnection(DBConnectionString);

        connection.Open();

        var transaction = connection.BeginTransaction();

        var createAccount = connection.CreateCommand();
        createAccount.CommandText =
            "INSERT INTO accounts (account_id, username) VALUES (@account_id, @username)";
        createAccount.Parameters.AddWithValue("@account_id", account_id);
        createAccount.Parameters.AddWithValue("@username", username);
        createAccount.ExecuteNonQuery();
            
        var command = connection.CreateCommand();
        command.CommandText =
            "INSERT INTO auth_providers (account_id, provider_account_id, provider) VALUES (@account_id, @provider_account_id, @provider)";
        command.Parameters.AddWithValue("@account_id", account_id);
        command.Parameters.AddWithValue("@provider_account_id", provider_account_id);
        command.Parameters.AddWithValue("@provider", "discord");

        command.ExecuteNonQuery();

        transaction.Commit();
        return account_id;
    }

    public string CreateSession(string account_id)
    {
        var sessionID = Guid.NewGuid().ToString();
        using var connection = new MySqlConnection(DBConnectionString);
        
        connection.Open();

        var transaction = connection.BeginTransaction();

        var createSession = connection.CreateCommand();
        createSession.CommandText = """
                                    INSERT INTO sessions(session_id, account_id) VALUES (@session_id, @account_id)
                                    """;
        createSession.Parameters.AddWithValue("@session_id", sessionID);
        createSession.Parameters.AddWithValue("@account_id", account_id);

        createSession.ExecuteNonQuery();
        
        transaction.Commit();

        return sessionID;
    }

    public Account? GetAccountFromSession(string session_id)
    {
        using var connection = new MySqlConnection(DBConnectionString);
        
        connection.Open();

        var query = """
                    SELECT accounts.account_id as account_id, accounts.username
                    FROM accounts
                             JOIN sessions ON accounts.account_id = sessions.account_id
                    WHERE sessions.session_id = @sessionID
                    """;

        var command = connection.CreateCommand();

        command.CommandText = query;
        command.Parameters.AddWithValue("sessionID", session_id);

        var reader = command.ExecuteReader();

        var username = "";
        var accountID = "";
        while (reader.Read())
        {
            username = reader["username"].ToString();
            accountID = reader["account_id"].ToString();
        }
        
        if (username == "" || accountID == "")
        {
            return null;
        }
        else
        {
            return new Account(accountID, username);
        }
    }
}