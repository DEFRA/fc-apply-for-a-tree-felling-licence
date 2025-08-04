//@ts-check
//register-account-type-selection.js
const { expect } = require('@playwright/test');
const { FloDefaultAuthPage } = require('./flo-default-auth-page');
const { RegisterOwnerTypePage } = require('./register-owner-type-selection-page');


exports.RegisterAccountTypePage = class RegisterAccountTypePage extends FloDefaultAuthPage {

  #OwnerRadio
  #AgentRadio
  #TenantRadio
  #TrustRadio
  
  /**
   * @param {import('@playwright/test').Page} page
   */
  constructor(page, baseURL) {
    super(page, baseURL);
    this.page = page;
    this.baseURL = baseURL;

    //todo this needs to be  changed once FLOV22-299 is sorted.
    this.#AgentRadio = page.locator('input[name="UserTypeModel\\.AccountType"] >> nth=0');
    this.#OwnerRadio = page.locator('input[name="UserTypeModel\\.AccountType"] >> nth=1');
    this.#TenantRadio = page.locator('input[name="UserTypeModel\\.AccountType"] >> nth=2');
    this.#TrustRadio = page.locator('input[name="UserTypeModel\\.AccountType"] >> nth=3');
  }

  async selectAgent() {
    await this.#AgentRadio.check();
  }

  async selectOwner() {
    await this.#OwnerRadio.check();
  }

  async selectTenant() {
    await this.#TenantRadio.check();
  }

  async selectTrust() {
    await this.#TrustRadio.check();
  }

  async continue() {
    await super.clickContinue();
    return new RegisterOwnerTypePage(this.page, this.baseURL);
  }

  async cancel() {
    await super.clickCancel();
  }
}