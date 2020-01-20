output "vault_public_ip" {
  description = "public IP address of the vault server"
  value       = azurerm_public_ip.vault.ip_address
}

output "vault_private_ip" {
  description = "private IP address of the vault server"
  value       = azurerm_network_interface.vault.private_ip_address
}

output "vault_ssh" {
  description = "shortcut to ssh into the vault vm."
  value = "ssh azureuser@${azurerm_public_ip.vault.ip_address} -i ${path.module}/.ssh/id_rsa -L 8200:localhost:8200"
}