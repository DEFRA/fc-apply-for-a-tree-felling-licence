//@ts-check
// navigate-to-login.spec.js
const { test, expect } = require('@playwright/test');
const { FloDefaultUnauthPage } = require('../../pages/flo-default-unauth-page');

test.describe.configure({ mode: 'parallel' });

test.describe('can navigate to signup page', () => {
  
  test('via Sign into my Account', async ({ page, baseURL }) => {
    const redirectionPage = await setUpRedirectionPage(page, baseURL);
    const loginPage = await redirectionPage.selectSignInToMyAccountRadio();
    await expect(loginPage.signInheading).toBeVisible();
    const accountJustification = await loginPage.signUpLink();
    await expect(accountJustification.accountJustificationheading).toBeVisible();
    const signupPage = await accountJustification.selectCreateAccount();
    await expect(signupPage.signupheading).toBeVisible();
  });

  test('via Resume an Application', async ({ page, baseURL }) => {
    const redirectionPage = await setUpRedirectionPage(page, baseURL);
    const loginPage = await redirectionPage.selectResumeAnApplication();
    await expect(loginPage.signInheading).toBeVisible();
    const accountJustification = await loginPage.signUpLink();
    await expect(accountJustification.accountJustificationheading).toBeVisible();
    const signupPage = await accountJustification.selectCreateAccount();
    await expect(signupPage.signupheading).toBeVisible();
  });

  test('via Start a new Application, Yes Account holder', async ({ page, baseURL }) => {
    const redirectionPage = await setUpRedirectionPage(page, baseURL);
    const accountHolderConfirmationPage = await redirectionPage.selectStartNewApplication();
    const loginPage = await accountHolderConfirmationPage.selectYesAccountHolder();
    await expect(loginPage.signInheading).toBeVisible();
    const accountJustification = await loginPage.signUpLink();
    await expect(accountJustification.accountJustificationheading).toBeVisible();
    const signupPage = await accountJustification.selectCreateAccount();
    await expect(signupPage.signupheading).toBeVisible();
  });

  test('via Start a new Application, No Account holder', async ({ page, baseURL }) => {
    const redirectionPage = await setUpRedirectionPage(page, baseURL);
    const accountHolderConfirmationPage = await redirectionPage.selectStartNewApplication();
    const accountJustification = await accountHolderConfirmationPage.selectNoAccountHolder();
    await expect(accountJustification.accountJustificationheading).toBeVisible();
    const signupPage = await accountJustification.selectCreateAccount();
    await expect(signupPage.signupheading).toBeVisible();
  });
})

async function setUpRedirectionPage(page, baseURL) {
  const floDefaultUnauthPage = await new FloDefaultUnauthPage(page, baseURL).open();
  await floDefaultUnauthPage.acceptAnalyticsCookies();
  await floDefaultUnauthPage.hideCookieMessage();
  return await floDefaultUnauthPage.clickStartHere();
}

