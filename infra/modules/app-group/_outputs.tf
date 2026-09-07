output "resource_group_name" {
  description = "Name of the app-group resource group"
  value       = azurerm_resource_group.this.name
}

output "location" {
  description = "Azure region of the app-group resource group"
  value       = azurerm_resource_group.this.location
}

output "key_vault_id" {
  description = "Resource ID of the app-group Key Vault"
  value       = azurerm_key_vault.this.id
}

output "key_vault_uri" {
  description = "URI of the app-group Key Vault"
  value       = azurerm_key_vault.this.vault_uri
}

output "user_assigned_identity_name" {
  description = "Name of the app-group user-assigned identity"
  value       = azurerm_user_assigned_identity.this.name
}

output "user_assigned_identity_id" {
  description = "Resource ID of the app-group user-assigned identity"
  value       = azurerm_user_assigned_identity.this.id
}
