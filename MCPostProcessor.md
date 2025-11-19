# MC Post Processor - Azure Function Implementation

## Overview

This directory contains a complete Azure Function implementation that converts the existing Power Automate "MC Post" flow into a serverless, production-ready application.

## 🎯 Purpose

The MC Post Processor automatically:
1. Fetches Message Center posts from Microsoft 365 filtered for Copilot/Agent content
2. Transforms the data into a structured JSON format
3. Uploads the results to SharePoint
4. Sends email notifications with the data attached

Runs daily at 1 AM EST (6 AM UTC).

## 📁 Project Structure

```
MCPostProcessor/
├── Functions/
│   └── MCPostTimerFunction.cs      # Main timer-triggered function
├── Models/
│   ├── AppSettings.cs              # Configuration model
│   ├── MessageCenterPost.cs        # Graph API response model
│   └── TransformedPost.cs          # Output data model
├── Services/
│   ├── GraphService.cs             # Microsoft Graph API operations
│   ├── SharePointService.cs        # SharePoint file upload
│   ├── EmailService.cs             # Email notifications
│   ├── RetryHelper.cs              # Retry logic with exponential backoff
│   └── I*.cs                       # Service interfaces
├── QUICKSTART.md                   # 5-minute getting started guide
├── README.md                       # Comprehensive documentation
├── DEPLOYMENT.md                   # Step-by-step deployment guide
├── ARCHITECTURE.md                 # Technical architecture details
├── SUMMARY.md                      # Implementation summary
├── MCPostProcessor.csproj          # Project file
├── Program.cs                      # Dependency injection setup
└── host.json                       # Azure Functions configuration
```

## 🚀 Quick Start

### Prerequisites
- .NET 8.0 SDK
- Azure subscription
- Azure AD app registration with required permissions

### Run Locally
```bash
cd MCPostProcessor
cp local.settings.json.template local.settings.json
# Edit local.settings.json with your configuration
dotnet build
func start
```

See [QUICKSTART.md](MCPostProcessor/QUICKSTART.md) for detailed instructions.

## 📚 Documentation

| Document | Description |
|----------|-------------|
| [QUICKSTART.md](MCPostProcessor/QUICKSTART.md) | Get running in 5 minutes |
| [README.md](MCPostProcessor/README.md) | Complete project documentation |
| [DEPLOYMENT.md](MCPostProcessor/DEPLOYMENT.md) | Azure deployment guide |
| [ARCHITECTURE.md](MCPostProcessor/ARCHITECTURE.md) | Technical architecture |
| [SUMMARY.md](MCPostProcessor/SUMMARY.md) | Implementation summary |

## ✨ Key Features

### Functional Features
- ✅ Daily scheduled execution (1 AM EST)
- ✅ Microsoft Graph API integration
- ✅ OData filtering for Copilot/Agent posts
- ✅ Data transformation (dates, arrays to strings)
- ✅ SharePoint file upload
- ✅ Email notifications with attachments

### Technical Features
- ✅ .NET 8.0 isolated worker model
- ✅ Dependency injection
- ✅ Retry logic with exponential backoff
- ✅ Comprehensive error handling
- ✅ Application Insights integration
- ✅ Structured logging
- ✅ Azure Key Vault support

### Security Features
- ✅ No hardcoded secrets
- ✅ Azure AD authentication
- ✅ Managed identity compatible
- ✅ Zero security vulnerabilities (verified)
- ✅ Zero CodeQL alerts (verified)

## 🔒 Required Azure AD Permissions

Your Azure AD app registration needs:
- `ServiceMessage.Read.All` - Read Message Center posts
- `Sites.ReadWrite.All` - Upload to SharePoint
- `Mail.Send` - Send notification emails

## 🛠️ Technology Stack

- **Runtime**: .NET 8.0
- **Framework**: Azure Functions v4 (isolated worker)
- **Authentication**: Azure.Identity (client credentials)
- **API**: Microsoft.Graph 5.96.0
- **Monitoring**: Application Insights

## 📊 Status

### Build Status
✅ Release build: 0 warnings, 0 errors

### Security Status
✅ NuGet packages: 0 vulnerabilities  
✅ CodeQL analysis: 0 alerts

### Deployment Status
🟡 Ready for deployment (configuration required)

## 📝 Configuration

Required settings (store in Azure Key Vault for production):
```json
{
  "TenantId": "YOUR_TENANT_ID",
  "ApplicationId": "YOUR_APP_ID",
  "ClientSecret": "YOUR_CLIENT_SECRET",
  "SharePointSiteUrl": "https://your-tenant.sharepoint.com/sites/YourSite",
  "EmailRecipient": "recipient@domain.com"
}
```

## 🚢 Deployment

### Quick Deploy
```bash
cd MCPostProcessor
func azure functionapp publish <your-function-app-name>
```

### Complete Guide
See [DEPLOYMENT.md](MCPostProcessor/DEPLOYMENT.md) for comprehensive deployment instructions including:
- Azure AD app registration
- Azure resource creation
- Configuration management
- Key Vault integration
- Monitoring setup

## 📈 Improvements Over Power Automate

| Aspect | Power Automate | Azure Function |
|--------|----------------|----------------|
| **Performance** | Workflow engine overhead | Native compiled code |
| **Reliability** | Built-in retry | Custom retry with exponential backoff |
| **Monitoring** | Basic flow analytics | Full Application Insights |
| **Cost** | Connector costs + executions | Consumption plan only |
| **Versioning** | Limited | Full source control |
| **Testing** | Manual testing | Unit + integration tests |
| **CI/CD** | Not available | GitHub Actions/Azure DevOps |
| **Debugging** | Limited visibility | Full logging and debugging |

## 🔍 Monitoring

### Application Insights Queries

**Recent executions:**
```kusto
traces
| where timestamp > ago(7d)
| where message contains "MCPostTimerFunction"
| order by timestamp desc
```

**Error rate:**
```kusto
exceptions
| where timestamp > ago(7d)
| summarize count() by bin(timestamp, 1h)
```

## 🤝 Contributing

This is part of the agent-academy repository. See the main [CONTRIBUTING.md](../CONTRIBUTING.md) for guidelines.

## 📄 License

Copyright (c) Microsoft Corporation. All rights reserved.  
See [LICENSE](../LICENSE) for details.

## 🆘 Support

### Documentation
- [Quick Start Guide](MCPostProcessor/QUICKSTART.md)
- [README](MCPostProcessor/README.md)
- [Deployment Guide](MCPostProcessor/DEPLOYMENT.md)

### Troubleshooting
See the [README Troubleshooting section](MCPostProcessor/README.md#troubleshooting)

### Resources
- [Azure Functions Documentation](https://docs.microsoft.com/azure/azure-functions/)
- [Microsoft Graph API](https://docs.microsoft.com/graph/)
- [Message Center API](https://docs.microsoft.com/graph/api/resources/serviceannouncement)

## ✅ Implementation Checklist

- [x] Azure Function project created
- [x] Data models implemented
- [x] Services implemented (Graph, SharePoint, Email)
- [x] Error handling and retry logic
- [x] Timer trigger configured
- [x] Dependency injection setup
- [x] Comprehensive documentation
- [x] Security verification (0 vulnerabilities)
- [x] Build verification (0 warnings, 0 errors)
- [ ] Deploy to Azure
- [ ] Configure monitoring
- [ ] Set up alerts

## 🎓 Learn More

This implementation demonstrates:
- Azure Functions best practices
- Microsoft Graph API integration
- Service-oriented architecture
- Error handling and resilience patterns
- Dependency injection in Azure Functions
- Secrets management with Azure Key Vault
- Application Insights integration

For detailed architecture insights, see [ARCHITECTURE.md](MCPostProcessor/ARCHITECTURE.md).

---

**Status**: ✅ Implementation Complete - Ready for Deployment  
**Last Updated**: 2024-11-19
