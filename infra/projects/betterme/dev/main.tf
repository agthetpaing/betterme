locals {
  common_tags = {
    environment = "dev"
    owner       = "apaing"
    project     = "betterme"
  }

  resource_suffix = "betterme-dev-ae"
}

module "app_group" {
  source = "../../../modules/app-group"

  name     = local.resource_suffix
  location = "australiaeast"

  tags = local.common_tags
}
