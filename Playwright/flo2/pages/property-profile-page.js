//@ts-check
//property-profile-page.js
const { expect } = require('@playwright/test');
const { AddCompartmentDetailsPage } = require('./add-compartment-details-page');
const { FloDefaultAuthPage } = require('./flo-default-auth-page');

exports.PropertyProfilePage = class PropertyProfilePage extends FloDefaultAuthPage {

  #addCompartmentLocator;

  /**
   * @param {import('@playwright/test').Page} page
   */
  constructor(page, baseURL) {
    super(page, baseURL);
    this.page = page;
    this.baseURL = baseURL;
    this.govukheading = page.locator('h1.govuk-heading-xl');

    //todo have to do nth here, as there are actually two of these in the DOM thanks to datatables.. even though only one in app source code.
    this.#addCompartmentLocator = page.locator('a.govuk-link:has-text("Add Compartment")').nth(1);
  }

  async addCompartment(){
    await this.#addCompartmentLocator.click();
    return new AddCompartmentDetailsPage(this.page, this.baseURL);
  }
}