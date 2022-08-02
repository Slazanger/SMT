![Build status](https://robmburke.visualstudio.com/ESI.NET/_apis/build/status/ESI.NET) ![Quality gate](https://sonarcloud.io/api/project_badges/measure?project=ESI.NET&metric=alert_status) ![NuGet](https://img.shields.io/nuget/v/ESI.NET.svg)

# What is ESI.NET?

**ESI.NET** is a .NET wrapper for the [Eve Online ESI API](https://esi.evetech.net/). This wrapper simplifies the process of integrating ESI into your .NET application.

### Resources
* [Discord - E.N](https://discord.gg/SvdN39f) - This channel is where you can contact me (Psianna Archeia) for questions and where automated webhook notifications will be pushed for github and when builds are completed. (If you have Discord, this is the preferred way to contact me concerning ESI.NET. I **DO NOT** monitor Slack anymore for ESI.NET issues.)
* [Tweetfleet - #esi](https://tweetfleet.slack.com/messages/C30KX8UUX/) - This is the official slack channel to speak with CCP devs (and developers) concerning ESI.
* [ESI Application Keys](https://developers.eveonline.com/)
* [ESI Swagger Definition](https://esi.tech.ccp.is/swagger.json)
* [ESI-Docs](https://docs.esi.evetech.net/) ([source](https://github.com/esi/esi-docs)) - This is the best documentation concerning ESI and the SSO process.

It is extremely important to not solely rely on ESI.NET. You may need to refer to the official specifications to understand what data is expected to be provided. For example, in some instances, ESI.NET will ask for specific values in the endpoint method and construct the JSON object that needs to be sent in the POST request body because it is a simple object that requires a few values. Some of the more complex objects will need to be constructed with anonymous objects by the developer and this can be determined when the endpoint method requires an `object` instead of an `int` or a `string`. Refer to the official documentation and construct the anonymous object to reflect what is expected as Json.NET will be able to convert that anonymous object into the appropriate JSON data.

## ESI.NET on NuGet
https://www.nuget.org/packages/ESI.NET

`dotnet add package ESI.NET `

## Client Instantiation
ESI.NET is Dependency Injection compatible. There are a few parts required to set this up properly in a .NET Standard/Core application:

### .NET Standard (Dependency Injection)
In your appsettings.json, add the following object and fill it in appropriately:
```json
"EsiConfig": {
    "EsiUrl": "https://esi.evetech.net/",
    "DataSource": "Tranquility",
    "ClientId": "**********",
    "SecretKey": "**********",
    "CallbackUrl": "",
    "UserAgent": "",
    "AuthVersion": "v2"
  }
```
*For your protection (and mine), you are required to supply a user_agent value. This can be your character name and/or project name. CCP will be more likely to contact you than just cut off access to ESI if you provide something that can identify you within the New Eden galaxy. Without this property populated, the wrapper will not work.*

Inject the EsiConfig object into your configuration in `Startup.cs` in the `ConfigureServices()` method:
```cs
services.AddEsi(Configuration.GetSection("ESIConfig"));
```

Lastly, access the client in your class constructor (the config options above will automatically be injected into it:
```cs
private readonly IEsiClient _client;
public ApiTestController(IEsiClient client) { _client = client; }

```

### .NET Framework
If you are using a .NET Standard-compatible .NET Framework application, you can instantiate the client in this manner:

```cs
IOptions<EsiConfig> config = Options.Create(new EsiConfig()
{
    EsiUrl = "https://esi.evetech.net/",
    DataSource = DataSource.Tranquility,
    ClientId = "**********",
    SecretKey = "**********",
    CallbackUrl = "",
    UserAgent = "",
    AuthVersion = AuthVersion.v2
});

EsiClient client = new EsiClient(config);
```
*For your protection (and mine), you are required to supply a user_agent value. This can be your character name and/or project name. CCP will be more likely to contact you than just cut off access to ESI if you provide something that can identify you within the New Eden galaxy. Without this property populated, the wrapper will not work.*

NOTE: You will need to import `Microsoft.Extensions.Options` to accomplish the above.

### Endpoint Example
Accessing a public endpoint is extremely simple:
```cs
EsiResponse response = _client.Universe.Names(new List<long>()
{
    1590304510,
    99006319,
    20000006
}).Result;
```

## SSO Example

### SSO Login URL generator
ESI.NET has a helper method to generate the URL required to authenticate a character or authorize roles (by providing a List<string> of scopes) for the Eve Online SSO.  You should also provide a value for "state" that you verify when it is returned (it will be included in the callback).
```cs
var url = _client.SSO.CreateAuthenticationUrl();
```

### Initial SSO Token Request
```cs
SsoToken token = await _client.SSO.GetToken(GrantType.AuthorizationCode, code);
AuthorizedCharacterData auth_char = await _client.SSO.Verify(token);
```
### Refresh Token Request
```cs
SsoToken token = await _client.SSO.GetToken(GrantType.RefreshToken, auth_char.RefreshToken);
```
### Performing an authenticated request
Set the character data on the client before performing the request.
```cs
_client.SetCharacterData(authorizedCharacterData)
```
