//@ts-check
// loginHelper.js
const { FloDefaultUnauthPage } = require('../flo-default-unauth-page');

exports.loginHelper = class loginHelper  {
    
    /**
     * @param {import("playwright-core").Page} page
     * @param {string | undefined} baseURL
     * @param {string} userEmail
     */
    static async loginAsNewFloUser(page, baseURL, userEmail){    
        const azureB2CLoginPage = await this.#accessLoginPage(page, baseURL);    
        return await azureB2CLoginPage.loginAsNewFloUser(userEmail);
    }   
    /**
     * @param {import("playwright-core").Page} page
     * @param {any} baseURL
     * @param {string} userEmail
     */
    static async loginAsWoodlandOwner(page, baseURL, userEmail){    
        const azureB2CLoginPage = await this.#accessLoginPage(page, baseURL);    
        return await azureB2CLoginPage.loginAsWoodlandOwner(userEmail);
    }   
    /**
     * @param {import("playwright-core").Page} page
     * @param {string | undefined} baseURL
     * @param {string} userEmail
     * @param {string} password
     */
    static async loginExpectingPasswordError(page, baseURL, userEmail, password){    
        const azureB2CLoginPage = await this.#accessLoginPage(page, baseURL);    
        return await azureB2CLoginPage.loginWithIncorrectPassword(userEmail, password);
    }   

    /**
     * @param {import("playwright-core").Page} page 
     * @param {*} baseURL 
     * @param {string} userEmail 
     * @returns 
     */
    static async loginExpectingError(page, baseURL, userEmail){    
        const azureB2CLoginPage = await this.#accessLoginPage(page, baseURL);    
        return await azureB2CLoginPage.loginExpectingError(userEmail);
    }   

    /**
     * @param {import("playwright-core").Page} page
     * @param {any} baseURL
     */
    static async #accessLoginPage(page, baseURL, acceptCookies = true){
        const floDefaultUnauthPage = await new FloDefaultUnauthPage(page, baseURL).open();

        if (acceptCookies){
            await floDefaultUnauthPage.acceptAnalyticsCookies();
            await floDefaultUnauthPage.hideCookieMessage();
        } 

        const azureB2CLoginPage = await floDefaultUnauthPage.clickLogin(); 
        return azureB2CLoginPage;
    }
}
