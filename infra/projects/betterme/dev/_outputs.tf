output "resource_group_name" {
  value = module.app_group.resource_group_name
}

output "postgres_fqdn" {
  value = azurerm_postgresql_flexible_server.core.fqdn
}

output "postgres_database" {
  value = azurerm_postgresql_flexible_server_database.betterme.name
}

output "key_vault_uri" {
  value = module.app_group.key_vault_uri
}

output "log_analytics_workspace_id" {
  value = azurerm_log_analytics_workspace.this.id
}
