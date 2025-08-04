//@ts-check
// create-property.spec.js
const { test, expect } = require('@playwright/test');
const AxeBuilder = require('@axe-core/playwright').default; 
const { loginHelper } = require("../../pages/helpers/loginHelper");
const wcagTags = require('../../dataprovider/wcag-tags');

const TestUserEmail = "Flov2_Perf_2@qxlva.com";
const propertyName = "Shabbington";
const nearestTown = "Bristol";
const compartmentName ="1";
const subCompartmentname ="a";
const polygonRingPoints = [{x:227, y:93},{x:327, y:393},{x:427, y:193},{x:227, y:93}];

test.describe('create properties and compartments', () => {

//cancel before submit property
//test with/without plan references
//test validations
//unique prop name etc

  test.skip('create a property with a compartment', async ({ page, baseURL }) => {

    const woodlandOwnerHomepage = await loginHelper.loginAsWoodlandOwner(page, baseURL, TestUserEmail);  
    
    const addPropertyPage = await woodlandOwnerHomepage.addPropertyClick(); 
    await addPropertyPage.hasExpectedBreadCrumbs(["Home", "Property"]); 
    await addPropertyPage.setPropertyName(propertyName);
    await addPropertyPage.setNearestTown(nearestTown);
    await addPropertyPage.hasGuidanceDetails("What is a woodland management plan?");
    await addPropertyPage.hasGuidanceDetails("What is a woodland certification scheme?");
    await addPropertyPage.hasWoodlandManagementPlan("FS2022-01");
    await addPropertyPage.hasWoodlandCertScheme("scheme-01");
    await addPropertyPage.uncheckHasWoodCertScheme();

    const propertyProfilePage = await addPropertyPage.continue();
    await propertyProfilePage.hasExpectedBreadCrumbs(["Home", propertyName]);

    const addCompartmentDetailsPage = await propertyProfilePage.addCompartment();
    await addCompartmentDetailsPage.hasExpectedBreadCrumbs(["Home", propertyName, "Compartment"]);
    await addCompartmentDetailsPage.hasGuidanceDetails("Help with compartment");
    await addCompartmentDetailsPage.setCompartmentName(compartmentName);
    await addCompartmentDetailsPage.setSubCompartmentName(subCompartmentname);
    await addCompartmentDetailsPage.setWoodlandName("Bernwood");
    await addCompartmentDetailsPage.setDesignation("Clear Felling");

    const drawCompartmentPage = await addCompartmentDetailsPage.continue();
    await drawCompartmentPage.hasExpectedBreadCrumbs(["Home", propertyName, compartmentName+subCompartmentname]);
    await drawCompartmentPage.hasGuidanceDetails("TBC guidance");

    //everything below is going to fail, as need to draw on the canvas, which is flakey at best, until find best way.

    //await drawCompartmentPage.hasCorrectPageHeading();//

    //await drawCompartmentPage.drawPolygon(polygonRingPoints); 
    await drawCompartmentPage.clickPolygonTool();
    //await drawCompartmentPage.clickPoints(polygonRingPoints);

    // Resize viewport for individual page
    await page.setViewportSize({ width: 1600, height: 1200 });
    const canvasLocator = page.locator('canvas');


    // Click div[role="application"]
    await page.locator('div[role="application"]').click();

    // Click [aria-label="Default map view"]
    await page.locator('[aria-label="Default map view"]').click();


    // Using ‘page.mouse’ to trace a 100x100 square.
    //await page.mouse.move(0, 0);
    await page.mouse.click(527,493,{delay:500});
    await page.waitForTimeout(1000);

    await page.mouse.click(627,793,{delay:500});
    await page.waitForTimeout(1000);

    await page.mouse.click(727,593,{delay:500});
    await page.waitForTimeout(1000);

    await page.mouse.click(727,593,{delay:500});

    // await page.mouse.move(0, 100);
    // await page.mouse.move(100, 100);
    // await page.mouse.move(100, 0);
    // await page.mouse.move(0, 0);
    //await page.mouse.up();

    await page.waitForTimeout(1000);


    // await page.mouse.move(0, 0);
    // await page.mouse.down();
    // //await page.mouse.move(0, 100);
    // await page.mouse.move(227, 93);
    // //await page.mouse.move(100, 0);
    // //await page.mouse.move(0, 0);
    //await page.mouse.up();
      // await page.evaluate(() => {
      //   alert("alert");
      //   // var canvasElement = document.querySelector("canvas");
      //   // var context = canvasElement.getContext("2d");
      //   // context.beginPath();
      //   // context.moveTo(100, 100);
      //   // context.lineTo(100, 300);
      //   // context.lineTo(300, 300);
      //   // context.closePath();
      // })

    await canvasLocator.click({
      position: {
        x: 227,
        y: 93
      }
    });
    console.log('1');
    await page.waitForTimeout(1500);

    await canvasLocator.click({
      position: {
        x: 327,
        y: 393
      }
    });
    console.log('2');
    await page.waitForTimeout(1500);

    await canvasLocator.click({
      position: {
        x: 427,
        y: 193
      }
    });
    console.log('3');
    await page.waitForTimeout(1500);

    //in effect double clicking the last point to join up the poly
    await canvasLocator.click({
      position: {
        x: 427,
        y: 193
      }
    });
    console.log('4');
    await page.waitForTimeout(1500);

    await drawCompartmentPage.continue(); //is save/continue?

    //back to pp page, should display the compartment in the datatable, showing 1 to 1, and the compatment name with edit. etc.
    // and the image
  }); 
});