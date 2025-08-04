//@ts-check
//draw-compartment-page.js
const { FloDefaultAuthPage } = require('./flo-default-auth-page');

exports.DrawCompartmentPage = class DrawCompartmentPage extends FloDefaultAuthPage {

  #polygonToolLocator;

  /**
   * @param {import('@playwright/test').Page} page
   * @param {any} baseURL
   */
  constructor(page, baseURL) {
    super(page, baseURL);
    this.page = page;
    this.baseURL = baseURL;
    this.#polygonToolLocator = page.locator('[aria-label="Draw a polygon"]');
  }

  // /**
  //  * @param {{ x: number; y: number; }[]} pointsArray
  //  */
  // async drawPolygon(pointsArray){
  //   // await this.page.locator('div#viewDiv');
  //   // await this.page.locator('div.esri-overlay-surface');
  // //  await this.#clickPolygonTool();
  // //  await this.#clickPoints(pointsArray);
  // }

  async clickPolygonTool(){
    await this.#polygonToolLocator.click();
  }

  // asyncUppercase = item =>
  // new Promise(resolve =>
  //   setTimeout(
  //     () => resolve(item.toUpperCase()),
  //     Math.floor(Math.random() * 1000)
  //   )
  // );
  //  uppercaseItems = async () => {
  //   const items = ['a', 'b', 'c'];
  //   for (item of items) {
  //     const uppercaseItem = await asyncUppercase(item);
  //     console.log(uppercaseItem);
  //   }
  
  //   console.log('Items processed');
  // };

  /**
   * @param {{ x: number; y: number; }[]} pointsArray
   */
  async clickPoints(pointsArray){
 
    console.log('entered');
    for (let i=0; i< pointsArray.length; i++){
      const pointToClick = pointsArray[i];
      await // Click canvas
      this.page.locator('canvas').click({
        position: {
          x: pointToClick.x,
          y: pointToClick.y
        }//, force : true      
      });
      await this.page.waitForTimeout(1500);
    }
    console.log('done');
//     for await(const pointToClick of pointsArray)
//     {
// //        console.log(pointToClick.x, pointToClick.y);

//       await // Click canvas
//       this.page.locator('canvas').click({
//         position: {
//           x: pointToClick.x,
//           y: pointToClick.y
//         }, force : true      
//       }); 
//     }

    // pointsArray.forEach(async pointToClick => {
    //   // Click canvas
    //   this.page.locator('canvas').click({
    //     position: {
    //       x: pointToClick.x,
    //       y: pointToClick.y
    //     }, force : true      
    //   });
    // });
  }

  async continue() {
    console.log('clicked continue');
    await super.clickContinue(); //todo wrong selector in super.. so have a dedicated one for "save and continue" text
    //at this point - do not return PropertyProfilePage, as circular dependancy, should already have this page object anyhow in the test class. 
  }
  
  async cancel() {
    await super.clickCancel();
  }
}