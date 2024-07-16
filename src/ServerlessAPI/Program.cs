using System.Text.Json;
using Models;
using ServerlessAPI.Database;
using ServerlessAPI.Responses;
using ServerlessAPI.Validators;

var builder = WebApplication.CreateBuilder(args);

//Logger
builder.Logging
    .ClearProviders()
    .AddJsonConsole();

// Add services to the container.
builder.Services
    .AddControllers()
    .AddJsonOptions(options => { options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase; });

// Add AWS Lambda support. When running the application as an AWS Serverless application, Kestrel is replaced
// with a Lambda function contained in the Amazon.Lambda.AspNetCoreServer package, which marshals the request into the ASP.NET Core hosting framework.
builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

const string DiscordClientId = "";
const string DiscordClientSecret = "";

// read from env vars
var DBHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
var DBUser = Environment.GetEnvironmentVariable("DB_USER") ?? "root";
var DBPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "password";
var DBName = "accounts";
var InitializationSecret = Environment.GetEnvironmentVariable("INITIALIZATION_SECRET") ?? "secret";

var db = new AccountsDB(DBHost, DBName, DBUser, DBPassword);


var authValidators = new Dictionary<string,IAuthValidator> (){
    {"discord", new DiscordAuthValidator(DiscordClientId, DiscordClientSecret) }
};
    
app.MapPost("/initialize", async (HttpRequest request) =>
{
    var body = await request.ReadFromJsonAsync<ConfigurationRequest>();

    try
    {
        db.ConnectAndInitialize(body.url);
        return Results.Text("Initialized successfully");
    }
    catch (Exception e)
    {
        app.Logger.LogError("error initializing database: {error}" , e.ToString());
        return Results.StatusCode(500);
    }


});

app.MapGet("/", (HttpRequest request) =>
{
    var publicUrl = db.GetPublicUrl();

    var sessionID = request.Cookies.TryGetValue("session-id", out var sess);
    
    if (sessionID)
    {
        try
        {
            var account = db.GetAccountFromSession(sess);
            if (account != null)
            {
                return Results.Extensions.HtmlWithCookie(
                    $"<html><body>Welcome! <p>Username: {account.Username}</p> <p>Account ID: {account.Id}</p></body></html>",
                    new Dictionary<string, string>()
                    {
                        { "session-id", sess }
                    });
            }
        }
        catch (Exception e)
        {
            return Results.StatusCode(500);
        }
    }
    
    var url = authValidators["discord"].GenerateAuthUrl(publicUrl);

    return Results.Extensions.Html($"<html><body><a href={url}>Login With Discord</a></body></html>");
});

app.MapGet("/callback", (HttpRequest request) =>
{
    var redirectUrl = $"{db.GetPublicUrl()}callback";

    // Read the code query parameter
    var code = request.Query["code"];
    
    if (string.IsNullOrEmpty(code))
    {
        return Results.BadRequest("No code query parameter");
    }
    
    var userData = authValidators["discord"].HandleOAuthCallback(code, redirectUrl);
    

    var acct = db.GetAccountByProvider(userData.ProviderAccountId, "discord");
    if (acct != null)
    {
        var sess = db.CreateSession(acct.Id);
        
        return Results.Extensions.HtmlWithCookie($"<html><body><p>Username: {acct.Username}</p> <p>Account ID: {acct.Id}</p></body></html>", new Dictionary<string, string>()
        {
            { "session-id", sess }
        });
    }
    
    try
    {

        var account_id = db.InsertNewAccount(userData.ProviderUsername,userData.ProviderAccountId, "discord");
        var sess = db.CreateSession(account_id);
        return Results.Extensions.HtmlWithCookie($"<html><body><p>Username: {userData.ProviderUsername}</p> <p>Account ID: {account_id}</p></body></html>", new Dictionary<string, string>()
        {
            { "session-id", account_id }
        });
        
    }
    catch (Exception e)
    {
        return Results.Text(e.ToString());
    }

});


app.Run();