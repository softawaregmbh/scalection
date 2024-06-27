targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location

@description('')
param principalId string

@description('')
param principalName string


resource sqlServer_YFcCarAEq 'Microsoft.Sql/servers@2020-11-01-preview' = {
  name: toLower(take('sqlserver${uniqueString(resourceGroup().id)}', 24))
  location: location
  tags: {
    'aspire-resource-name': 'sqlserver'
  }
  properties: {
    version: '12.0'
    publicNetworkAccess: 'Enabled'
    administrators: {
      administratorType: 'ActiveDirectory'
      login: principalName
      sid: principalId
      tenantId: subscription().tenantId
      azureADOnlyAuthentication: true
    }
  }
}

resource sqlFirewallRule_9ocemjyMQ 'Microsoft.Sql/servers/firewallRules@2020-11-01-preview' = {
  parent: sqlServer_YFcCarAEq
  name: 'AllowAllAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource sqlDatabase_UE3hbDaNi 'Microsoft.Sql/servers/databases@2020-11-01-preview' = {
  parent: sqlServer_YFcCarAEq
  name: 'sqldb'
  location: location
  sku: {
    name: 'GP_S_Gen5'
    tier: 'GeneralPurpose'
    family: 'Gen5'
    capacity: 4
  }
  properties: {
      autoPauseDelay: 60
  }
}

resource backupLongTermRetentionPolicy 'Microsoft.Sql/servers/databases/backupLongTermRetentionPolicies@2023-08-01-preview' = {
  name: 'default'
  parent: sqlDatabase_UE3hbDaNi
  properties: {    
    monthlyRetention: 'PT0S'
    weeklyRetention: 'PT0S'
    yearlyRetention: 'PT0S'
    weekOfYear: 1
  }
}

resource backupShortTermRetentionPolicy 'Microsoft.Sql/servers/databases/backupShortTermRetentionPolicies@2023-08-01-preview' = {
  name: 'default'
  parent: sqlDatabase_UE3hbDaNi
  properties: {    
    retentionDays: 1
    diffBackupIntervalInHours: 24
  }
}

output sqlServerFqdn string = sqlServer_YFcCarAEq.properties.fullyQualifiedDomainName
