//@ts-check
//register-owner-type-page.js
const { expect } = require('@playwright/test');
const { FloDefaultAuthPage } = require('./flo-default-auth-page');
const { RegisterPersonNamePage } = require('./register-person-name-page');

exports.RegisterOwnerTypePage = class RegisterOwnerTypePage extends FloDefaultAuthPage {

  #IndividualRadio
  #OrganisationRadio
  
  /**
   * @param {import('@playwright/test').Page} page
   */
  constructor(page, baseURL) {
    super(page, baseURL);
    this.page = page;
    this.baseURL = baseURL;

    //todo this needs to be  changed once FLOV22-299 is sorted.
    this.#IndividualRadio = page.locator('input[name="OrganisationStatus"] >> nth=0');
    this.#OrganisationRadio = page.locator('input[name="OrganisationStatus"] >> nth=1');
  }

  async selectIndividual() {
    await this.#IndividualRadio.check();
  }

  async selectOrganisation() {
    await this.#OrganisationRadio.check();
  }

  async continue() {
    await super.clickContinue();
    return new RegisterPersonNamePage(this.page, this.baseURL);
  }

  async cancel() {
    await super.clickCancel();
  }
}