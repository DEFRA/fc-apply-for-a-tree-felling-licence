//@ts-check
//flo-default-unauth-page.js

const { expect } = require('@playwright/test');
const { CookiesPage } = require('../pages/cookies-page');
const { TaskRedirectionPage } = require('../pages/task-redirection-page')
const { PrivacyPolicyPage } = require('../pages/privacy-policy-page')
const { AccessibilityPage } = require('../pages/accessibility-page')

exports.FloDefaultUnauthPage = class FloDefaultUnauthPage {

#hideCookieMessage

  /**
   * @param {import('@playwright/test').Page} page
   */
  constructor(page, baseURL) {
    this.page = page;
    this.baseURL = baseURL;

    this.govukheading = page.locator('h1.govuk-heading-xl');

    this.header = page.locator('.govuk-header', { hasText: 'Apply for a tree felling licence' });
    this.footer = page.locator('footer >> a'); 
    this.privacyPageLink = page.locator('footer >> a', { hasText: 'Privacy policy' });
    this.accessibilityPageLink = page.locator('footer >> a', { hasText: 'Accessibility statement' });
    this.cookiesPageLink = page.locator('footer >> a', { hasText: 'Cookies' });
    this.signOutLink =  page.locator('a.govuk-header__link', { hasText: 'Sign out' })
    this.serviceStartButton = page.locator('a.govuk-button--start');
    
    this.cookieBannerDiv = page.locator('div#cookieMessage');
    this.acceptAnalyticsCookiesButton = page.locator('button#acceptButton');
    this.rejectAnalyticsCookiesButton = page.locator('button#rejectButton');
    this.#hideCookieMessage = page.locator('button#acceptHideCookieButton');
    this.cookieAcceptedConfirmationText = page.locator('div.govuk-cookie-banner__message#accepted p.govuk-body');
    this.cookieRejectedConfirmationText = page.locator('div.govuk-cookie-banner__message#rejected p.govuk-body');
  }

  //should be using methods on whichever page object model we are wanting, i.e. WOHomePage.createProperty()
  async open() {
    await this.page.goto(this.baseURL);
    return this;
  }

  async clickLogin() {
    await this.serviceStartButton.click();

    const newPage = new TaskRedirectionPage(this.page, this.baseURL);
    return newPage.selectSignInToMyAccountRadio();
  }

  async clickStartHere() {
    this.serviceStartButton.click();
    return new TaskRedirectionPage(this.page, this.baseURL);
  }

  async privacyPOM(){
    await this.privacyPageLink.click(); //todo POM
    const newPage = await this.page.context().waitForEvent('page');
    await newPage.waitForLoadState(); // Ensure the new page is loaded
    return new PrivacyPolicyPage(newPage);
  }

  async accessibilityPOM(){
    await this.accessibilityPageLink.click(); //todo POM
    const newPage = await this.page.context().waitForEvent('page');
    await newPage.waitForLoadState(); // Ensure the new page is loaded
    return new AccessibilityPage(newPage);
  }

  async ClickCookiesFooterLink(){
    await this.cookiesPageLink.click();
    const newPage = await this.page.context().waitForEvent('page');
    await newPage.waitForLoadState(); // Ensure the new page is loaded
    return new CookiesPage(newPage);
  }

  async acceptAnalyticsCookies(){
    await this.acceptAnalyticsCookiesButton.click();
    await expect(this.cookieAcceptedConfirmationText).toContainText("You have accepted analytics cookies.  You can change your cookie settings at any time.");
  }

  async hideCookieMessage(){
    await this.#hideCookieMessage.click();   
  }

  async rejectAnalyticsCookies(){
    await this.rejectAnalyticsCookiesButton.click();
    await expect(this.cookieRejectedConfirmationText).toContainText("You’ve rejected analytics cookies. You can change your cookie settings at any time.");
  }

  async AssertIsDisplayingCookieBanner(){
    await expect(this.cookieBannerDiv).toBeVisible();
  }
  async AssertIsNotDisplayingCookieBanner(){
    await expect(this.cookieBannerDiv).not.toBeVisible();
  }  
}