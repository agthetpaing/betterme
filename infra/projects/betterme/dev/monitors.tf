data "azurerm_container_app" "api" {
  name                = var.container_app_name
  resource_group_name = module.app_group.resource_group_name
}

resource "azurerm_monitor_action_group" "ops" {
  name                = "ag-${local.resource_suffix}"
  resource_group_name = module.app_group.resource_group_name
  short_name          = "betterme"
  tags                = local.common_tags

  email_receiver {
    name          = "ops"
    email_address = var.alert_email
  }
}

resource "azurerm_monitor_metric_alert" "api_5xx" {
  name                = "alert-${local.resource_suffix}-api-5xx"
  resource_group_name = module.app_group.resource_group_name
  scopes              = [data.azurerm_container_app.api.id]
  description         = "BetterMe API returned more than five 5xx responses in five minutes."
  severity            = 2
  frequency           = "PT1M"
  window_size         = "PT5M"
  tags                = local.common_tags

  criteria {
    metric_namespace = "Microsoft.App/containerApps"
    metric_name      = "Requests"
    aggregation      = "Total"
    operator         = "GreaterThan"
    threshold        = 5

    dimension {
      name     = "statusCodeCategory"
      operator = "Include"
      values   = ["5xx"]
    }
  }

  action {
    action_group_id = azurerm_monitor_action_group.ops.id
  }
}

resource "azurerm_monitor_metric_alert" "api_restarts" {
  name                = "alert-${local.resource_suffix}-api-restarts"
  resource_group_name = module.app_group.resource_group_name
  scopes              = [data.azurerm_container_app.api.id]
  description         = "BetterMe API replica restarted (possible crash loop)."
  severity            = 2
  frequency           = "PT1M"
  window_size         = "PT5M"
  tags                = local.common_tags

  criteria {
    metric_namespace = "Microsoft.App/containerApps"
    metric_name      = "RestartCount"
    aggregation      = "Total"
    operator         = "GreaterThan"
    threshold        = 0
  }

  action {
    action_group_id = azurerm_monitor_action_group.ops.id
  }
}
