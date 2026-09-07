variable "name" {
  description = "Resource suffix used for the resource group, identity, and Key Vault"
  type        = string
}

variable "location" {
  description = "Azure region"
  type        = string
}

variable "tags" {
  description = "Tags applied to all app-group resources"
  type        = map(string)
}
