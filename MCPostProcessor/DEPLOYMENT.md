# Deployment Guide - MC Post Processor Azure Function

This guide provides step-by-step instructions for deploying the MC Post Processor Azure Function to Azure.

## Prerequisites

- Azure Subscription
- Azure CLI installed locally
- .NET 8.0 SDK installed
- Azure Functions Core Tools v4
- Access to Azure AD with permissions to create app registrations
- Access to SharePoint site for file uploads

## Step 1: Azure AD App Registration

### Create App Registration

1. Navigate to Azure Portal > Azure Active Directory > App registrations
2. Click "New registration"
3. Enter name: "MC Post Processor Function"
4. Select "Accounts in this organizational directory only"
5. Click "Register"

### Configure API Permissions

1. Go to "API permissions"
2. Click "Add a permission"
3. Select "Microsoft Graph"
4. Select "Application permissions"
5. Add the following permissions:
   - `ServiceMessage.Read.All`
   - `Sites.ReadWrite.All`
   - `Mail.Send`
6. Click "Grant admin consent" (requires Global Admin)

### Create Client Secret

1. Go to "Certificates & secrets"
2. Click "New client secret"
3. Add description: "MC Post Processor Secret"
4. Select expiration (recommended: 6 months to 1 year)
5. Click "Add"
6. **IMPORTANT**: Copy the secret value immediately (you won't see it again)

### Record Application Details

Note the following values from the "Overview" page:
- **Application (client) ID**: e.g., `1f1572f7-977c-4a35-8da0-e0f448ef12e8`
- **Directory (tenant) ID**: e.g., `59d9af4c-c058-43ca-9193-440bc8c84da0`
- **Client Secret**: The value you copied in the previous step

## Step 2: Azure Resources Setup

### Option A: Using Azure Portal

#### Create Resource Group

1. Navigate to Azure Portal > Resource groups
2. Click "Create"
3. Enter details:
   - Subscription: Select your subscription
   - Resource group: `rg-mcpost-prod`
   - Region: `East US` (or your preferred region)
4. Click "Review + create" > "Create"

#### Create Storage Account

1. Navigate to Azure Portal > Storage accounts
2. Click "Create"
3. Enter details:
   - Resource group: `rg-mcpost-prod`
   - Storage account name: `stmcpostprod` (must be globally unique)
   - Region: Same as resource group
   - Performance: Standard
   - Redundancy: LRS (or your preference)
4. Click "Review + create" > "Create"

#### Create Function App

1. Navigate to Azure Portal > Function Apps
2. Click "Create"
3. Enter details:
   - **Basics:**
     - Resource group: `rg-mcpost-prod`
     - Function App name: `func-mcpost-prod` (must be globally unique)
     - Runtime stack: `.NET`
     - Version: `8 (LTS), isolated worker model`
     - Region: Same as resource group
     - Operating System: `Windows` or `Linux`
     - Plan type: `Consumption (Serverless)`
   - **Storage:**
     - Storage account: `stmcpostprod` (created above)
   - **Networking:**
     - Enable public access: `On` (or configure as needed)
   - **Monitoring:**
     - Enable Application Insights: `Yes`
     - Application Insights: Create new or select existing
4. Click "Review + create" > "Create"

### Option B: Using Azure CLI

```bash
# Login to Azure
az login

# Set variables
RESOURCE_GROUP="rg-mcpost-prod"
LOCATION="eastus"
STORAGE_ACCOUNT="stmcpostprod"
FUNCTION_APP="func-mcpost-prod"
APP_INSIGHTS="ai-mcpost-prod"

# Create resource group
az group create --name $RESOURCE_GROUP --location $LOCATION

# Create storage account
az storage account create \
  --name $STORAGE_ACCOUNT \
  --location $LOCATION \
  --resource-group $RESOURCE_GROUP \
  --sku Standard_LRS

# Create Application Insights
az monitor app-insights component create \
  --app $APP_INSIGHTS \
  --location $LOCATION \
  --resource-group $RESOURCE_GROUP

# Get Application Insights connection string
AI_CONNECTION=$(az monitor app-insights component show \
  --app $APP_INSIGHTS \
  --resource-group $RESOURCE_GROUP \
  --query connectionString -o tsv)

# Create function app
az functionapp create \
  --resource-group $RESOURCE_GROUP \
  --consumption-plan-location $LOCATION \
  --runtime dotnet-isolated \
  --runtime-version 8 \
  --functions-version 4 \
  --name $FUNCTION_APP \
  --storage-account $STORAGE_ACCOUNT \
  --app-insights-key "$AI_CONNECTION"
```

## Step 3: Configure Application Settings

### Using Azure Portal

1. Navigate to Function App > Configuration
2. Click "New application setting" for each setting:

| Name | Value | Example |
|------|-------|---------|
| `AppSettings__TenantId` | Your Azure AD Tenant ID | `59d9af4c-c058-43ca-9193-440bc8c84da0` |
| `AppSettings__ApplicationId` | Your App Registration Client ID | `1f1572f7-977c-4a35-8da0-e0f448ef12e8` |
| `AppSettings__ClientSecret` | Your Client Secret | `your-secret-value` |
| `AppSettings__SharePointSiteUrl` | SharePoint site URL | `https://m365cpi48088324.sharepoint.com/sites/Copilot2` |
| `AppSettings__EmailRecipient` | Email address for notifications | `ahurtado@microsoft.com` |
| `AppSettings__SharePointLibraryName` | Document library name | `Shared Documents` |
| `AppSettings__DaysToLookBack` | Number of days to query | `90` |

3. Click "Save"

### Using Azure CLI

```bash
# Set your values
TENANT_ID="59d9af4c-c058-43ca-9193-440bc8c84da0"
APP_ID="1f1572f7-977c-4a35-8da0-e0f448ef12e8"
CLIENT_SECRET="your-secret-value"
SHAREPOINT_URL="https://m365cpi48088324.sharepoint.com/sites/Copilot2"
EMAIL_RECIPIENT="ahurtado@microsoft.com"

# Configure settings
az functionapp config appsettings set \
  --name $FUNCTION_APP \
  --resource-group $RESOURCE_GROUP \
  --settings \
    "AppSettings__TenantId=$TENANT_ID" \
    "AppSettings__ApplicationId=$APP_ID" \
    "AppSettings__ClientSecret=$CLIENT_SECRET" \
    "AppSettings__SharePointSiteUrl=$SHAREPOINT_URL" \
    "AppSettings__EmailRecipient=$EMAIL_RECIPIENT" \
    "AppSettings__SharePointLibraryName=Shared Documents" \
    "AppSettings__DaysToLookBack=90"
```

## Step 4: Deploy the Function

### Option A: Using Visual Studio

1. Right-click the MCPostProcessor project
2. Select "Publish"
3. Choose "Azure" as target
4. Select "Azure Function App (Windows)" or "Azure Function App (Linux)"
5. Sign in to Azure
6. Select your function app: `func-mcpost-prod`
7. Click "Publish"

### Option B: Using VS Code

1. Install Azure Functions extension
2. Open the MCPostProcessor folder
3. Click the Azure icon in the sidebar
4. Sign in to Azure
5. Expand your subscription > Function Apps
6. Right-click `func-mcpost-prod`
7. Select "Deploy to Function App"

### Option C: Using Azure Functions Core Tools

```bash
# Navigate to project directory
cd MCPostProcessor

# Build the project
dotnet build --configuration Release

# Deploy to Azure
func azure functionapp publish func-mcpost-prod
```

### Option D: Using Azure CLI (ZIP Deploy)

```bash
# Navigate to project directory
cd MCPostProcessor

# Build and publish
dotnet publish --configuration Release --output ./publish

# Create ZIP file
cd publish
zip -r ../deploy.zip .
cd ..

# Deploy to Azure
az functionapp deployment source config-zip \
  --resource-group $RESOURCE_GROUP \
  --name $FUNCTION_APP \
  --src deploy.zip
```

## Step 5: Secure Configuration with Azure Key Vault (Recommended)

### Create Key Vault

```bash
KEY_VAULT="kv-mcpost-prod"

# Create Key Vault
az keyvault create \
  --name $KEY_VAULT \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION
```

### Enable Managed Identity for Function App

```bash
# Enable system-assigned managed identity
az functionapp identity assign \
  --name $FUNCTION_APP \
  --resource-group $RESOURCE_GROUP

# Get the principal ID
PRINCIPAL_ID=$(az functionapp identity show \
  --name $FUNCTION_APP \
  --resource-group $RESOURCE_GROUP \
  --query principalId -o tsv)
```

### Grant Key Vault Access

```bash
# Grant the Function App access to Key Vault secrets
az keyvault set-policy \
  --name $KEY_VAULT \
  --object-id $PRINCIPAL_ID \
  --secret-permissions get list
```

### Store Secret in Key Vault

```bash
# Add client secret to Key Vault
az keyvault secret set \
  --vault-name $KEY_VAULT \
  --name "MCPostClientSecret" \
  --value "your-secret-value"

# Get the secret URI
SECRET_URI=$(az keyvault secret show \
  --vault-name $KEY_VAULT \
  --name "MCPostClientSecret" \
  --query id -o tsv)
```

### Update Function App Setting

```bash
# Update the ClientSecret setting to reference Key Vault
az functionapp config appsettings set \
  --name $FUNCTION_APP \
  --resource-group $RESOURCE_GROUP \
  --settings "AppSettings__ClientSecret=@Microsoft.KeyVault(SecretUri=$SECRET_URI)"
```

## Step 6: Verify Deployment

### Check Function Status

1. Navigate to Azure Portal > Function App
2. Go to "Functions"
3. Verify "MCPostTimerFunction" is listed
4. Click on the function
5. Check the "Monitor" tab for execution logs

### Test Manually

1. In the function details, click "Code + Test"
2. Click "Test/Run"
3. Click "Run" to trigger manually
4. Check the output logs

### Monitor Execution

1. Navigate to Function App > Application Insights
2. Go to "Logs"
3. Run query to see recent executions:
   ```kusto
   traces
   | where timestamp > ago(1h)
   | where message contains "MCPostTimerFunction"
   | order by timestamp desc
   ```

## Step 7: Set Up Alerts (Optional)

### Failure Alert

```bash
# Create action group for notifications
az monitor action-group create \
  --name "MCPost-Alerts" \
  --resource-group $RESOURCE_GROUP \
  --short-name "MCPost" \
  --email-receiver "Admin" "$EMAIL_RECIPIENT"

# Create alert for function failures
az monitor metrics alert create \
  --name "MCPost-Function-Failures" \
  --resource-group $RESOURCE_GROUP \
  --scopes "/subscriptions/{subscription-id}/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.Web/sites/$FUNCTION_APP" \
  --condition "count failedRequestCount > 0" \
  --description "Alert when MC Post function fails" \
  --evaluation-frequency 5m \
  --window-size 5m \
  --action "MCPost-Alerts"
```

## Step 8: Schedule Verification

The function is configured to run at 1 AM EST (6 AM UTC) daily.

### Verify Timer Schedule

1. Navigate to Function App > MCPostTimerFunction
2. Click "Integration"
3. Check the timer trigger shows: `0 0 6 * * *`

### Adjust for Daylight Saving Time (if needed)

To run at 1 AM EST year-round:
- Winter (EST): `0 0 6 * * *` (6 AM UTC)
- Summer (EDT): `0 0 5 * * *` (5 AM UTC)

Consider creating two functions or using logic to adjust based on the time of year.

## Troubleshooting

### Function Not Executing

1. Check Application Insights logs
2. Verify all app settings are configured correctly
3. Check Azure AD app permissions are granted
4. Ensure the function app is started

### Authentication Errors

1. Verify tenant ID and application ID
2. Check client secret hasn't expired
3. Confirm API permissions are granted with admin consent
4. Test Graph API access using Graph Explorer

### SharePoint Upload Failures

1. Verify SharePoint URL format
2. Check Sites.ReadWrite.All permission
3. Ensure document library exists
4. Test access to the site

### Email Send Failures

1. Verify Mail.Send permission
2. Check recipient email address
3. Ensure mailbox exists for the user
4. Review Graph API logs

## Maintenance

### Rotate Client Secret

1. Create new client secret in Azure AD
2. Update Key Vault secret or app setting
3. Test the function
4. Delete old secret after verification

### Update Function Code

1. Make code changes locally
2. Test thoroughly
3. Deploy using preferred method
4. Monitor for errors

### Monitor Costs

1. Navigate to Azure Portal > Cost Management
2. Filter by resource group: `rg-mcpost-prod`
3. Set up budget alerts if needed

## Rollback

If issues occur after deployment:

### Using Azure Portal

1. Navigate to Function App > Deployment Center
2. Go to "Logs"
3. Find previous successful deployment
4. Click "Redeploy"

### Using Azure CLI

```bash
# List deployments
az functionapp deployment list \
  --name $FUNCTION_APP \
  --resource-group $RESOURCE_GROUP

# Redeploy specific version
az functionapp deployment source sync \
  --name $FUNCTION_APP \
  --resource-group $RESOURCE_GROUP
```

## Security Checklist

- [ ] Client secret stored in Key Vault (not app settings)
- [ ] Managed identity configured for Key Vault access
- [ ] API permissions granted with minimal scope
- [ ] Application Insights monitoring enabled
- [ ] Alerts configured for failures
- [ ] Network security configured (if required)
- [ ] Client secret expiration monitored
- [ ] Regular security reviews scheduled

## Next Steps

1. Set up automated deployment (CI/CD) using GitHub Actions or Azure DevOps
2. Configure backup and disaster recovery
3. Implement additional monitoring and alerting
4. Document runbook procedures
5. Schedule regular reviews of Graph API permissions

## Support

For issues or questions:
- Review Application Insights logs
- Check Azure Function documentation: https://docs.microsoft.com/azure/azure-functions/
- Check Microsoft Graph documentation: https://docs.microsoft.com/graph/
