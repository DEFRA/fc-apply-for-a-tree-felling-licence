//@ts-check
// public-facing-accessibility.spec.js
//
// see https://playwright.dev/docs/accessibility-testing
// 

const { test, expect } = require('@playwright/test');
const AxeBuilder = require('@axe-core/playwright').default; 
const wcagTags = require('../../dataprovider/wcag-tags');

const PublicFacingPagesToTest = [
    '/',
    '/home/signin',
    '/home/cookies',
    '/home/privacy',
    '/home/accessibility' 
]

test.describe('check accessibility of public facing pages', () => {
  for (const urlPath of PublicFacingPagesToTest) {
    test(`checking page ${urlPath}`, async ({ page, baseURL }) => {
      await page.goto(urlPath);
      const accessibilityScanResults = await new AxeBuilder({ page })
      .withTags(wcagTags.Tags)
      .analyze();
      expect(accessibilityScanResults.violations).toEqual([]);
    })
  }
});
