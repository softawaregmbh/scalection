targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location

@description('')
param keyVaultName string


resource keyVault_IeF8jZvXV 'Microsoft.KeyVault/vaults@2022-07-01' existing = {
  name: keyVaultName
}

resource cosmosDBAccount_1Qjjjmm7K 'Microsoft.DocumentDB/databaseAccounts@2023-04-15' = {
  name: toLower(take('cosmosaccount${uniqueString(resourceGroup().id)}', 24))
  location: location
  tags: {
    'aspire-resource-name': 'cosmosaccount'
  }
  kind: 'GlobalDocumentDB'
  properties: {
    enableFreeTier: true
    databaseAccountOfferType: 'Standard'
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    locations: [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]
    capabilities: [
      {
        name: 'EnableServerless'
      }
    ]
  }
}

resource cosmosDBSqlDatabase_BymUovuDf 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2023-04-15' = {
  parent: cosmosDBAccount_1Qjjjmm7K
  name: 'cosmosdb'
  location: location
  properties: {
    resource: {
      id: 'cosmosdb'
    }
  }
}

resource keyVaultSecret_Ddsc3HjrA 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
  parent: keyVault_IeF8jZvXV
  name: 'connectionString'
  location: location
  properties: {
    value: 'AccountEndpoint=${cosmosDBAccount_1Qjjjmm7K.properties.documentEndpoint};AccountKey=${cosmosDBAccount_1Qjjjmm7K.listkeys(cosmosDBAccount_1Qjjjmm7K.apiVersion).primaryMasterKey}'
  }
}
