variable "location" {
  default = "East US"
}

variable "resource_group_name" {
  default = "authorization"
}

variable "vnet_name" {
  default = "AuthorizationVnet"
}

variable "sg_name" {
  default = "authorization-vault"
}

variable "vnet_cidr_range" {
  type    = string
  default = "10.1.0.0/16"
}

variable "subnet_prefixes" {
  type    = list(string)
  default = ["10.1.1.0/24", "10.1.2.0/24", "10.1.3.0/24"]
}

variable "subnet_names" {
  default = ["azure-vault-public-subnet", "azure-vault-private-subnet", "siem"]
}

## Provisioning script variables

variable "cmd_extension" {
  description = "Command to be excuted by the custom script extension"
  default     = "sh vault-install.sh"
}

variable "cmd_script" {
  description = "Script to download which can be executed by the custom script extension"
  default     = "https://gist.githubusercontent.com/anubhavmishra/0b6eb19f38e63bb2eb9d459fd1c53b1d/raw/696eea84b8d12cd099c283439c2c412ae13d308d/vault-install.sh"
}

variable "ssh_key_public" {}

variable "ssh_key_private" {}