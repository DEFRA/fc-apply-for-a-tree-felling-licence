//@ts-check
//register-person-name-page.js
const { expect } = require('@playwright/test');
const { FloDefaultAuthPage } = require('./flo-default-auth-page');
const { RegisterPersonContactDetailsPage } = require('./register-person-contact-details-page');

exports.RegisterPersonNamePage = class RegisterPersonNamePage extends FloDefaultAuthPage {

  #titleSelect
  #firstName
  #lastName
  
  /**
   * @param {import('@playwright/test').Page} page
   */
  constructor(page, baseURL) {
    super(page, baseURL);
    this.page = page;
    this.baseURL = baseURL;
    this.#titleSelect = page.locator('.govuk-select#PersonName_Title');
    this.#firstName = page.locator('.govuk-input#PersonName_FirstName');
    this.#lastName = page.locator('.govuk-input#PersonName_LastName');
  }

  async setTitle(title) {
    await (this.#titleSelect).selectOption(title);
    return this;
  }

  async setFirstName(firstName) {
    await (this.#firstName).fill(firstName);
    return this;
  }

  async setLastName(lastName) {
    await (this.#lastName).fill(lastName);
    return this;
  }

  async continue() {
    await super.clickContinue();
    return new RegisterPersonContactDetailsPage(this.page, this.baseURL);
  }

  async cancel() {
    await super.clickCancel();
  }
}