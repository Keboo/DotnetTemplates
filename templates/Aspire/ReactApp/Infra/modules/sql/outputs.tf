output "server_fqdn" {
  value = azurerm_mssql_server.sql_server.fully_qualified_domain_name
}

output "database_name" {
  value = azurerm_mssql_database.database.name
}

output "connection_string" {
  value = local.connection_string
}

output "database_id" {
  description = "ID of the SQL database"
  value       = azurerm_mssql_database.database.id
}