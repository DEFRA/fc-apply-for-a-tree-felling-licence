//azureB2C-login-page.js

const { expect } = require('@playwright/test');
const { RegisterAccountTypePage } = require('./register-account-type-page');
const { WoodlandOwnerHomepage } = require('./woodland-owner-home-page');
const { AccountJustificationPage } = require('../pages/account-justification-page')
const Users  = require('../dataprovider/users/users');
class AzureB2CLoginPage {
    /**
     * @param {import('playwright').Page} page 
     */
    constructor(page, baseURL) {
      this.page = page;
      this.baseURL = baseURL;
      this.errorSummaryLocator = page.locator('#error-summary-title');
      this.errorDetailList = page.locator('ul.govuk-error-summary__list > li > a');
      this.signInheading = page.locator('h1.govuk-heading-l[role="heading"]:has-text("Sign in")');
      this.noAccountLink = this.page.locator('a:has-text("I do not have an account")');
    }
    
    async loginAsWoodlandOwner(userEmail) {
      await this.submitCredentials(userEmail);
      return new WoodlandOwnerHomepage(this.page, this.baseURL);
    }

    async loginAsNewFloUser(userEmail) {
      await this.submitCredentials(userEmail);
      return new RegisterAccountTypePage(this.page, this.baseURL);
    }

    async signUpLink() {
      await this.noAccountLink.click();
      return new AccountJustificationPage(this.page, this.baseURL);
    }

    async loginExpectingError(userEmail) {
      await this.submitCredentials(userEmail);
      return this; // expecting to remain on azure - if having an authentication issue. 
    }

    async loginWithIncorrectPassword(userEmail, badPassword) {
      await this.page.locator('input[name="Email Address"]').fill(userEmail);
      await this.page.locator('input[name="Password"]').fill(badPassword);
      await this.page.locator('button:has-text("Continue")').click();
      return this; // expecting to remain on azure - if having an authentication issue. 
    }

    async submitCredentials(userEmail){
      const TestUser = Users.GetUserByEmail(userEmail);
      await this.page.locator('input[name="Email Address"]').fill(TestUser.email);
      await this.page.locator('input[name="Password"]').fill(TestUser.password);
      await this.page.locator('button:has-text("Continue")').click();
    }

    //Yes - will need to handle an array of errors, address when required:
    async expectErrorSummaryHavingText(errorText){
      await expect(this.errorSummaryLocator).toHaveText("There is a problem");
      await expect(this.errorDetailList).toHaveText(errorText);
    }
  }
  
  module.exports = { AzureB2CLoginPage };