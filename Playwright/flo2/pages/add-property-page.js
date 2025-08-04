//@ts-check
//add-property-page.js
const { expect } = require('@playwright/test');
const { FloDefaultAuthPage } = require('./flo-default-auth-page');
const { PropertyProfilePage } = require('./property-profile-page');

exports.AddPropertyPage = class AddPropertyPage extends FloDefaultAuthPage {

  #propertyNameLocator
  #nearestTownLocator
  #hasWoodlandManagementPlanLocator
  #woodlandManagementPlanReferenceLocator
  #hasWoodlandCertSchemeLocator
  #woodlandCertSchemeReferenceLocator

  /**
   * @param {import('@playwright/test').Page} page
   */
  constructor(page, baseURL) {
    super(page, baseURL);
    this.page = page;
    this.baseURL = baseURL;
    this.govukheading = page.locator('h1.govuk-heading-xl');
    this.#propertyNameLocator = page.locator('.govuk-input#Name');
    this.#nearestTownLocator = page.locator('.govuk-input#NearestTown');
    this.#hasWoodlandManagementPlanLocator = page.locator('input#HasWoodlandManagementPlan');
    this.#woodlandManagementPlanReferenceLocator = page.locator('.govuk-input#WoodlandManagementPlanReference');
    this.#hasWoodlandCertSchemeLocator = page.locator('input#IsWoodlandCertificationScheme');
    this.#woodlandCertSchemeReferenceLocator = page.locator('.govuk-input#WoodlandCertificationSchemeReference');
  }

  async setPropertyName(propertyName){
    await this.#propertyNameLocator.fill(propertyName);
    return this;
  }
  async setNearestTown(nearestTown){
    await this.#nearestTownLocator.fill(nearestTown);
    return this;
  }

  //todo, radios on this application screen is currenty wrong, so this is how the POM has to work. - see FLOV2-336
  async hasWoodlandManagementPlan(planReference){
    await this.#hasWoodlandManagementPlanLocator.nth(0).check();
    await this.setWoodlandManagementPlanReference(planReference);
    return this;
  }

  async setWoodlandManagementPlanReference(planReference){
    await this.#woodlandManagementPlanReferenceLocator.fill(planReference);
    return this;
  }

  async uncheckHasWoodlandManagementPlan(){
    await this.#hasWoodlandManagementPlanLocator.nth(1).check(); 
  }
  
  //todo, radios on this application screen is currenty wrong, so this is how the POM has to work. - see FLOV2-336
  async hasWoodlandCertScheme(schemeReference){
    await this.#hasWoodlandCertSchemeLocator.nth(0).check();
    await this.setCertSchemeReference(schemeReference);
    return this;
  }

  async setCertSchemeReference(schemeReference){
    await this.#woodlandCertSchemeReferenceLocator.fill(schemeReference);
    return this;
  }

  async uncheckHasWoodCertScheme(){
    await this.#hasWoodlandCertSchemeLocator.nth(1).check(); 
  }

  async continue() {
    await super.clickContinue();
    return new PropertyProfilePage(this.page, this.baseURL);
  }  
  async cancel() {
    await super.clickCancel();
  }
}