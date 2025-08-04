//@ts-check
//register-organisation-details-page.js
const { expect } = require('@playwright/test');
const { FloDefaultAuthPage } = require('./flo-default-auth-page');
const { RegisterAccountSummaryPage } = require('./register-account-summary-page');

exports.RegisterOrganisationDetailsPage = class RegisterOrganisationDetailsPage extends FloDefaultAuthPage {

  #organisationName;
  #contactName;
  #contactEmailAddress;
  #contactTelephone
  #organisationAddressLine1
  #organisationAddressLine2
  #organisationAddressLine3
  #organisationAddressLine4
  #organisationAddressPostCode
  #sameAddressCheckBox

  /**
   * @param {import('@playwright/test').Page} page
   */
  constructor(page, baseURL) {
    super(page, baseURL);
    this.page = page;
    this.baseURL = baseURL;
    this.#organisationName = page.locator('.govuk-input#OrganisationName');
    this.#contactName = page.locator('.govuk-input#ContactName');
    this.#contactEmailAddress = page.locator('.govuk-input#ContactEmail');
    this.#contactTelephone = page.locator('.govuk-input#ContactTelephoneNumber');

    this.#organisationAddressLine1 = page.locator('.govuk-input#OrganisationAddress_Line1');
    this.#organisationAddressLine2 = page.locator('.govuk-input#OrganisationAddress_Line2');
    this.#organisationAddressLine3 = page.locator('.govuk-input#OrganisationAddress_Line3');
    this.#organisationAddressLine4 = page.locator('.govuk-input#OrganisationAddress_Line4');
    this.#organisationAddressPostCode = page.locator('.govuk-input#OrganisationAddress_PostalCode');

    this.#sameAddressCheckBox = page.locator('#ContactAddressMatchesOrganisationAddress')
  }

  async setOrganisationName(organisationName) {
    await (this.#organisationName).fill(organisationName);
    return this;
  }
  async setContactName(contactName) {
    await (this.#contactName).fill(contactName);
    return this;
  }
  async setContactEmailAddress(contactEmail) {
    await (this.#contactEmailAddress).fill(contactEmail);
    return this;
  }

  async setContactMobileTelephone(telephone) {
    await (this.#contactTelephone).fill(telephone);
    return this;
  }

  async setOrganisationAddressLine1(addressLine1) {
    await (this.#organisationAddressLine1).fill(addressLine1);
    return this;
  }
  async setOrganisationAddressLine2(addressLine2) {
    await (this.#organisationAddressLine2).fill(addressLine2);
    return this;
  }
  async setOrganisationAddressLine3(addressLine3) {
    await (this.#organisationAddressLine3).fill(addressLine3);
    return this;
  }

  async setOrganisationAddressLine4(addressLine4) {
    await (this.#organisationAddressLine4).fill(addressLine4);
    return this;
  }

  async setOrganisationAddressPostcode(postcode) {
    await (this.#organisationAddressPostCode).fill(postcode);
    return this;
  }

  async CheckContactAddressMatchesOrganisationRegisteredAddress(){
    await (this.#sameAddressCheckBox).check();
  }

  async continueAsWoodlandOrganisation() {
    await super.clickContinue();
    return new RegisterAccountSummaryPage(this.page, this.baseURL);
  }

  async cancel() {
    await super.clickCancel();
  }
}