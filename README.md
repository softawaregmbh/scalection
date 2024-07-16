# Scalection

This repository contains the source code for the blog series

## [Building a scalable web application with ASP.NET Core and Azure](https://softaware.at/codeaware/building-a-scalable-web-application-with-asp-net-core-and-azure-1/)

It contains a sample implementation of an electronic voting system as a showcase for how we can build scalable web applications using .NET and Azure.

Please refer to the blog posts for a detailed description of the project.

### Running the sample

The sample uses [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview) for simplified local development.

See the docs for [prerequesites](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/setup-tooling?tabs=windows&pivots=visual-studio) or additional information on how to run it [locally](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/networking-overview) or [deploy to Azure](https://learn.microsoft.com/en-us/dotnet/aspire/deployment/azure/aca-deployment).

### Migrations

EF Migrations can be executed via the [Migration Service](src/Scalection/Scalection.MigrationService)'s endpoints.

### Endpoints
The API Service contains an [.http file](src/Scalection/Scalection.ApiService/app.http) that shows how to use the various endpoints.
