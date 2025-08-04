//@ts-check
// external-user-registration.spec.js
const { test, expect } = require('@playwright/test');
const { loginHelper } = require("../../pages/helpers/loginHelper");
const TestUserEmailForWoodlandOwner = "Flov2_Perf_4@qxlva.com";
const TestUserEmailForWoodlandOrg = "Flov2_Perf_3@qxlva.com";

/*
  For registration tests, need an external applicant user who:-
    - Has registered previously with AZB2C - satisfying MFA
    - Not accessed FLOv2 to submit any part of the signup process, or had their user account data wipped before this test)
*/

test.describe.configure({ mode: 'parallel' });

test.describe('can register as an external applicant', () => {
  test('register as individual woodland owner', async ({ page, baseURL }) => {

    test.slow();
    const registerAccountTypePage = await loginHelper.loginAsNewFloUser(page, baseURL, TestUserEmailForWoodlandOwner);
   
    //Account Type:
    await registerAccountTypePage.selectOwner();
    const RegisterOwenerTypePage = await registerAccountTypePage.continue();

    //Type of Owner
    await RegisterOwenerTypePage.selectIndividual();
    const registerPersonNamePage = await RegisterOwenerTypePage.continue();

    //Person Name
    await registerPersonNamePage.setFirstName(TestUserEmailForWoodlandOwner.split('@')[0]),
    await registerPersonNamePage.setLastName("TestWoodlandOwner")
    const registerPersonContactDetailsPage = await registerPersonNamePage.continue();

    //Person Contact Details
    await registerPersonContactDetailsPage.setAddressLine1("3A London Rd");
    await registerPersonContactDetailsPage.setAddressLine2("Warmley");
    await registerPersonContactDetailsPage.setAddressLine3("Bristol"); 
    await registerPersonContactDetailsPage.setAddressLine4("Avon"); 
    await registerPersonContactDetailsPage.setPostcode("BS30 5JB");
    await registerPersonContactDetailsPage.setTelephone("0330 333 3300");
    await registerPersonContactDetailsPage.setContactMobileTelephone("0777 777 777");  
    await registerPersonContactDetailsPage.setPreferredContactMethod("Email");
    const registerAccountSummaryPage = await registerPersonContactDetailsPage.continueAsWoodlandOwner();

    //Confirm Details
    await registerAccountSummaryPage.checkRole('Owner (individual)');
    await registerAccountSummaryPage.checkName('Flov2_Perf_4 TestWoodlandOwner');
    await registerAccountSummaryPage.checkAddress('3A London Rd\nWarmley\nBristol\nAvon\nBS30 5JB');
    await registerAccountSummaryPage.checkContactTelephone('0330 333 3300');
    await registerAccountSummaryPage.checkContactMobTelephone('0777 777 777');
    const termsAndConditionsPage = await registerAccountSummaryPage.continue();

    //Terms and Conditions Page
    //todo check can click on PP and opens in new tab..
    // // Check text=Accept Privacy Policy (opens in new tab) Accept Terms and Conditions of Use >> input[name="AcceptsTermsAndConditions\.AcceptsPrivacyPolicy"]
    // await page.locator('text=Accept Privacy Policy (opens in new tab) Accept Terms and Conditions of Use >> input[name="AcceptsTermsAndConditions\\.AcceptsPrivacyPolicy"]').check();
    // // Check text=Accept Privacy Policy (opens in new tab) Accept Terms and Conditions of Use >> input[name="AcceptsTermsAndConditions\.AcceptsTermsAndConditions"]
    // await page.locator('text=Accept Privacy Policy (opens in new tab) Accept Terms and Conditions of Use >> input[name="AcceptsTermsAndConditions\\.AcceptsTermsAndConditions"]').check();

    await termsAndConditionsPage.assertAcceptButtonIsDisabled();
    await termsAndConditionsPage.acceptPrivacyPolicy(); 
    await termsAndConditionsPage.assertAcceptButtonIsDisabled();
    await termsAndConditionsPage.acceptTermsAndConditions();
    await termsAndConditionsPage.assertAcceptButtonIsEnabled();
    const woodlandOwnerHomePage = await termsAndConditionsPage.clickAcceptAsWoodlandOwner();

    //Basic Checks on Woodland Owner Homepage
    await woodlandOwnerHomePage.hasCorrectPageHeading("Your applications");
    await woodlandOwnerHomePage.hasExpectedUserNameInformation(TestUserEmailForWoodlandOwner.split('@')[0] + " TestWoodlandOwner")
    await woodlandOwnerHomePage.hasExpectedNavigationMenuLinkText(["Home","Woodlands","Your profile","Sign out"]);

  });
  
  test('register as organisation woodland owner', async ({ page, baseURL }) => {
    test.slow();
    //needs to be a diff user here
    const registerAccountTypePage = await loginHelper.loginAsNewFloUser(page, baseURL, TestUserEmailForWoodlandOrg);
   
    //Account Type:
    await registerAccountTypePage.selectOwner();
    const RegisterOwenerTypePage = await registerAccountTypePage.continue();

    //Type of Owner
    await RegisterOwenerTypePage.selectOrganisation();
    const registerPersonNamePage = await RegisterOwenerTypePage.continue();

    //Person Name
    await registerPersonNamePage.setFirstName(TestUserEmailForWoodlandOrg.split('@')[0]),
    await registerPersonNamePage.setLastName("Test WoodlandOrg")
    const registerPersonContactDetailsPage = await registerPersonNamePage.continue();
 
     //Person Contact Details
    await registerPersonContactDetailsPage.setAddressLine1("6B London Rd");
    await registerPersonContactDetailsPage.setAddressLine2("Warmley");
    await registerPersonContactDetailsPage.setAddressLine3("Bristol");
    await registerPersonContactDetailsPage.setAddressLine4("Avon"); 
    await registerPersonContactDetailsPage.setPostcode("BS30 5JB");
    await registerPersonContactDetailsPage.setTelephone("0330 333 3300");
    await registerPersonContactDetailsPage.setContactMobileTelephone(""); // test can be empty
    await registerPersonContactDetailsPage.setPreferredContactMethod("Email");
    const registerOrganisationDetailsPage = await registerPersonContactDetailsPage.continueAsWoodlandOrganisation();

    //Organisation details..
    await registerOrganisationDetailsPage.setOrganisationName('Hayden');
    await registerOrganisationDetailsPage.setContactName('Louisa');
    await registerOrganisationDetailsPage.setContactEmailAddress('lousia.niu@qxlva.com');
    await registerOrganisationDetailsPage.setContactMobileTelephone('0330 333 3300');
    await registerOrganisationDetailsPage.setOrganisationAddressLine1('Woodland 123');
    await registerOrganisationDetailsPage.setOrganisationAddressLine2('White Edge');
    await registerOrganisationDetailsPage.setOrganisationAddressLine3('Bristol');
    await registerOrganisationDetailsPage.setOrganisationAddressLine4(''); //is optional
    await registerOrganisationDetailsPage.setOrganisationAddressPostcode('BS1 2EQ');
    await registerOrganisationDetailsPage.CheckContactAddressMatchesOrganisationRegisteredAddress();
    //todo - the additional fields, if contact blocks are different
    const registerAccountSummaryPage = await registerOrganisationDetailsPage.continueAsWoodlandOrganisation();
    
    await registerAccountSummaryPage.checkRole('Owner (organisation)');
    await registerAccountSummaryPage.checkName('Flov2_Perf_3 Test WoodlandOrg');
    await registerAccountSummaryPage.checkAddress('6B London Rd\nWarmley\nBristol\nAvon\nBS30 5JB');
    await registerAccountSummaryPage.checkContactTelephone('0330 333 3300');
    await registerAccountSummaryPage.checkContactMobTelephone('');

    await registerAccountSummaryPage.checkOrganisationName('Hayden');
    await registerAccountSummaryPage.checkOrganisationContactName('Louisa');
    await registerAccountSummaryPage.checkOrganisationContactEmail('lousia.niu@qxlva.com');
    await registerAccountSummaryPage.checkOrganisationRegisteredAddress('Woodland 123\nWhite Edge\nBristol\nBS1 2EQ');
    await registerAccountSummaryPage.checkOrganisationContactAddress('Woodland 123\nWhite Edge\nBristol\nBS1 2EQ');

    //T&Cs
    const termsAndConditionsPage = await registerAccountSummaryPage.continue();
    await termsAndConditionsPage.acceptPrivacyPolicy(); 
    await termsAndConditionsPage.acceptTermsAndConditions();
    const woodlandOrganisationHomePage = await termsAndConditionsPage.clickAcceptAsWoodlandOrganisation();

    //Basic Checks on Woodland Owner Homepage
    await woodlandOrganisationHomePage.hasCorrectPageHeading("Your applications");
    await woodlandOrganisationHomePage.hasExpectedUserNameInformation(TestUserEmailForWoodlandOrg.split('@')[0] + " Test WoodlandOrg")
    //await woodlandOrganisationHomePage.hasExpectedBreadCrumbs(["Home"]);
    await woodlandOrganisationHomePage.hasExpectedNavigationMenuLinkText(["Home","Woodlands","Your profile","Sign out"]);
 });

  test.skip('register as individual agent', async ({ page, baseURL }) => {
    test.slow();
  });

  test.skip('register as organisation agent', async ({ page, baseURL }) => {
    test.slow();
  });

  test.skip('register as tenant yes description of crown land', async ({ page, baseURL }) => {
    test.slow();
  });

  test.skip('register as trustee trust', async ({ page, baseURL }) => {
    test.slow();
  });

  test.skip('register as organisational representative trust', async ({ page, baseURL }) => {
    test.slow();
  });

})
