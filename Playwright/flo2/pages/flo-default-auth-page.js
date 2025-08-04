//@ts-check
//flo-default-auth-page.js
const { expect, default: test } = require('@playwright/test');
const AxeBuilder = require('@axe-core/playwright').default; 
const wcagTags = require('../dataprovider/wcag-tags');
exports.FloDefaultAuthPage = class FloDefaultAuthPage {

  
  #cancelLink;
  #continueButton;
  #errorSummaryLocator
  #errorDetailList
  #breadcrumbList
  #breadcrumbListItemText
  #govukheading
  #navigationLocator
  #navigationItemsLocator
  #userNameLocator

  /**
   * @param {import('@playwright/test').Page} page
   */
   constructor(page, baseURL) {
    this.page = page;
    this.baseURL = baseURL;
    this.#govukheading = page.locator('h1.govuk-heading-xl');
    this.mainContentDiv = page.locator('div.govuk-width-container');
    this.signOutLink =  page.locator('a#signout', { hasText: 'Sign out' })
    this.#errorSummaryLocator = page.locator('#error-summary-title');
    this.#errorDetailList = page.locator('ul.govuk-error-summary__list > li > a');
    this.#continueButton = page.locator('button[type="submit"]');
    this.#cancelLink = page.locator('a.govuk-link', { hasText: 'Cancel' });
    this.#breadcrumbList = page.locator('ol.govuk-breadcrumbs__list >li');
    this.#breadcrumbListItemText = page.locator('.govuk-breadcrumbs__link');
    this.#userNameLocator = page.locator('[data-test-id=userfullname]');
    this.#navigationLocator = page.locator('#navigation > .govuk-header__navigation-item');
    this.#navigationItemsLocator = page.locator('#navigation >> a.govuk-header__link');
  }

  async hasExpectedUserNameInformation(expectedUserInformation)
  {
    await expect(this.#userNameLocator).toHaveText(expectedUserInformation)
  }

  async hasGuidanceDetails(guidanceLinkText){
    const x = this.page.locator('details > summary > span',  { hasText: guidanceLinkText });
    x.click();
  }

  //todo https://playwright.dev/docs/accessibility-testing#exporting-scan-results-as-a-test-attachment
  async assertAccessibilityChecks(){
    let  x = this.page;
    var accessibilityScanResults = await new AxeBuilder({ x })
    .withTags(wcagTags.Tags)
    .analyze();
    expect(accessibilityScanResults.violations).toEqual([]);

//
  }

  /**
   * @param {any[]} expectedBreadcrumbs
   */
  async hasExpectedBreadCrumbs(expectedBreadcrumbs){
    await expect(this.#breadcrumbList).toHaveCount(expectedBreadcrumbs.length);
    var count=0;
    expectedBreadcrumbs.forEach(async crumb => {
      //await expect(this.#breadcrumbListItemText.nth(count)).toContainText(crumb);
      count++;
    });
  }

  async hasExpectedNavigationMenuLinkText(expectedDisplayedLinks){
    await expect (this.#navigationLocator).toHaveCount(expectedDisplayedLinks.length);
    var count=0;
    expectedDisplayedLinks.forEach(async navLinkText => {
      //await expect(this.#navigationItemsLocator.nth(count)).toContainText(navLinkText);
      count++;
    });
  }


  async hasCorrectPageHeading(expectedPageHeading){
    await expect(this.#govukheading).toHaveText(expectedPageHeading);
  };

  async logout() {
    await this.signOutLink.click();
    await expect(this.page).toHaveURL(this.baseURL);
  }

  //Yes - will need to handle an array of errors, fix when required:
  async expectErrorSummaryHavingText(errorText){
    await expect(this.#errorSummaryLocator).toHaveText("There is a problem");
    await expect(this.#errorDetailList).toHaveText(errorText);
  }

  async clickCancel(){
    await this.#cancelLink.click();
  }

  async clickContinue(){
    await this.#continueButton.click();
  }
}