//@ts-check
//woodland-organisation-home-page.js
const { expect } = require('@playwright/test');
const { FloDefaultAuthPage } = require('./flo-default-auth-page');

exports.WoodlandOrganisationHomepage = class WoodlandOrganisationHomepage extends FloDefaultAuthPage {

  /**
   * @param {import('@playwright/test').Page} page
   */
  constructor(page, baseURL) {
    super(page, baseURL);
    this.page = page;
    this.baseURL = baseURL;
    this.govukheading = page.locator('h1.govuk-heading-xl');
  }
}