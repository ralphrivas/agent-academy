# Architecture Documentation - MC Post Processor

## Overview

This document describes the architecture, design decisions, and implementation details of the MC Post Processor Azure Function.

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                      Azure Function App                          │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │            MCPostTimerFunction (Timer Trigger)             │ │
│  │                   Runs Daily at 1 AM EST                   │ │
│  └────────────────────────────────────────────────────────────┘ │
│                              │                                   │
│          ┌───────────────────┼───────────────────┐              │
│          ▼                   ▼                   ▼               │
│  ┌──────────────┐   ┌──────────────┐   ┌──────────────┐        │
│  │ GraphService │   │SharePoint    │   │EmailService  │        │
│  │              │   │Service       │   │              │        │
│  └──────────────┘   └──────────────┘   └──────────────┘        │
└─────────────────────────────────────────────────────────────────┘
           │                   │                   │
           │                   │                   │
           ▼                   ▼                   ▼
┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐
│ Microsoft Graph  │  │ Microsoft Graph  │  │ Microsoft Graph  │
│   API (Service   │  │   API (Sites)    │  │   API (Mail)     │
│   Announcement)  │  │                  │  │                  │
└──────────────────┘  └──────────────────┘  └──────────────────┘
```

## Components

### 1. MCPostTimerFunction

**Purpose**: Main orchestrator for the Message Center post processing workflow.

**Trigger**: Timer trigger using NCRONTAB expression `0 0 6 * * *` (6 AM UTC = 1 AM EST)

**Responsibilities**:
- Coordinates the execution flow
- Handles errors and logging
- Ensures all steps complete successfully

**Workflow**:
1. Fetch posts from Microsoft Graph
2. Transform data to desired format
3. Upload JSON to SharePoint
4. Send email notification

### 2. GraphService

**Purpose**: Handles all Microsoft Graph API interactions for Message Center posts.

**Key Methods**:
- `GetMessageCenterPostsAsync()`: Fetches filtered posts
- `TransformPosts()`: Converts posts to output format

**Features**:
- Client credentials authentication
- OData filtering for Copilot/Agent posts
- Retry logic with exponential backoff
- Date formatting (dd-MM-yyyy)

**API Endpoint**: `/beta/admin/serviceAnnouncement/messages`

**Filter Criteria**:
```
(contains(title, 'Copilot') or contains(title, 'Agent')) 
and (startDateTime ge {90 days ago})
```

### 3. SharePointService

**Purpose**: Uploads JSON files to SharePoint document library.

**Key Methods**:
- `UploadJsonToSharePointAsync()`: Uploads JSON to SharePoint

**Features**:
- Site and drive discovery via Graph API
- File upload using Microsoft Graph
- Retry logic for transient failures
- Returns SharePoint URL of uploaded file

**Implementation Details**:
- Uses Microsoft Graph Sites and Drives API
- Supports custom document library names
- Generates timestamped filenames

### 4. EmailService

**Purpose**: Sends notification emails with JSON attachments.

**Key Methods**:
- `SendNotificationEmailAsync()`: Sends email with attachment

**Features**:
- HTML email body formatting
- JSON file attachment
- Retry logic for delivery issues
- Links to SharePoint file

**Email Content**:
- Subject: "Message Center Posts - {date}"
- Body: HTML with post count and links
- Attachment: JSON file with all posts

### 5. RetryHelper

**Purpose**: Provides reusable retry logic with exponential backoff.

**Features**:
- Configurable retry attempts
- Exponential backoff (1s, 2s, 4s, 8s, up to 30s)
- Transient error detection
- Comprehensive logging

**Retry Criteria**:
- HTTP request exceptions
- Timeout exceptions
- 429 (Too Many Requests)
- 503 (Service Unavailable)
- 504 (Gateway Timeout)

## Data Models

### AppSettings

Configuration model for application settings.

**Properties**:
- `TenantId`: Azure AD tenant identifier
- `ApplicationId`: App registration client ID
- `ClientSecret`: Client secret for authentication
- `SharePointSiteUrl`: Target SharePoint site
- `EmailRecipient`: Notification recipient
- `SharePointLibraryName`: Document library name
- `DaysToLookBack`: Query time range

### MessageCenterPost

Raw data model matching Microsoft Graph API response.

**Key Properties**:
- `Id`, `Title`: Identifiers
- `StartDateTime`, `EndDateTime`: Date range
- `Category`, `Severity`: Classification
- `Services`, `Tags`: Arrays
- `Body`: Message content

### TransformedPost

Output data model matching Power Automate Select action.

**Transformations**:
- Dates formatted as `dd-MM-yyyy`
- Arrays converted to comma-separated strings
- Body content extracted from HTML

## Design Decisions

### 1. Dependency Injection

**Decision**: Use Microsoft.Extensions.DependencyInjection

**Rationale**:
- Built-in Azure Functions support
- Enables testability
- Supports singleton services for efficiency
- Allows easy mocking for unit tests

### 2. Retry Logic

**Decision**: Implement custom retry helper with exponential backoff

**Rationale**:
- Microsoft Graph can have transient failures
- Network issues are common in cloud environments
- Exponential backoff prevents overwhelming services
- Configurable for different scenarios

### 3. Client Credentials Flow

**Decision**: Use OAuth2 client credentials for authentication

**Rationale**:
- No user interaction required
- Suitable for daemon/service applications
- Application permissions for service accounts
- Secure with managed identities

### 4. Singleton Service Registration

**Decision**: Register services as singletons

**Rationale**:
- Graph client can be reused
- Reduces authentication overhead
- Improves performance
- Safe for stateless services

### 5. Separate Services

**Decision**: Separate concerns into distinct services

**Rationale**:
- Single Responsibility Principle
- Easier to test and maintain
- Can be replaced independently
- Clear separation of concerns

## Security Considerations

### Authentication

- Uses Azure AD application permissions
- Client credentials stored securely
- Supports Azure Key Vault integration
- Managed identity compatible

### Secrets Management

**Recommendations**:
1. Store client secret in Azure Key Vault
2. Use managed identity for Key Vault access
3. Rotate secrets regularly
4. Never commit secrets to source control

### Permissions

**Required Graph API Permissions**:
- `ServiceMessage.Read.All`: Read Message Center posts
- `Sites.ReadWrite.All`: Upload to SharePoint
- `Mail.Send`: Send notification emails

**Principle of Least Privilege**:
- Only request necessary permissions
- Use application permissions (not delegated)
- Regular permission audits

### Network Security

**Options**:
- VNet integration for private connectivity
- Private endpoints for services
- IP restrictions on Function App
- Managed identity for authentication

## Performance Considerations

### Optimization Strategies

1. **Connection Pooling**: Graph client reused across invocations
2. **Batch Processing**: Single API call for all posts
3. **Efficient Serialization**: System.Text.Json for performance
4. **Streaming**: MemoryStream for file uploads
5. **Async/Await**: Non-blocking I/O operations

### Scalability

- Consumption plan scales automatically
- Stateless design supports horizontal scaling
- No in-memory state between executions
- Idempotent operations

### Cost Optimization

- Runs once daily (minimal executions)
- Consumption plan charges per execution
- No always-on infrastructure
- Efficient Graph API queries with filters

## Error Handling

### Strategy

1. **Try-Catch**: Each service method wrapped in try-catch
2. **Retry Logic**: Automatic retry for transient errors
3. **Logging**: Comprehensive error logging
4. **Fail Fast**: Stop on critical errors
5. **Graceful Degradation**: Continue on non-critical errors

### Error Scenarios

| Error Type | Handling | Retry | Impact |
|------------|----------|-------|--------|
| Authentication failure | Log and fail | No | Critical - stops execution |
| Transient HTTP error | Log and retry | Yes (3x) | Recoverable |
| No posts found | Log warning | No | Normal - complete successfully |
| SharePoint upload failure | Log and retry | Yes (3x) | Partial - email may still send |
| Email send failure | Log and retry | Yes (3x) | Partial - data saved to SharePoint |

## Monitoring and Observability

### Application Insights Integration

**Metrics**:
- Execution duration
- Success/failure rate
- Retry attempts
- Post counts

**Logs**:
- Info: Normal operations
- Warning: Retries and edge cases
- Error: Failures and exceptions

**Queries** (Kusto):
```kusto
// Recent executions
traces
| where timestamp > ago(7d)
| where message contains "MCPostTimerFunction"
| summarize count() by bin(timestamp, 1d)

// Error rate
exceptions
| where timestamp > ago(7d)
| summarize errors = count() by bin(timestamp, 1h)

// Performance
requests
| where name == "MCPostTimerFunction"
| summarize avg(duration) by bin(timestamp, 1d)
```

### Alerts

**Recommended Alerts**:
1. Function failure (any execution fails)
2. High retry rate (>50% of operations retry)
3. No posts found for 7 consecutive days
4. Execution duration >5 minutes

## Testing Strategy

### Unit Testing

**Components to Test**:
- Data transformation logic
- Retry helper logic
- Configuration validation
- Date formatting

**Mocking**:
- Mock Graph client
- Mock logger
- Mock configuration

### Integration Testing

**Scenarios**:
- End-to-end workflow
- Graph API integration
- SharePoint upload
- Email sending

**Test Environment**:
- Separate Azure AD app registration
- Test SharePoint site
- Test email recipient

### Manual Testing

**Steps**:
1. Deploy to test environment
2. Trigger function manually
3. Verify posts retrieved
4. Check SharePoint file
5. Confirm email received
6. Review Application Insights

## Deployment

### Environments

1. **Development**: Local development with emulators
2. **Test**: Azure test environment
3. **Production**: Production Azure environment

### CI/CD Pipeline

**Recommended Flow**:
```
Code Commit → Build → Unit Tests → Deploy to Test → 
Integration Tests → Manual Approval → Deploy to Prod
```

**Tools**:
- GitHub Actions
- Azure DevOps
- Azure CLI

### Configuration Management

**Per Environment**:
- Application settings
- Connection strings
- Key Vault references
- Function app settings

## Future Enhancements

### Potential Improvements

1. **Incremental Processing**:
   - Track last processed post
   - Only fetch new posts
   - Reduce API calls

2. **Dead Letter Queue**:
   - Store failed operations
   - Retry later
   - Manual intervention

3. **Multiple Recipients**:
   - Support distribution lists
   - Per-recipient customization
   - Delivery reports

4. **Advanced Filtering**:
   - Configurable filter criteria
   - Multiple filter sets
   - Category-based filtering

5. **Data Enrichment**:
   - Additional metadata
   - Related posts
   - Historical analysis

6. **Reporting Dashboard**:
   - Power BI integration
   - Trend analysis
   - Custom visualizations

## Maintenance

### Regular Tasks

1. **Monthly**:
   - Review Application Insights metrics
   - Check for Graph API updates
   - Monitor cost trends

2. **Quarterly**:
   - Rotate client secrets
   - Review permissions
   - Update dependencies

3. **Annually**:
   - Full security audit
   - Architecture review
   - Performance optimization

### Troubleshooting Guide

See [README.md](README.md#troubleshooting) for common issues and solutions.

## References

- [Azure Functions Documentation](https://docs.microsoft.com/azure/azure-functions/)
- [Microsoft Graph API](https://docs.microsoft.com/graph/)
- [Message Center API Reference](https://docs.microsoft.com/graph/api/resources/serviceannouncement)
- [Azure AD App Permissions](https://docs.microsoft.com/azure/active-directory/develop/v2-permissions-and-consent)
