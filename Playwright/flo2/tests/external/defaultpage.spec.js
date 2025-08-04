//@ts-check
// defaultpage.spec.js
const { test, expect } = require('@playwright/test');
const { FloDefaultUnauthPage } = require('../../pages/flo-default-unauth-page');

test.describe.configure({ mode: 'parallel' });

  test('display default page', async ({ page, baseURL }) => {
    const floDefaultUnauthPage = await new FloDefaultUnauthPage(page, baseURL).open();
  
    await expect(floDefaultUnauthPage.header).toContainText([
      'Apply for a tree felling licence'
    ]);

    await expect(floDefaultUnauthPage.footer).toContainText([
      'Accessibility statement',
      'Cookies',
      'Privacy policy'
    ]);

    await expect(floDefaultUnauthPage.govukheading).toContainText("Apply for a tree felling licence");

    //assert should have service start button:
    
  });
 
//todo assert content.Cookies() changes..  
test.describe("cookie policy tests", ()=>{
  test('shows cookie policy banner by default', async ({ page, baseURL }) => {
    const floDefaultUnauthPage = await new FloDefaultUnauthPage(page, baseURL).open();
    await floDefaultUnauthPage.AssertIsDisplayingCookieBanner();
  });

  test('clicking accept removes cookie banner', async ({page, baseURL }) => {
    const floDefaultUnauthPage = await new FloDefaultUnauthPage(page, baseURL).open();
    await floDefaultUnauthPage.acceptAnalyticsCookies();
    await floDefaultUnauthPage.AssertIsNotDisplayingCookieBanner();
  });

  test('clicking reject removes cookie banner', async ({context, page, baseURL }) => {
    const floDefaultUnauthPage = await new FloDefaultUnauthPage(page, baseURL).open();
    await floDefaultUnauthPage.rejectAnalyticsCookies();
    await floDefaultUnauthPage.AssertIsNotDisplayingCookieBanner();
  });
});

test.describe("can use footer links", () => {
  test('can click through to Privacy Page', async ({ page, baseURL }) => {
    const floDefaultUnauthPage = await new FloDefaultUnauthPage(page, baseURL).open();
    const privacyPage = await floDefaultUnauthPage.privacyPOM();
    await expect(privacyPage.govukheading).toContainText("Privacy policy");
  });

  test('can click through to Accessibility Page', async ({ page, baseURL }) => {
    const floDefaultUnauthPage = await new FloDefaultUnauthPage(page, baseURL).open();
    const accessibilityPage = await floDefaultUnauthPage.accessibilityPOM();
    await expect(accessibilityPage.govukheading).toContainText("Accessibility statement for Apply for a tree felling licence");
  });

  test('can click through to Cookies Page', async ({ page, baseURL }) => {
    const floDefaultUnauthPage = await new FloDefaultUnauthPage(page, baseURL).open();
    const cookiesPage = await floDefaultUnauthPage.ClickCookiesFooterLink();
    await expect(cookiesPage.govukheading).toContainText("Cookies");
  });
});