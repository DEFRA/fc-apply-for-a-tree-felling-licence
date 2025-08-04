//@ts-check
//add-property-page.js
const { expect } = require('@playwright/test');
const { DrawCompartmentPage } = require('./draw-compartment-page');
const { FloDefaultAuthPage } = require('./flo-default-auth-page');

exports.AddCompartmentDetailsPage = class AddCompartmentDetailsPage extends FloDefaultAuthPage {

  #compartmentLocator
  #subCompartmentLocator
  #woodlandNameLocator
  #designationLocator

  /**
   * @param {import('@playwright/test').Page} page
   */
  constructor(page, baseURL) {
    super(page, baseURL);
    this.page = page;
    this.baseURL = baseURL;
    this.govukheading = page.locator('h1.govuk-heading-xl');
    this.#compartmentLocator = page.locator('.govuk-input#CompartmentNumber');
    this.#subCompartmentLocator = page.locator('.govuk-input#SubCompartmentName');
    this.#woodlandNameLocator = page.locator('.govuk-input#WoodlandName');
    this.#designationLocator = page.locator('.govuk-input#Designation');
  }

  async setCompartmentName(compartmentName){
    await this.#compartmentLocator.fill(compartmentName);
    return this;
  }
  async setSubCompartmentName(subCompartmentName){
    await this.#subCompartmentLocator.fill(subCompartmentName);
    return this;
  }

  async setWoodlandName(woodlandName){
    await this.#woodlandNameLocator.fill(woodlandName);
    return this;
  }

  async setDesignation(desgination){
    await this.#designationLocator.fill(desgination); 
  }
  
  async continue() {
    await super.clickContinue();
    return new DrawCompartmentPage(this.page, this.baseURL);
  }  
  async cancel() {
    await super.clickCancel();
  }
}