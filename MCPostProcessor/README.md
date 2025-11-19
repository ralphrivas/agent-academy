# MC Post Processor - Azure Function

This Azure Function converts the Power Automate "MC Post" flow into a serverless function that processes Microsoft Message Center posts.

## Overview

The function runs on a daily schedule (1 AM EST) and performs the following tasks:
1. Authenticates with Microsoft Graph API using client credentials
2. Fetches Message Center posts filtered for Copilot/Agent content from the last 90 days
3. Transforms the data into a structured JSON format
4. Saves the results to SharePoint
5. Sends an email notification with the data attached

## Project Structure

```
MCPostProcessor/
├── Models/
│   ├── AppSettings.cs           # Configuration settings model
│   ├── MessageCenterPost.cs     # Microsoft Graph API response models
│   └── TransformedPost.cs       # Transformed post data model
├── Services/
│   ├── IGraphService.cs         # Graph service interface
│   ├── GraphService.cs          # Microsoft Graph API operations
│   ├── ISharePointService.cs    # SharePoint service interface
│   ├── SharePointService.cs     # SharePoint upload operations
│   ├── IEmailService.cs         # Email service interface
│   └── EmailService.cs          # Email notification operations
├── Functions/
│   └── MCPostTimerFunction.cs   # Timer-triggered function
├── Program.cs                   # Dependency injection setup
├── host.json                    # Function host configuration
├── local.settings.json          # Local development settings
└── MCPostProcessor.csproj       # Project file
```

## Configuration

### Required Settings

Configure the following settings in `local.settings.json` for local development or in Azure Function App Settings for production:

```json
{
  "AppSettings": {
    "TenantId": "59d9af4c-c058-43ca-9193-440bc8c84da0",
    "ApplicationId": "1f1572f7-977c-4a35-8da0-e0f448ef12e8",
    "ClientSecret": "YOUR_CLIENT_SECRET_HERE",
    "SharePointSiteUrl": "https://m365cpi48088324.sharepoint.com/sites/Copilot2",
    "EmailRecipient": "ahurtado@microsoft.com",
    "SharePointLibraryName": "Shared Documents",
    "DaysToLookBack": 90
  }
}
```

### Azure AD App Registration

The application requires an Azure AD app registration with the following permissions:

**Microsoft Graph API Permissions:**
- `ServiceMessage.Read.All` - Read service messages
- `Sites.ReadWrite.All` - Upload files to SharePoint
- `Mail.Send` - Send emails

**Grant Type:** Client Credentials (Application permissions)

### Secure Configuration with Azure Key Vault (Recommended)

For production deployment, store sensitive values in Azure Key Vault:

1. Create an Azure Key Vault
2. Store the client secret as a Key Vault secret
3. Configure the Function App to reference Key Vault:
   ```
   ClientSecret: @Microsoft.KeyVault(SecretUri=https://{vault-name}.vault.azure.net/secrets/{secret-name})
   ```

## Timer Schedule

The function uses a NCRONTAB expression to run daily at 1 AM EST:

```
"0 0 6 * * *"
```

- Runs at 6:00 AM UTC (1:00 AM EST)
- Format: `{second} {minute} {hour} {day} {month} {day-of-week}`
- Note: Azure Functions use UTC time. Adjust for daylight saving time if needed.

## Data Flow

1. **Fetch Data**: Calls Microsoft Graph API endpoint:
   - `/beta/admin/serviceAnnouncement/messages`
   - Filter: `(contains(title, 'Copilot') or contains(title, 'Agent')) and (startDateTime ge {90 days ago})`

2. **Transform Data**: Converts API response to match Power Automate Select action output:
   - Formats dates to `dd-MM-yyyy` format
   - Converts arrays to comma-separated strings
   - Extracts body content

3. **Upload to SharePoint**: Saves JSON file to SharePoint document library:
   - File name format: `MCPosts_yyyy-MM-dd_HHmmss.json`
   - Uses Microsoft Graph API for file upload

4. **Send Email**: Sends notification email:
   - Subject: `Message Center Posts - yyyy-MM-dd`
   - Includes JSON file as attachment
   - Contains summary and SharePoint link

## Development

### Prerequisites

- .NET 8.0 SDK or later
- Azure Functions Core Tools (for local testing)
- Visual Studio 2022 or Visual Studio Code

### Build

```bash
cd MCPostProcessor
dotnet build
```

### Run Locally

```bash
cd MCPostProcessor
func start
```

Or use Visual Studio/VS Code debugger.

### Test Locally

To test the timer function manually without waiting for the schedule:

```bash
# Using HTTP trigger (if added) or
# Manually invoke in Azure Portal
```

## Deployment

### Using Azure Portal

1. Create an Azure Function App (Runtime: .NET 8, OS: Windows/Linux)
2. Configure Application Settings with the required values
3. Deploy using Visual Studio, VS Code, or Azure CLI

### Using Azure CLI

```bash
# Create resource group
az group create --name rg-mcpost --location eastus

# Create storage account
az storage account create --name stmcpost --location eastus --resource-group rg-mcpost --sku Standard_LRS

# Create function app
az functionapp create --resource-group rg-mcpost --consumption-plan-location eastus \
  --runtime dotnet-isolated --runtime-version 8 --functions-version 4 \
  --name func-mcpost --storage-account stmcpost

# Deploy
func azure functionapp publish func-mcpost
```

### Configure Application Settings

```bash
az functionapp config appsettings set --name func-mcpost --resource-group rg-mcpost \
  --settings \
    "AppSettings__TenantId=59d9af4c-c058-43ca-9193-440bc8c84da0" \
    "AppSettings__ApplicationId=1f1572f7-977c-4a35-8da0-e0f448ef12e8" \
    "AppSettings__ClientSecret=YOUR_SECRET" \
    "AppSettings__SharePointSiteUrl=https://m365cpi48088324.sharepoint.com/sites/Copilot2" \
    "AppSettings__EmailRecipient=ahurtado@microsoft.com" \
    "AppSettings__SharePointLibraryName=Shared Documents" \
    "AppSettings__DaysToLookBack=90"
```

## Monitoring

### Application Insights

The function is configured with Application Insights for monitoring:

- View logs in Azure Portal
- Set up alerts for failures
- Monitor execution times and performance

### Logs

Check logs in:
- Azure Portal > Function App > Monitor > Logs
- Application Insights > Logs
- Live Metrics

## Error Handling

The function includes comprehensive error handling:

- Retries for transient failures (via Graph SDK)
- Detailed logging at each step
- Exceptions are logged and re-thrown for Azure monitoring

## Security Best Practices

1. **Store secrets in Azure Key Vault**, not in configuration files
2. **Use managed identity** where possible instead of client secrets
3. **Limit API permissions** to only what's required
4. **Enable monitoring and alerting** for security events
5. **Regularly rotate client secrets**
6. **Review SharePoint and email permissions** regularly

## Troubleshooting

### Common Issues

**Authentication Failures:**
- Verify Azure AD app registration has required permissions
- Ensure admin consent is granted for application permissions
- Check client secret hasn't expired

**SharePoint Upload Failures:**
- Verify SharePoint site URL is correct
- Ensure app has Sites.ReadWrite.All permission
- Check document library name matches configuration

**Email Send Failures:**
- Verify Mail.Send permission is granted
- Check recipient email address is valid
- Ensure mailbox exists for the recipient

**No Posts Retrieved:**
- Check the filter criteria matches expected posts
- Verify the date range includes relevant posts
- Check Graph API permissions

## Performance Considerations

- Function uses connection pooling for HTTP clients
- Services are registered as Singletons for efficiency
- Large result sets are handled efficiently with streaming

## Cost Optimization

- Uses Azure Functions Consumption Plan (pay-per-execution)
- Runs once daily (minimal executions)
- Efficient Graph API queries with filters
- No persistent infrastructure costs

## Future Enhancements

Potential improvements:
- Add retry logic with exponential backoff
- Implement dead letter queue for failed operations
- Add metrics and custom telemetry
- Support multiple email recipients
- Add filtering options via configuration
- Implement incremental processing (only new posts)

## License

Copyright (c) Microsoft Corporation. All rights reserved.
