//@ts-check
//account-holder-confirmation-page.js
const { expect } = require('@playwright/test');
const { AzureB2CLoginPage } = require('../pages/azureB2C-login-page');
const { AccountJustificationPage } = require('../pages/account-justification-page')

exports.AccountHolderConfirmationPage = class AccountHolderConfirmationPage {

  #yesRadio
  #noRadio
  #continueButton
  #backButton

  /**
   * @param {import('@playwright/test').Page} page
   * @param {string | undefined} baseURL
   */
  constructor(page, baseURL) {
    this.page = page;
    this.baseURL = baseURL;

    this.#yesRadio = page.locator('input[value="True"]');
    this.#noRadio = page.locator('input[value="False"]');
    this.#continueButton = this.page.locator('button.govuk-button');
    this.#backButton = page.locator('a.govuk-back-link');
  }

  async selectYesAccountHolder() {
    await this.#yesRadio.check();
    await this.#continueButton.click();
    return new AzureB2CLoginPage(this.page, this.baseURL);
  }

  async selectNoAccountHolder() {
    await this.#noRadio.check();
    await this.#continueButton.click();
    return new AccountJustificationPage(this.page, this.baseURL);
  }

  async back() {
    await this.#backButton.click();
  }
}