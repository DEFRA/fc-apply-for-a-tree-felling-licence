//azureB2C-login-page.js

const { expect } = require('@playwright/test');
const Users  = require('../dataprovider/users/users');
class AzureB2CSignupPage {
    /**
     * @param {import('playwright').Page} page
     * @param {string | undefined} baseURL
     */
    constructor(page, baseURL) {
      this.page = page;
      this.baseURL = baseURL;

      this.signupheading = page.locator('h1.govuk-heading-l');
      this.errorSummaryLocator = page.locator('#error-summary-title');
      this.errorDetailList = page.locator('ul.govuk-error-summary__list > li > a');
      this.signInHeading = page.locator('h1.govuk-heading-l[role="heading"]:has-text("Sign in")');
    }
    
    async submitEmail(userEmail){
      const TestUser = Users.GetUserByEmail(userEmail);
      await this.page.locator('input[aria-label="Email Address"]').fill(TestUser.email);
      await this.page.locator('button:has-text("Continue")').click();
    }

    //Yes - will need to handle an array of errors, address when required:
    async expectErrorSummaryHavingText(errorText){
      await expect(this.errorSummaryLocator).toHaveText("There is a problem");
      await expect(this.errorDetailList).toHaveText(errorText);
    }
  }
  
  module.exports = { AzureB2CSignupPage };