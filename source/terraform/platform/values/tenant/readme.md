
## Create User Flow

1. Go to the Portal and find the relevant B2C tenant
2. Under the Policies section select User Flows
3. Click New user flow
4. Select the Sign up and sign in type with the Recommended version
5. Provide the desired name
6. At Section 2. Identity Providers - Select Email Signup
7. At section 5. User attributes and token claims - Select Email Address as a collect attribute
8. Click Create
9. Once created select your newly created User Flow
10. Under properties select On for Enable JavaScript enforcing page layout and under Password Configuration select Self-service password reset and set password complexity to Strong
11. Under Application claims select User's Object ID and Email Addresses
12. Under Page Layouts select Yes to Use custom page content and provide the relevant Custom paga URI for the environment for the folloing pages (note, versions listed are for EAPC/FLO2) - 
    - Unified sign up or sign in page v2.1.7
    - Local account sign up page v2.1.12
    - Multifactor authentication page using email v2.1.12
    - Error page v 1.2.3
    - Forgot password page v2.1.12
    - Change password page v2.1.12
13. Under API permissions click Grant admin consent for DEV FLO v2 (AZ ADB2C -> App Registrations -> Application -> API Permissions