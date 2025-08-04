//@ts-check
//account-justification-page.js
const { expect } = require('@playwright/test');
const { AzureB2CSignupPage } = require('../pages/azureB2C-signup-page')

exports.AccountJustificationPage = class AccountJustificationPage {

  #continueButton
  #backButton

  /**
   * @param {import('@playwright/test').Page} page
   * @param {string | undefined} baseURL
   */
  constructor(page, baseURL) {
    this.page = page;
    this.baseURL = baseURL;
    this.accountJustificationheading = this.page.locator('h1:has-text("You’ll need an account to use this service")');

    this.#continueButton = this.page.locator('a:has-text("Create an account")');
    this.#backButton = page.locator('a.govuk-back-link');
  }

  async selectCreateAccount() {
    await this.#continueButton.click();
    return new AzureB2CSignupPage(this.page, this.baseURL);
  }

  async back() {
    await this.#backButton.click();
  }
}