//@ts-check
//lis-internal-rest-api.spec.js

//see https://playwright.dev/docs/api/class-apirequestcontext

// Request context is reused by all tests in the file.
let apiContext;

const { test, expect } = require('@playwright/test');
const fs = require('fs')
const API_KEY_VALUE ='iazo54uwhDnydbAqbrHcDvUr4UTf4w5zr1hKwSm4JxJGNvCsS';
const LOCAL_DEBUG_BASE_URL = 'https://localhost:7254';

const PDF_FILE_PATH = 'test-resources/FLOv2IntegrationwithForesterESRIProposal.pdf';

const HTTP_STATUS_CODE_ACCEPTED = 201;
const HTTP_STATUS_CODE_BAD_REQUEST = 400;
const HTTP_STATUS_CODE_UNAUTHORIZED = 401;
const HTTP_STATUS_CODE_SERVER_ERROR = 500;

const KNOWN_APPLICATION_ID_IN_DRAFT_STATE = 'c9bff098-f508-4c34-9d80-b7a0387623fb';
const KNOWN_APPLICATION_ID_IN_REJECTED_STATE = 'E1EF6E7B-3F36-41F9-BA26-A5A6196CF943';
const UNKNOWN_APPLICATION_ID = '10000000-0000-0000-0000-000000000000';
const INVALID_APPLICATION_ID = 'abc123';

test.beforeAll(async ({ playwright, baseURL }) => {

  let intBaseUrl = baseURL?.replace('testflo','internaltestflo')
  apiContext = await playwright.request.newContext({
    ignoreHTTPSErrors : true,
    // All requests we send go to this API endpoint.
    baseURL: intBaseUrl, //LOCAL_DEBUG_BASE_URL, //intBaseUrl, 
    extraHTTPHeaders: {
        'Content-Type': 'application/pdf',
        'X-Api-Key': API_KEY_VALUE
    },
  });
})

test.afterAll(async ({ }) => {
    // Dispose all responses.
    await apiContext.dispose();
});

test('can add LIS doc when FLA exists and in draft state', async ({ page }) => {

  const PDF_FILE_STREAM = fs.createReadStream(PDF_FILE_PATH);

  const response = await apiContext.put(`/api/lis/${KNOWN_APPLICATION_ID_IN_DRAFT_STATE}`, {
    multipart: {
      name: 'filename',
      mimeType:'application/pdf',
      buffer : PDF_FILE_STREAM
    },  
  });    

  expect(response.ok()).toBeTruthy();
  expect(response.status()).toBe(HTTP_STATUS_CODE_ACCEPTED);

}); 

test('cannot add LIS doc when incorrect content-type', async ({ page }) => {
    
  const PDF_FILE_STREAM = fs.createReadStream(PDF_FILE_PATH);

  const response = await apiContext.put(`/api/lis/${KNOWN_APPLICATION_ID_IN_DRAFT_STATE}`, {
      headers: {
        'content-type': 'text/plain',
      },
      multipart: {
        name: 'filename',
        mimeType:'application/pdf',
        buffer : PDF_FILE_STREAM
      },
    });    
  
  expect(response.ok()).toBeFalsy();
  expect(response.status()).toBe(HTTP_STATUS_CODE_BAD_REQUEST);

}); 

test('cannot add LIS doc when FLA Id has incorrect format type', async ({ page }) => {

  const PDF_FILE_STREAM = fs.createReadStream(PDF_FILE_PATH);

  const response = await apiContext.put(`/api/lis/${INVALID_APPLICATION_ID}`, {
    multipart: {
      name: 'filename',
      mimeType:'application/pdf',
      buffer : PDF_FILE_STREAM
    }
  });    
  
  expect(response.ok()).toBeFalsy();
  expect(response.status()).toBe(HTTP_STATUS_CODE_BAD_REQUEST);

}); 

test('cannot add LIS doc when invalid API-Key value', async ({ page }) => {
    
  const PDF_FILE_STREAM = fs.createReadStream(PDF_FILE_PATH);

  const response = await apiContext.put(`/api/lis/${KNOWN_APPLICATION_ID_IN_DRAFT_STATE}`, {
      headers: {
        'X-Api-Key': 'yadda',
      },
      multipart: {
        name: 'filename',
        mimeType:'application/pdf',
        buffer : PDF_FILE_STREAM
      }
    });    
  
  expect(response.ok()).toBeFalsy();
  expect(response.status()).toBe(HTTP_STATUS_CODE_UNAUTHORIZED);

}); 

test('cannot add LIS doc when empty API-Key value', async ({ page }) => {

  const PDF_FILE_STREAM = fs.createReadStream(PDF_FILE_PATH);

  const response = await apiContext.put(`/api/lis/${KNOWN_APPLICATION_ID_IN_DRAFT_STATE}`, {
      headers: {
        'X-Api-Key': '',
      },
      multipart: {
        name: 'filename',
        mimeType:'application/pdf',
        buffer : PDF_FILE_STREAM
      }
    });    
  
  expect(response.ok()).toBeFalsy();
  expect(response.status()).toBe(HTTP_STATUS_CODE_UNAUTHORIZED);

}); 

test('cannot add LIS doc when no body content', async ({ page }) => {
    
  const response = await apiContext.put(`/api/lis/${KNOWN_APPLICATION_ID_IN_DRAFT_STATE}`);    

  expect(response.ok()).toBeFalsy();
  expect(response.status()).toBe(HTTP_STATUS_CODE_BAD_REQUEST);

}); 

test('cannot add LIS doc when FLA not found', async ({ page }) => {
    
  const PDF_FILE_STREAM = fs.createReadStream(PDF_FILE_PATH);

  const response = await apiContext.put(`/api/lis/${UNKNOWN_APPLICATION_ID}`, {
      multipart: {
        name: 'filename',
        mimeType:'application/pdf',
        buffer : PDF_FILE_STREAM
      }
  });    

  expect(response.ok()).toBeFalsy();
  expect(response.status()).toBe(HTTP_STATUS_CODE_SERVER_ERROR);

}); 

test('cannot add LIS doc when FLA has incorrect state to accept it', async ({ page }) => {
    
  const PDF_FILE_STREAM = fs.createReadStream(PDF_FILE_PATH);

  const response = await apiContext.put(`/api/lis/${KNOWN_APPLICATION_ID_IN_REJECTED_STATE}`, {
      multipart: {
        name: 'filename',
        mimeType:'application/pdf',
        buffer : PDF_FILE_STREAM
      }
  });    

  expect(response.ok()).toBeFalsy();
  expect(response.status()).toBe(HTTP_STATUS_CODE_SERVER_ERROR);

}); 
