//@ts-check
//register-person-name-page.js
const { expect } = require('@playwright/test');
const { FloDefaultAuthPage } = require('./flo-default-auth-page');
const { WoodlandOrganisationHomepage } = require('./woodland-organisation-home-page');
const { WoodlandOwnerHomepage } = require('./woodland-owner-home-page');

exports.TermsAndConditionsPage = class TermsAndConditionsPage extends FloDefaultAuthPage {

  #acceptPrivacyPolicyCheckBox
  #acceptTermsAndConditionsCheckBox
  #acceptButton
  
  /**
   * @param {import('@playwright/test').Page} page
   */
  constructor(page, baseURL) {
    super(page, baseURL);
    this.page = page;
    this.baseURL = baseURL;
    this.#acceptPrivacyPolicyCheckBox = page.locator('.govuk-checkboxes__input#AcceptsTermsAndConditions_AcceptsPrivacyPolicy');
    this.#acceptTermsAndConditionsCheckBox = page.locator('.govuk-checkboxes__input#AcceptsTermsAndConditions_AcceptsTermsAndConditions');
    this.#acceptButton = page.locator('button.govuk-button#accept-terms-btn');
  }

  async acceptTermsAndConditions() {
    await (this.#acceptTermsAndConditionsCheckBox).check();
    return this;
  }
  async acceptPrivacyPolicy() {
    await (this.#acceptPrivacyPolicyCheckBox).check();
    return this;
  }
  async clickAcceptAsWoodlandOwner() {
    await (this.#acceptButton).click();
    return new WoodlandOwnerHomepage(this.page, this.baseURL);
  }

  async clickAcceptAsWoodlandOrganisation() {
    await (this.#acceptButton).click();
    return new WoodlandOrganisationHomepage(this.page, this.baseURL);
  }

  async assertAcceptButtonIsDisabled(){
    expect(this.#acceptButton).toBeDisabled;
  }

  async assertAcceptButtonIsEnabled(){
    expect(this.#acceptButton).not.toBeDisabled;
  }
}