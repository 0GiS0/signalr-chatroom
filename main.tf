terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = ">= 2.26"
    }
  }

  backend "remote" {
    organization = "returngis"
    workspaces {
      name = "signalr-chatroom"
    }  
  }
}

provider "azurerm" {
  features {}
}

resource "random_pet" "service" {

}


#Resource Group
resource "azurerm_resource_group" "rg" {
  name     = random_pet.service.id
  location = var.location
}

#Azure SignalR
resource "azurerm_signalr_service" "signalr" {
  name                = random_pet.service.id
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name

  sku {
    name     = "Free_F1"
    capacity = 1
  }

  cors {
    allowed_origins = ["*"]
  }

  features {
    flag  = "ServiceMode"
    value = "Default"
  }

  features {
    flag  = "EnableConnectivityLogs"
    value = "False"
  }

  features {
    flag  = "EnableMessagingLogs"
    value = "False"
  }

}

#CosmosDB
resource "azurerm_cosmosdb_account" "cosmosdbaccount" {
  name                = random_pet.service.id
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  offer_type          = "Standard"
  kind                = "GlobalDocumentDB"

  consistency_policy {
    consistency_level       = "Session"
    max_interval_in_seconds = 10
    max_staleness_prefix    = 200
  }

  geo_location {
    location          = azurerm_resource_group.rg.location
    failover_priority = 0
  }

}

#CosmosDB - Database
resource "azurerm_cosmosdb_sql_database" "cosmosdb_db" {
  name                = var.cosmosdb_db
  resource_group_name = azurerm_resource_group.rg.name
  account_name        = azurerm_cosmosdb_account.cosmosdbaccount.name
  throughput          = 400
}

#CosmosDB - Container
resource "azurerm_cosmosdb_sql_container" "cosmosdb_container" {
  name                  = var.cosmosdb_container
  resource_group_name   = azurerm_resource_group.rg.name
  account_name          = azurerm_cosmosdb_account.cosmosdbaccount.name
  database_name         = azurerm_cosmosdb_sql_database.cosmosdb_db.name
  partition_key_path    = var.cosmosdb_partition_key
  partition_key_version = 1
  throughput            = 400
}


#Azure Search
resource "azurerm_search_service" "search" {
  name                = random_pet.service.id
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  sku                 = "standard"
}

#Content Moderator
resource "azurerm_cognitive_account" "content_moderator" {
  name                = "${random_pet.service.id}-mod"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  kind                = "ContentModerator"

  sku_name = "S0"
}

#LUIS
resource "azurerm_cognitive_account" "luis" {
  name                = "${random_pet.service.id}-luis-prediction"
  location            = var.luis_location
  resource_group_name = azurerm_resource_group.rg.name
  kind                = "LUIS"

  sku_name = "S0"
}

resource "azurerm_cognitive_account" "luis_authoring" {
  name                = "${random_pet.service.id}-luis-authoring"
  location            = var.luis_location
  resource_group_name = azurerm_resource_group.rg.name
  kind                = "LUIS.Authoring"

  sku_name = "F0"
}

output "luis_authoring_endpoint" {
  value = azurerm_cognitive_account.luis_authoring.endpoint
}

output "luis_authoring_key" {
  value = azurerm_cognitive_account.luis_authoring.primary_access_key
}


#Application Insights
resource "azurerm_application_insights" "appinsights" {
  name                = random_pet.service.id
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  application_type    = "web"
}

#Azure Storage for the static website (Chat)
resource "azurerm_storage_account" "staticweb" {
  name                     = replace(random_pet.service.id, "-", "")
  resource_group_name      = azurerm_resource_group.rg.name
  location                 = azurerm_resource_group.rg.location
  account_tier             = "Standard"
  account_replication_type = "GRS"
  account_kind             = "StorageV2"

  static_website {
    index_document = "index.html"
  }
}

output "storage" {
  value = azurerm_storage_account.staticweb.name
}

#App Service
#Plan
resource "azurerm_app_service_plan" "appserviceplan" {
  name                = random_pet.service.id
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name

  sku {
    tier = "Free"
    size = "F1"
  }
}

#Web App
resource "azurerm_app_service" "webapp" {
  name                = random_pet.service.id
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  app_service_plan_id = azurerm_app_service_plan.appserviceplan.id

  # site_config {
  #   cors {
  #     allowed_origins     = [azurerm_storage_account.staticweb.primary_web_endpoint]
  #     support_credentials = false
  #   }
  # }

  app_settings = {
    "Azure:SignalR:ConnectionString" = azurerm_signalr_service.signalr.primary_connection_string
    "CosmosDb:Account"               = azurerm_cosmosdb_account.cosmosdbaccount.endpoint
    "CosmosDb:Key"                   = azurerm_cosmosdb_account.cosmosdbaccount.primary_key
    "CosmosDb:DatabaseName"          = var.cosmosdb_db
    "CosmosDb:ContainerName"         = var.cosmosdb_container
    "LUIS:PredictionEndpoint"        = azurerm_cognitive_account.luis.endpoint    
    "LUIS:ApiKey"                    = azurerm_cognitive_account.luis.primary_access_key
    "APPINSIGHTS_INSTRUMENTATIONKEY" = azurerm_application_insights.appinsights.instrumentation_key
    "ContentModerator:Endpoint"      = azurerm_cognitive_account.content_moderator.endpoint
    "ContentModerator:ApiKey"        = azurerm_cognitive_account.content_moderator.primary_access_key
  }
}

output "app_service_name" {
  value = azurerm_app_service.webapp.name
}
