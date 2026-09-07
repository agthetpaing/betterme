locals {
  common_tags = {
    environment = "dev"
    owner       = "apaing"
    project     = "betterme"
  }

  resource_suffix = "betterme-dev-ea"
}

module "app_group" {
  source = "../../../modules/app-group"

  name     = local.resource_suffix
  location = "eastasia"

  tags = local.common_tags
}
