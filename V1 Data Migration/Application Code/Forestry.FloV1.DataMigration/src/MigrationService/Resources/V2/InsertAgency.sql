INSERT INTO "Applicants"."Agency"(
"Address_Line1", 
"Address_Line2", 
"Address_Line3", 
"Address_Line4", 
"Address_PostalCode", 
"ContactEmail", 
"ContactName", 
"IsOrganisation", 
"OrganisationName", 
"ShouldAutoApproveThinningApplications")
VALUES(
@addressLine1, 
@addressLine2, 
@addressLine3, 
@addressLine4, 
@postalCode, 
@email, 
@contactName, 
@isOrganisation, 
@organisationName, 
false) 
RETURNING "Id";