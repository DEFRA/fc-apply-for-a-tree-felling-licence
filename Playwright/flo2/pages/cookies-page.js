//@ts-check
//cookiesPage.js
const { expect } = require('@playwright/test');

exports.CookiesPage = class CookiesPage {

  /**
   * @param {import('@playwright/test').Page} page
   */
  constructor(page) {
    this.page = page;
    this.govukheading = page.locator('h1.govuk-heading-xl');
  }




  // async clickBackLink() {
  //   await this.page.locator('a.govuk-button--start').click();
  //   return await new AzureB2CLoginPage(this.page);
  //   // await this.getStartedLink.first().click();
  //   // await expect(this.gettingStartedHeader).toBeVisible();
  // }
}