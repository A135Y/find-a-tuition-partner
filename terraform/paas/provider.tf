provider "cloudfoundry" {
  api_url  = var.api_url
  user     = var.username
  password = var.password
}


terraform {

  required_providers {
    cloudfoundry = {
      source  = "cloudfoundry-community/cloudfoundry"
      version = "0.15.3"
    }
  }
}