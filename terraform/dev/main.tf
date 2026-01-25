# ============================================
# TERRAFORM CONFIGURATION
# ============================================
terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.0"
    }
  }
}

# ============================================
# PROVIDER CONFIGURATION
# ============================================
provider "azurerm" {
  features {
    key_vault {
      purge_soft_delete_on_destroy = true
      recover_soft_deleted_key_vaults = true
    }
  }
}

# ============================================
# VARIABLES
# ============================================
variable "environment" {
  description = "Environment name (dev, staging, prod)"
  type        = string
  default     = "dev"
}

variable "location" {
  description = "Azure region"
  type        = string
  default     = "North Europe"
}

# ============================================
# DATA SOURCES
# ============================================
data "azurerm_client_config" "current" {}

# ============================================
# RANDOM RESOURCES
# ============================================
resource "random_string" "suffix" {
  length  = 6
  upper   = false
  special = false
}

resource "random_password" "db_password" {
  length  = 32
  special = true
  override_special = "!#$%&*()-_=+[]{}<>:?"
}

# ============================================
# RESOURCE GROUP
# ============================================
resource "azurerm_resource_group" "yogyn" {
  name     = "yogyn-${var.environment}-rg"
  location = var.location
  
  tags = {
    Environment = var.environment
    Project     = "Yogyn"
    ManagedBy   = "Terraform"
    Owner       = "Joanna"
  }
}

# ============================================
# KEY VAULT
# ============================================
resource "azurerm_key_vault" "yogyn" {
  name                = "yogyn-${var.environment}-kv-${random_string.suffix.result}"
  location            = azurerm_resource_group.yogyn.location
  resource_group_name = azurerm_resource_group.yogyn.name
  tenant_id           = data.azurerm_client_config.current.tenant_id
  sku_name            = "standard"
  
  # Allow Terraform to manage secrets
  enable_rbac_authorization = false
  
  # Access policy for current user
  access_policy {
    tenant_id = data.azurerm_client_config.current.tenant_id
    object_id = data.azurerm_client_config.current.object_id
    
    secret_permissions = [
      "Get",
      "List",
      "Set",
      "Delete",
      "Purge",
      "Recover"
    ]
  }
  
  tags = azurerm_resource_group.yogyn.tags
}

# ============================================
# KEY VAULT SECRET
# ============================================
resource "azurerm_key_vault_secret" "db_password" {
  name         = "db-admin-password"
  value        = random_password.db_password.result
  key_vault_id = azurerm_key_vault.yogyn.id
  
  depends_on = [azurerm_key_vault.yogyn]
}

# ============================================
# POSTGRESQL DATABASE
# ============================================
resource "azurerm_postgresql_flexible_server" "yogyn" {
  name                   = "yogyn-${var.environment}-${random_string.suffix.result}"
  resource_group_name    = azurerm_resource_group.yogyn.name
  location               = azurerm_resource_group.yogyn.location
  
  administrator_login    = "yogynadmin"
  administrator_password = random_password.db_password.result
  
  sku_name   = "B_Standard_B1ms"
  storage_mb = 32768
  version    = "15"
  
  backup_retention_days        = 7
  geo_redundant_backup_enabled = false
  
  tags = azurerm_resource_group.yogyn.tags
}

# ============================================
# DATABASE
# ============================================
resource "azurerm_postgresql_flexible_server_database" "yogyn" {
  name      = "yogyn"
  server_id = azurerm_postgresql_flexible_server.yogyn.id
  charset   = "UTF8"
  collation = "en_US.utf8"
}

# ============================================
# FIREWALL RULE
# ============================================
resource "azurerm_postgresql_flexible_server_firewall_rule" "allow_azure_services" {
  name             = "AllowAzureServices"
  server_id        = azurerm_postgresql_flexible_server.yogyn.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}

resource "azurerm_postgresql_flexible_server_firewall_rule" "allow_dev_ip" {
  name             = "AllowDevIP"
  server_id        = azurerm_postgresql_flexible_server.yogyn.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "255.255.255.255"
}

# ============================================
# KEY VAULT SECRET
# ============================================
resource "azurerm_key_vault_secret" "db_connection_string" {
  name         = "db-connection-string"
  value        = "Host=${azurerm_postgresql_flexible_server.yogyn.fqdn};Database=yogyn;Username=yogynadmin;Password=${random_password.db_password.result};SSL Mode=Require"
  key_vault_id = azurerm_key_vault.yogyn.id
  
  depends_on = [
    azurerm_key_vault.yogyn,
    azurerm_postgresql_flexible_server.yogyn
  ]
}

# ============================================
# OUTPUTS
# ============================================
output "resource_group_name" {
  value       = azurerm_resource_group.yogyn.name
  description = "The name of the resource group"
}

output "key_vault_name" {
  value       = azurerm_key_vault.yogyn.name
  description = "The name of the Key Vault"
}

output "key_vault_uri" {
  value       = azurerm_key_vault.yogyn.vault_uri
  description = "The URI of the Key Vault"
}

output "db_host" {
  value       = azurerm_postgresql_flexible_server.yogyn.fqdn
  description = "PostgreSQL server hostname"
}

output "db_password_secret_name" {
  value       = azurerm_key_vault_secret.db_password.name
  description = "Name of the Key Vault secret containing DB password"
}

output "db_connection_string_secret_name" {
  value       = azurerm_key_vault_secret.db_connection_string.name
  description = "Name of the Key Vault secret containing connection string"
}

output "db_connection_string_for_local_dev" {
  value       = azurerm_key_vault_secret.db_connection_string.value
  sensitive   = true
  description = "Use 'terraform output -raw db_connection_string_for_local_dev' to get connection string"
}