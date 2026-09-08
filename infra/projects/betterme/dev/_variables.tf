variable "alert_email" {
  description = "Email address for Azure Monitor action group notifications. Set via TF_VAR_alert_email or -var."
  type        = string
}

variable "container_app_name" {
  description = "Existing Container App name (created by azure-hosting-bootstrap.sh)."
  type        = string
  default     = "betterme-api"
}
