terraform {
  backend "s3" {
    bucket = "terraformstate-dv"
    key    = "awscsvprocess.tfstate"
    region = "eu-west-1"
  }

  required_providers {
    aws = {
      source = "hashicorp/aws"
      version = "4.63.0"
    }

    archive = {
      source = "hashicorp/archive"
      version = "2.3.0"
    }
  }
}

provider "aws" {
  region = "eu-west-1"
}

provider "archive" {
}