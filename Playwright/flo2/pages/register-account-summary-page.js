//@ts-check
//register-account-summary-page.js
const { expect } = require('@playwright/test');
const { FloDefaultAuthPage } = require('./flo-default-auth-page');
const { TermsAndConditionsPage } = require('./terms-and-conditions-page');

exports.RegisterAccountSummaryPage = class RegisterAccountSummaryPage extends FloDefaultAuthPage {

  // Personal details
  #roleLocator
  #nameLocator;
  #addressLocator;
  #contactTelephoneLocator;
  #contactMobTelephoneLocator;

  // Organisation details
  #organisationName;
  #organisationContactName;
  #organisationContactEmail;
  #organisationContactTelephone;
  #organisationRegisteredAddress;
  #organisationContactAddress;

  #continueButton;

  /**
   * @param {import('@playwright/test').Page} page
   */
  constructor(page, baseURL) {
    super(page, baseURL);
    this.page = page;
    this.baseURL = baseURL;

    this.#roleLocator = page.locator('.govuk-summary-list__row:has-text("Role") .govuk-summary-list__value');
    
    // locate personal details
    this.#nameLocator = page.locator('.govuk-summary-card:has-text("Your personal details") .govuk-summary-list__row:has-text("Name") .govuk-summary-list__value');
    this.#addressLocator = page.locator('.govuk-summary-card:has-text("Your personal details") .govuk-summary-list__row:has-text("Contact address") .govuk-summary-list__value');
    this.#contactTelephoneLocator = page.locator('.govuk-summary-card:has-text("Your personal details") .govuk-summary-list__row:has-text("Telephone number") .govuk-summary-list__value');
    this.#contactMobTelephoneLocator = page.locator('.govuk-summary-card:has-text("Your personal details") .govuk-summary-list__row:has-text("Mobile number") .govuk-summary-list__value');

    // locate organisation details
    this.#organisationName = page.locator('.govuk-summary-card:has-text("Your organisation") >> xpath=//dt[contains(text(), "Name")]/following-sibling::dd[1]');
    this.#organisationContactName = page.locator('.govuk-summary-card:has-text("Your organisation") .govuk-summary-list__row:has-text("Contact name") .govuk-summary-list__value');
    this.#organisationContactEmail = page.locator('.govuk-summary-card:has-text("Your organisation") .govuk-summary-list__row:has-text("Contact email") .govuk-summary-list__value');
    this.#organisationContactTelephone = page.locator('.govuk-summary-card:has-text("Your organisation") .govuk-summary-list__row:has-text("Contact telephone number") .govuk-summary-list__value');
    this.#organisationRegisteredAddress = page.locator('.govuk-summary-card:has-text("Your organisation") .govuk-summary-list__row:has-text("Registered address") .govuk-summary-list__value');
    this.#organisationContactAddress = page.locator('.govuk-summary-card:has-text("Your organisation") .govuk-summary-list__row:has-text("Contact address") .govuk-summary-list__value');

    this.#continueButton = page.locator('a.govuk-button:has-text("Continue")');
  }

  async checkRole(value) {
    await this.checkTextContent(this.#roleLocator, value);
    return this;
  }

  // Check personal details
  async checkName(value) {
    await this.checkTextContent(this.#nameLocator, value);
    return this;
  }

  async checkAddress(value) {
    await this.checkTextContent(this.#addressLocator, value);
    return this;
  }

  async checkContactTelephone(value) {
    await this.checkTextContent(this.#contactTelephoneLocator, value);
    return this;
  }

  async checkContactMobTelephone(value) {
    await this.checkTextContent(this.#contactMobTelephoneLocator, value);
    return this;
  }

  // Check organisation details
  async checkOrganisationName(value) {
    await this.checkTextContent(this.#organisationName, value);
    return this;
  }

  async checkOrganisationContactName(value) {
    await this.checkTextContent(this.#organisationContactName, value);
    return this;
  }

  async checkOrganisationContactEmail(value) {
    await this.checkTextContent(this.#organisationContactEmail, value);
    return this;
  }

  async checkOrganisationContactTelephone(value) {
    await this.checkTextContent(this.#organisationContactTelephone, value);
    return this;
  }

  async checkOrganisationRegisteredAddress(value) {
    await this.checkTextContent(this.#organisationRegisteredAddress, value);
    return this;
  }

  async checkOrganisationContactAddress(value) {
    await this.checkTextContent(this.#organisationContactAddress, value);
    return this;
  }

  // Navigation
  async continue() {
    await this.#continueButton.click();
    return new TermsAndConditionsPage(this.page, this.baseURL);
  }
  
  async cancel() {
    await super.clickCancel();
  }

  // A helper function to get trimmed and normalized text content and compare it
  async checkTextContent(locator, expectedValue) {
    const text = await locator.textContent();
    if (text === null) {
      throw new Error("No text content found in the specified element");
    }

    // Normalize the text content
    const normalizedText = this.normalizeText(text);
    const normalizedExpected = this.normalizeText(expectedValue);

    expect(normalizedText).toBe(normalizedExpected);
  }

  // Function to normalize text (remove extra spaces, line breaks, etc.)
  normalizeText(text) {
    return text.replace(/\s+/g, ' ').trim();
  }
}