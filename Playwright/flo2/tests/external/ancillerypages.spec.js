//@ts-check
//ancillerypages.spec.js
const { test, expect } = require('@playwright/test');

const PagesWhichShouldHaveBackLinks = [
    '/home/cookies',
    '/home/privacy',
    '/home/accessibility',
    '/home/taskRedirection',
    '/home/accountHolderConfirmation',
    '/home/accountJustification'
    //etc
]

for (const pageUnderTest of PagesWhichShouldHaveBackLinks) {
    test(`can use back Links on ${pageUnderTest}`, async ({ page, baseURL }) => {

        //go to default page first (otherwise going back from the next page gives you the browser's default/blank page)
        await page.goto(baseURL);

        //Under playwright test execution the document.referrer in browser context is null without the following line
        //Which means it would fail to switch the Back link behaviour on the privacy page, and attempt a JS close window instead.
        await page.setExtraHTTPHeaders({Referer: baseURL});         

        await page.goto(baseURL + pageUnderTest);
        await expect.soft(page.locator('text=Back').last()).toBeVisible(); //feedback and back..
        await page.locator(".govuk-back-link").click();
        await expect.soft(page).toHaveURL(baseURL);
    });
}
