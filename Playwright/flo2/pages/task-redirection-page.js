//@ts-check
//task-redirection-page.js
const { expect } = require('@playwright/test');
const { AzureB2CLoginPage } = require('../pages/azureB2C-login-page');
const { AccountHolderConfirmationPage } = require('../pages/account-holder-confirmation-page');

exports.TaskRedirectionPage = class TaskRedirectionPage {

  #resumeAnApplicationRadio
  #startNewApplicationRadio
  #signInToMyAccountRadio
  #continueButton
  #cancelLink
  #backButton
  
  /**
   * @param {import('@playwright/test').Page} page
   * @param {string | undefined} baseURL
   */
  constructor(page, baseURL) {
    this.page = page;
    this.baseURL = baseURL;

    this.#startNewApplicationRadio = page.locator('#StartNewApplication');
    this.#resumeAnApplicationRadio = page.locator('#ResumeApplication');
    this.#signInToMyAccountRadio = page.locator('#SignIntoAccount');
    this.#continueButton = this.page.locator('button.govuk-button');
    this.#cancelLink = page.locator('a.govuk-link', { hasText: 'Cancel' });
    this.#backButton = page.locator('a.govuk-back-link');
  }

  async selectStartNewApplication() {
    await this.#startNewApplicationRadio.check();
    await this.#continueButton.click();
    return new AccountHolderConfirmationPage(this.page, this.baseURL);
  }

  async selectResumeAnApplication() {
    await this.#resumeAnApplicationRadio.check();
    await this.#continueButton.click();
    return new AzureB2CLoginPage(this.page, this.baseURL);
  }

  async selectSignInToMyAccountRadio() {
    await this.#signInToMyAccountRadio.check();
    await this.#continueButton.click();
    return new AzureB2CLoginPage(this.page, this.baseURL);
  }

  async cancel() {
    await this.#cancelLink.click();
    
  }

  async back() {
    await this.#backButton.click();
  }
}