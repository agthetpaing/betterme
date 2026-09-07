resource "azurerm_log_analytics_workspace" "this" {
  name                = "log-${local.resource_suffix}"
  location            = module.app_group.location
  resource_group_name = module.app_group.resource_group_name
  sku                 = "PerGB2018"
  retention_in_days   = 30
  tags                = local.common_tags
}

resource "azurerm_monitor_diagnostic_setting" "postgres_core" {
  name                       = "diag-psql-${local.resource_suffix}-core"
  target_resource_id         = azurerm_postgresql_flexible_server.core.id
  log_analytics_workspace_id = azurerm_log_analytics_workspace.this.id

  enabled_log {
    category = "PostgreSQLLogs"
  }

  metric {
    category = "AllMetrics"
    enabled  = true
  }
}
