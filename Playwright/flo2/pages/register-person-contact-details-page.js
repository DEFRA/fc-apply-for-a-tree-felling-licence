//@ts-check
//register-person-contact-details-page.js
const { expect } = require('@playwright/test');
const { FloDefaultAuthPage } = require('./flo-default-auth-page');
const { RegisterOrganisationDetailsPage } = require('./register-organisation-details-page');
const { RegisterAccountSummaryPage } = require('./register-account-summary-page');

exports.RegisterPersonContactDetailsPage = class RegisterPersonContactDetailsPage extends FloDefaultAuthPage {

  #addressLine1;
  #addressLine2;
  #addressLine3;
  #addressLine4;
  #postcode;
  #contactTelephone;
  #contactMobTelepehone;
  #preferredContactMethod;

  /**
   * @param {import('@playwright/test').Page} page
   */
  constructor(page, baseURL) {
    super(page, baseURL);
    this.page = page;
    this.baseURL = baseURL;
    this.#addressLine1 = page.locator('.govuk-input#PersonContactsDetails_ContactAddress_Line1');
    this.#addressLine2 = page.locator('.govuk-input#PersonContactsDetails_ContactAddress_Line2');
    this.#addressLine3 = page.locator('.govuk-input#PersonContactsDetails_ContactAddress_Line3');
    this.#addressLine4 = page.locator('.govuk-input#PersonContactsDetails_ContactAddress_Line4');
    this.#postcode = page.locator('.govuk-input#PersonContactsDetails_ContactAddress_PostalCode');
    this.#contactTelephone = page.locator('.govuk-input#PersonContactsDetails_ContactTelephoneNumber');
    this.#contactMobTelepehone = page.locator('.govuk-input#PersonContactsDetails_ContactMobileNumber');
    this.#preferredContactMethod = page.locator('.govuk-select#PersonContactsDetails_PreferredContactMethod');
  }

  async setPreferredContactMethod(method) {
    await (this.#preferredContactMethod).selectOption({label: method});
    return this;
  }

  async setAddressLine1(addressLine1) {
    await (this.#addressLine1).fill(addressLine1);
    return this;
  }
  async setAddressLine2(addressLine2) {
    await (this.#addressLine2).fill(addressLine2);
    return this;
  }
  async setAddressLine3(addressLine3) {
    await (this.#addressLine3).fill(addressLine3);
    return this;
  }
  
  async setAddressLine4(addressLine4) {
    await (this.#addressLine4).fill(addressLine4);
    return this;
  }
  
  async setPostcode(postcode) {
    await (this.#postcode).fill(postcode);
    return this;
  }
  
  async setTelephone(telephone) {
    await (this.#contactTelephone).fill(telephone);
    return this;
  }

  async setContactMobileTelephone(mobTelephphone) {
    await (this.#contactMobTelepehone).fill(mobTelephphone);
    return this;
  }

  async continueAsWoodlandOwner() {
    await super.clickContinue();
    return new RegisterAccountSummaryPage(this.page, this.baseURL);
  }

  async continueAsWoodlandOrganisation() {
    await super.clickContinue();
    return new RegisterOrganisationDetailsPage(this.page, this.baseURL);
  }
  
  async cancel() {
    await super.clickCancel();
  }
}