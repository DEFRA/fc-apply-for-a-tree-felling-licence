//@ts-check
// login.spec.js
const { test, expect } = require('@playwright/test');
const { loginHelper } = require("../../pages/helpers/loginHelper");
const TestUserEmail = "Flov2_Perf_2@qxlva.com";
const UnknownUserEmail = "unknown.user@qxlva.com"

test.describe.configure({ mode: 'parallel' });

  test('can login to service', async ({ page, baseURL }) => {
    const registerAccountTypePage = await loginHelper.loginAsNewFloUser(page, baseURL, TestUserEmail);
    await expect(registerAccountTypePage.signOutLink).toBeVisible();    
  });

  test('can login and then logout of service', async ({ page, baseURL }) => {
    test.slow();
    const registerAccountTypePage = await loginHelper.loginAsNewFloUser(page, baseURL, TestUserEmail);
    await expect(registerAccountTypePage.signOutLink).toBeVisible();    
    await registerAccountTypePage.logout(); 
  });

  test('cannot login with an incorrect password', async ({ page, baseURL }) => {
    const azureB2CLoginPage = await loginHelper.loginExpectingPasswordError(page, baseURL, TestUserEmail, "junkpassword");
    await azureB2CLoginPage.expectErrorSummaryHavingText("Your details are incorrect");
  });

  test('cannot login with an unknown email address', async ({ page, baseURL }) => {
    const azureB2CLoginPage = await loginHelper.loginExpectingError(page, baseURL, UnknownUserEmail);
    await azureB2CLoginPage.expectErrorSummaryHavingText("Your details are incorrect");
  });
