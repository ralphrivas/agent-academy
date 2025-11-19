# Quick Start Guide - MC Post Processor

Get the MC Post Processor Azure Function running locally in 5 minutes.

## Prerequisites

- .NET 8.0 SDK ([Download](https://dotnet.microsoft.com/download/dotnet/8.0))
- Azure Functions Core Tools v4 ([Install](https://docs.microsoft.com/azure/azure-functions/functions-run-local))
- Visual Studio Code or Visual Studio 2022 (optional)

## Steps

### 1. Get the Code

```bash
git clone https://github.com/ralphrivas/agent-academy.git
cd agent-academy/MCPostProcessor
```

### 2. Configure Local Settings

```bash
# Copy the template
cp local.settings.json.template local.settings.json

# Edit the file with your values
# - Use your Azure AD tenant ID and app ID
# - Add your client secret
# - Update SharePoint site URL
# - Set email recipient
```

**Minimum required changes in `local.settings.json`:**
```json
{
  "AppSettings": {
    "TenantId": "YOUR_TENANT_ID",
    "ApplicationId": "YOUR_APP_ID",
    "ClientSecret": "YOUR_CLIENT_SECRET",
    "SharePointSiteUrl": "YOUR_SHAREPOINT_URL",
    "EmailRecipient": "YOUR_EMAIL"
  }
}
```

### 3. Build the Project

```bash
dotnet build
```

Expected output:
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### 4. Run Locally

```bash
func start
```

Expected output:
```
Azure Functions Core Tools
Core Tools Version: 4.x.x
Function Runtime Version: 4.x.x

Functions:
    MCPostTimerFunction: timerTrigger

For detailed output, run func with --verbose flag.
```

### 5. Test the Function

The timer function runs on schedule, but you can test it manually:

#### Option A: Using Azure Portal (after deployment)
1. Navigate to your Function App
2. Click on "MCPostTimerFunction"
3. Click "Code + Test"
4. Click "Test/Run"

#### Option B: Using HTTP Trigger (add for testing)
Add a temporary HTTP trigger to test locally:

```csharp
[Function("MCPostTestTrigger")]
public async Task<HttpResponseData> RunTest(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
{
    await Run(new TimerInfo());
    var response = req.CreateResponse(HttpStatusCode.OK);
    await response.WriteStringAsync("Function executed successfully!");
    return response;
}
```

Then navigate to: `http://localhost:7071/api/MCPostTestTrigger`

## Verify It Works

Check the console output for:
```
[2024-XX-XX XX:XX:XX] Executing 'MCPostTimerFunction'
[2024-XX-XX XX:XX:XX] Fetching Message Center posts...
[2024-XX-XX XX:XX:XX] Retrieved X Message Center posts
[2024-XX-XX XX:XX:XX] Uploading to SharePoint...
[2024-XX-XX XX:XX:XX] Sending email notification...
[2024-XX-XX XX:XX:XX] Executed 'MCPostTimerFunction' (Succeeded)
```

## Common Issues

### Issue: "No such file or directory: local.settings.json"
**Solution**: Create the file from the template
```bash
cp local.settings.json.template local.settings.json
```

### Issue: "Authentication failed"
**Solution**: Verify your Azure AD app credentials
- Check Tenant ID is correct
- Verify Application ID
- Ensure Client Secret hasn't expired
- Confirm API permissions are granted

### Issue: "SharePoint site not found"
**Solution**: Verify SharePoint URL format
- Should be: `https://{tenant}.sharepoint.com/sites/{siteName}`
- Check you have access to the site

### Issue: "Mail.Send permission error"
**Solution**: Grant the required API permission
1. Go to Azure AD > App registrations > Your app
2. Click "API permissions"
3. Add "Mail.Send" application permission
4. Click "Grant admin consent"

## Next Steps

1. **Customize Configuration**: Edit `local.settings.json` for your needs
2. **Review Code**: Explore the Services and Models folders
3. **Deploy to Azure**: Follow [DEPLOYMENT.md](DEPLOYMENT.md)
4. **Set Up Monitoring**: Configure Application Insights

## Development Workflow

### Make Changes
1. Edit code in your preferred IDE
2. Build: `dotnet build`
3. Run locally: `func start`
4. Test your changes

### Add Features
1. Create new service in `Services/` folder
2. Register in `Program.cs`
3. Use in `MCPostTimerFunction.cs`

### Update Dependencies
```bash
dotnet add package PackageName --version X.X.X
dotnet restore
```

## Debugging

### Visual Studio
1. Open `MCPostProcessor.csproj`
2. Press F5 to start debugging
3. Set breakpoints in your code

### Visual Studio Code
1. Open the `MCPostProcessor` folder
2. Press F5 (or use Run and Debug panel)
3. Select ".NET Core" if prompted

## Testing

### Manual Test
```bash
# Build in release mode
dotnet build --configuration Release

# Check for warnings/errors
dotnet build --no-incremental
```

### Check Dependencies
```bash
# List all packages
dotnet list package

# Check for updates
dotnet list package --outdated
```

## Resources

- **Documentation**: See [README.md](README.md)
- **Deployment**: See [DEPLOYMENT.md](DEPLOYMENT.md)
- **Architecture**: See [ARCHITECTURE.md](ARCHITECTURE.md)
- **Azure Functions Docs**: https://docs.microsoft.com/azure/azure-functions/
- **Microsoft Graph Docs**: https://docs.microsoft.com/graph/

## Getting Help

If you encounter issues:
1. Check the [README.md](README.md#troubleshooting) troubleshooting section
2. Review Application Insights logs (if deployed)
3. Check Azure Function logs: `func azure functionapp logstream <app-name>`

## What's Next?

- ✅ **You're running locally!**
- ⬜ Deploy to Azure (see [DEPLOYMENT.md](DEPLOYMENT.md))
- ⬜ Set up monitoring
- ⬜ Configure alerts
- ⬜ Add to CI/CD pipeline

Happy coding! 🚀
