locals {
  action_group_id = {
    prod    = data.terraform_remote_state.platform.outputs.action_group_prod_id
    nonprod = data.terraform_remote_state.platform.outputs.action_group_nonprod_id
  }

  rg_name = data.terraform_remote_state.platform.outputs.resource_group_name
}