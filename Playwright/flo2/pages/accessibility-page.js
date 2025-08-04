//@ts-check
//accessibility-page.js
const { expect } = require('@playwright/test');

exports.AccessibilityPage = class AccessibilityPage {

  #backButton

  /**
   * @param {import('@playwright/test').Page} page
   */
  constructor(page) {
    this.page = page;
    this.govukheading = page.locator('h1.govuk-heading-xl');

    this.#backButton = page.locator('a.govuk-back-link');
  }

  async back() {
    await this.#backButton.click();
  }
}