//@ts-check
//woodland-owner-home-page.js
const { expect } = require('@playwright/test');
const { AddPropertyPage } = require('./add-property-page');
const { FloDefaultAuthPage } = require('./flo-default-auth-page');

exports.WoodlandOwnerHomepage = class WoodlandOwnerHomepage extends FloDefaultAuthPage {

  #addPropertyLinkLocator

  /**
   * @param {import('@playwright/test').Page} page
   */
  constructor(page, baseURL) {
    super(page, baseURL);
    this.page = page;
    this.baseURL = baseURL;
    this.govukheading = page.locator('h1.govuk-heading-xl');
    this.#addPropertyLinkLocator = page.locator('a.govuk-button', { hasText: 'Add Property' });
  }

  async addPropertyClick() {
    await this.#addPropertyLinkLocator.click();
    return new AddPropertyPage(this.page, this.baseURL);
  }
}