locals {
  postgres_admin_username = "bettermeadmin"

  /**
    Replace the placeholder with your public IP before apply if you need
    to reach the server from a workstation.
  */
  postgres_firewall_rules = {
    Allow-Azure-Services = "0.0.0.0"
    # Allow-Dev-Home = "x.x.x.x"
  }
}

resource "azurerm_postgresql_flexible_server" "core" {
  name                = "psql-${local.resource_suffix}-core"
  resource_group_name = module.app_group.resource_group_name
  location            = module.app_group.location
  version             = "16"

  administrator_login    = azurerm_key_vault_secret.postgresql_username.value
  administrator_password = azurerm_key_vault_secret.postgresql_password.value

  auto_grow_enabled             = true
  storage_mb                    = 32768
  zone                          = "1"
  sku_name                      = "B_Standard_B1ms"
  backup_retention_days         = 7
  geo_redundant_backup_enabled  = false
  public_network_access_enabled = true

  tags = local.common_tags

  lifecycle {
    ignore_changes = [
      zone,
      storage_mb
    ]
  }
}

resource "azurerm_postgresql_flexible_server_database" "betterme" {
  name      = "betterme"
  server_id = azurerm_postgresql_flexible_server.core.id
  charset   = "UTF8"
  collation = "en_US.utf8"
}

resource "azurerm_postgresql_flexible_server_configuration" "pg_stat_statements" {
  name      = "azure.extensions"
  server_id = azurerm_postgresql_flexible_server.core.id
  value     = "PG_STAT_STATEMENTS"
}

resource "random_password" "postgresql" {
  length           = 16
  special          = true
  override_special = "!#$%&*()-_=+[]{}<>:?"
  min_lower        = 1
  min_upper        = 1
  min_numeric      = 1
  min_special      = 1
}

resource "azurerm_key_vault_secret" "postgresql_password" {
  name         = "psql-${local.resource_suffix}-core-admin-password"
  value        = random_password.postgresql.result
  key_vault_id = module.app_group.key_vault_id
}

resource "azurerm_key_vault_secret" "postgresql_username" {
  name         = "psql-${local.resource_suffix}-core-admin-username"
  value        = local.postgres_admin_username
  key_vault_id = module.app_group.key_vault_id
}

resource "azurerm_key_vault_secret" "postgresql_connection_string" {
  name         = "psql-${local.resource_suffix}-core-connection-string"
  value        = "Host=${azurerm_postgresql_flexible_server.core.fqdn};Database=betterme;Username=${local.postgres_admin_username};Password=${random_password.postgresql.result};SSL Mode=Require;Trust Server Certificate=true"
  key_vault_id = module.app_group.key_vault_id
}

resource "azurerm_postgresql_flexible_server_firewall_rule" "this" {
  for_each = local.postgres_firewall_rules

  name             = each.key
  server_id        = azurerm_postgresql_flexible_server.core.id
  start_ip_address = each.value
  end_ip_address   = each.value
}
