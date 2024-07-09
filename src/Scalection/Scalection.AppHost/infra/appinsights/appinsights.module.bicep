targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location

@description('')
param applicationType string = 'web'

@description('')
param kind string = 'web'

@description('')
param logAnalyticsWorkspaceId string


resource applicationInsightsComponent_J1qKzRVWQ 'Microsoft.Insights/components@2020-02-02' = {
  name: toLower(take('appinsights${uniqueString(resourceGroup().id)}', 24))
  location: location
  tags: {
    'aspire-resource-name': 'appinsights'
  }
  kind: kind
  properties: {
    Application_Type: applicationType
    WorkspaceResourceId: logAnalyticsWorkspaceId
  }
}

output appInsightsConnectionString string = applicationInsightsComponent_J1qKzRVWQ.properties.ConnectionString
