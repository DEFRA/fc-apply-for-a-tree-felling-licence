# This is temporary and will be removed once we have proper vnet connection setup
# NSG – lock SSH to Forestry IPs only
resource "azurerm_network_security_group" "jumpbox" {
  name                = "nsg-temp-pg-jumpbox"
  location            = module.shared.azure_location
  resource_group_name = azurerm_resource_group.fs_flov2.name

  security_rule {
    name                       = "Allow-SSH-FE"
    priority                   = 100
    direction                  = "Inbound"
    access                     = "Allow"
    protocol                   = "Tcp"
    source_port_range          = "*"
    destination_port_range     = "22"
    source_address_prefixes    = ["xx.xx.x.xx/32", "xx.xx.x.xx/32"] # Testing IP but will be changed to FE IPs - think will be 35.177.250.84
    destination_address_prefix = "*"
  }

  security_rule {
    name                       = "Deny-All-Inbound"
    priority                   = 4096
    direction                  = "Inbound"
    access                     = "Deny"
    protocol                   = "*"
    source_port_range          = "*"
    destination_port_range     = "*"
    source_address_prefix      = "*"
    destination_address_prefix = "*"
  }

  security_rule {
    name                       = "Allow-Postgres-To-Replica"
    priority                   = 100
    direction                  = "Outbound"
    access                     = "Allow"
    protocol                   = "Tcp"
    source_port_range          = "*"
    destination_port_range     = "5432"
    destination_address_prefix = "192.168.58.0/24" # replica DB subnet
    source_address_prefix      = "*"
  }

  security_rule {
    name                       = "Allow-DNS"
    priority                   = 110
    direction                  = "Outbound"
    access                     = "Allow"
    protocol                   = "*"
    source_port_range          = "*"
    destination_port_ranges    = ["53"]
    destination_address_prefix = "*"
    source_address_prefix      = "*"
  }

  security_rule {
    name                       = "Deny-All-Outbound"
    priority                   = 4096
    direction                  = "Outbound"
    access                     = "Deny"
    protocol                   = "*"
    source_port_range          = "*"
    destination_port_range     = "*"
    destination_address_prefix = "*"
    source_address_prefix      = "*"
    }

}

# NIC + Public IP
resource "azurerm_public_ip" "jumpbox" {
  name                = "pip-temp-pg-jumpbox"
  location            = module.shared.azure_location
  resource_group_name = azurerm_resource_group.fs_flov2.name
  allocation_method   = "Static"
  sku                 = "Standard"
}

resource "azurerm_network_interface" "jumpbox" {
  name                = "nic-temp-pg-jumpbox"
  location            = module.shared.azure_location
  resource_group_name = azurerm_resource_group.fs_flov2.name

  ip_configuration {
    name                          = "ipconfig1"
    subnet_id                     = azurerm_subnet.default.id
    private_ip_address_allocation = "Dynamic"
    public_ip_address_id          = azurerm_public_ip.jumpbox.id
  }
}

resource "azurerm_network_interface_security_group_association" "jumpbox" {
  network_interface_id      = azurerm_network_interface.jumpbox.id
  network_security_group_id = azurerm_network_security_group.jumpbox.id
}

# Linux VM (small + cheap)
resource "azurerm_linux_virtual_machine" "jumpbox" {
  name                = "temp-pg-jumpbox"
  resource_group_name = azurerm_resource_group.fs_flov2.name
  location            = module.shared.azure_location
  size                = "Standard_B1ms"
  admin_username      = "azureuser"

  network_interface_ids = [
    azurerm_network_interface.jumpbox.id
  ]

  # Admin access user from test ssh
  admin_ssh_key {
    username   = "azureuser"
    public_key = module.shared.test_ssh
  }

  # add for each user and azureadmin
  # admin_ssh_key {
  #   username   = "azureadmin"
  #   public_key = module.shared.azurerm_key_vault_secrets.fs_user_1_public_key
  # }

  os_disk {
    caching              = "ReadWrite"
    storage_account_type = "Standard_LRS"
  }

  source_image_reference {
    publisher = "Canonical"
    offer     = "0001-com-ubuntu-server-jammy"
    sku       = "22_04-lts"
    version   = "latest"
  }

  tags = {
    purpose = "temporary-db-access"
    owner   = "quicksilva"
    expiry  = "DELETE-AFTER-PRIVATE-LINK"
  }
}

# Output Jumpbox Public IP
output "jumpbox_public_ip" {
  value = azurerm_public_ip.jumpbox.ip_address
}

# --------------------------------------------------------------------
# Stop & Start VM
resource "azurerm_automation_account" "jumpbox_startstop" {
  name                = "aa-flov2-startstop"
  location            = azurerm_resource_group.fs_flov2.location
  resource_group_name = azurerm_resource_group.fs_flov2.name
  sku_name            = "Basic"

  identity { type = "SystemAssigned" }

  tags = {
    purpose = "startstop"
    owner   = "quicksilva"
  }
}

resource "azurerm_role_assignment" "jumpbox_vmcontrib" {
  scope                = azurerm_linux_virtual_machine.jumpbox.id
  role_definition_name = "Virtual Machine Contributor"
  principal_id         = azurerm_automation_account.jumpbox_startstop.identity[0].principal_id

  depends_on = [
    azurerm_automation_account.jumpbox_startstop
  ]
}

resource "azurerm_automation_module" "jumpbox_az_accounts" {
  automation_account_name = azurerm_automation_account.jumpbox_startstop.name
  resource_group_name     = azurerm_automation_account.jumpbox_startstop.resource_group_name
  name                    = "Az.Accounts"

  module_link {
    uri = "https://www.powershellgallery.com/api/v2/package/Az.Accounts/2.16.0"
  }
}

resource "azurerm_automation_module" "jumpbox_az_compute" {
  automation_account_name = azurerm_automation_account.jumpbox_startstop.name
  resource_group_name     = azurerm_automation_account.jumpbox_startstop.resource_group_name
  name                    = "Az.Compute"

  module_link {
    uri = "https://www.powershellgallery.com/api/v2/package/Az.Compute/11.6.0"
  }

  depends_on = [azurerm_automation_module.jumpbox_az_accounts]
}

resource "azurerm_automation_runbook" "jumpbox_start" {
  name                    = "Start-VM-temp-pg-jumpbox"
  location                = azurerm_resource_group.fs_flov2.location
  resource_group_name     = azurerm_resource_group.fs_flov2.name
  automation_account_name = azurerm_automation_account.jumpbox_startstop.name
  runbook_type            = "PowerShell72"
  log_verbose             = true
  log_progress            = true
  description             = "Starts temp-pg-jumpbox at 07:00 on weekdays."

  content = <<-POWERSHELL
    param(
      [Parameter(Mandatory=$true)][string]$resourcegroupname,
      [Parameter(Mandatory=$true)][string]$vmname
    )

    Get-Module Az.* | Remove-Module -Force -ErrorAction SilentlyContinue
    Import-Module Az.Accounts -Force
    Import-Module Az.Compute  -Force

    Disable-AzContextAutosave -Scope Process | Out-Null
    Connect-AzAccount -Identity | Out-Null

    Start-AzVM -ResourceGroupName $resourcegroupname -Name $vmname -NoWait
  POWERSHELL

  depends_on = [
    azurerm_automation_module.jumpbox_az_accounts,
    azurerm_automation_module.jumpbox_az_compute
  ]
}

resource "azurerm_automation_runbook" "jumpbox_stop" {
  name                    = "Stop-VM-temp-pg-jumpbox"
  location                = azurerm_resource_group.fs_flov2.location
  resource_group_name     = azurerm_resource_group.fs_flov2.name
  automation_account_name = azurerm_automation_account.jumpbox_startstop.name
  runbook_type            = "PowerShell72"
  log_verbose             = true
  log_progress            = true
  description             = "Stops (deallocates) temp-pg-jumpbox at 18:00 on weekdays."

  content = <<-POWERSHELL
    param(
      [Parameter(Mandatory=$true)][string]$resourcegroupname,
      [Parameter(Mandatory=$true)][string]$vmname
    )

    Get-Module Az.* | Remove-Module -Force -ErrorAction SilentlyContinue
    Import-Module Az.Accounts -Force
    Import-Module Az.Compute  -Force

    Disable-AzContextAutosave -Scope Process | Out-Null
    Connect-AzAccount -Identity | Out-Null

    Stop-AzVM -ResourceGroupName $resourcegroupname -Name $vmname -Force
  POWERSHELL

  depends_on = [
    azurerm_automation_module.jumpbox_az_accounts,
    azurerm_automation_module.jumpbox_az_compute
  ]
}

resource "azurerm_automation_schedule" "jumpbox_wkday_0700" {
  name                    = "Jumpbox-Weekdays-07-00"
  resource_group_name     = azurerm_resource_group.fs_flov2.name
  automation_account_name = azurerm_automation_account.jumpbox_startstop.name

  frequency = "Week"
  interval  = 1
  timezone  = "Europe/London"
  week_days = ["Monday","Tuesday","Wednesday","Thursday","Friday"]

  # pick any future date/time that matches the intended hour; Azure will repeat weekly
  start_time  = "2025-12-22T07:00:00Z"
  description = "Start jumpbox at 07:00 Mon–Fri"
}

resource "azurerm_automation_schedule" "jumpbox_wkday_1800" {
  name                    = "Jumpbox-Weekdays-18-00"
  resource_group_name     = azurerm_resource_group.fs_flov2.name
  automation_account_name = azurerm_automation_account.jumpbox_startstop.name

  frequency = "Week"
  interval  = 1
  timezone  = "Europe/London"
  week_days = ["Monday","Tuesday","Wednesday","Thursday","Friday"]

  start_time  = "2025-12-22T18:00:00Z"
  description = "Stop jumpbox at 18:00 Mon–Fri"
}

resource "azurerm_automation_job_schedule" "jumpbox_start_link" {
  resource_group_name     = azurerm_resource_group.fs_flov2.name
  automation_account_name = azurerm_automation_account.jumpbox_startstop.name
  schedule_name           = azurerm_automation_schedule.jumpbox_wkday_0700.name
  runbook_name            = azurerm_automation_runbook.jumpbox_start.name

  parameters = {
    resourcegroupname = azurerm_linux_virtual_machine.jumpbox.resource_group_name
    vmname            = azurerm_linux_virtual_machine.jumpbox.name
  }

  depends_on = [
    azurerm_automation_runbook.jumpbox_start,
    azurerm_automation_schedule.jumpbox_wkday_0700,
    azurerm_automation_module.jumpbox_az_accounts,
    azurerm_automation_module.jumpbox_az_compute
  ]
}

resource "azurerm_automation_job_schedule" "jumpbox_stop_link" {
  resource_group_name     = azurerm_resource_group.fs_flov2.name
  automation_account_name = azurerm_automation_account.jumpbox_startstop.name
  schedule_name           = azurerm_automation_schedule.jumpbox_wkday_1800.name
  runbook_name            = azurerm_automation_runbook.jumpbox_stop.name

  parameters = {
    resourcegroupname = azurerm_linux_virtual_machine.jumpbox.resource_group_name
    vmname            = azurerm_linux_virtual_machine.jumpbox.name
  }

  depends_on = [
    azurerm_automation_runbook.jumpbox_stop,
    azurerm_automation_schedule.jumpbox_wkday_1800,
    azurerm_automation_module.jumpbox_az_accounts,
    azurerm_automation_module.jumpbox_az_compute
  ]
}
