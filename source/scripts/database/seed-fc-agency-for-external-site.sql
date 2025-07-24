--See https://quicksilva.atlassian.net/browse/FLOV2-750

--The FC Agency Account - the only Agency account where IsFcAgency is set to true.
INSERT INTO "Applicants"."Agency"(
	"Id", 
	"Address_Line1", 
	"Address_Line2", 
	"Address_Line3", 
	"Address_Line4", 
	"Address_PostalCode", 
	"ContactEmail", 
	"ContactName", 
	"OrganisationName", 
	"ShouldAutoApproveThinningApplications", 
	"IsFcAgency",
	"IsOrganisation"
)
VALUES (
	uuid_generate_v4(), 
	'620 Bristol Business Park', 
	'Coldharbour Lane', 
	'Bristol',
	'United Kingdom', 
	'BS16 1EJ', 
	'contactEmail', 
	'contactName', 
	'Forestry Commission', 
	false, 
	true,
	true
);

-- Agent user account for the FC Agency. (QXLVA)
-- This is the user which can then create all other FC User accounts for the above Agency entity.
INSERT INTO "Applicants"."UserAccount"(
	"Id", 
	"IdentityProviderId", 
	"AccountType", 
	"Title", 
	"FirstName", 
	"LastName", 
	"Email", 
	"PreferredContactMethod", 
	"ContactAddress_Line1", 
	"ContactAddress_Line2", 
	"ContactAddress_Line3", 
	"ContactAddress_Line4", 
	"ContactAddress_PostalCode", 
	"ContactTelephone", 
	"ContactMobileTelephone", 
	"Status", 
	"DateAcceptedTermsAndConditions", 
	"DateAcceptedPrivacyPolicy", 
	"AgencyId")
( SELECT 
	uuid_generate_v4(), 
	'e4e4a959-006a-49a0-a478-955b3bf5107c', --IdentityProviderId for email account beneath (paul.winslade+fcagency@qxlva.com):
	4, --(AccountTypeExternal.FcUser) 
	'No Title', 
	'itsupport', 
	'itsupport', 
	'paul.winslade+fcagency@qxlva.com', --email --todo ITSUPPORT@QXLVA.COM !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
	'Email',
	'A1 Methuen Park',
	'Chippenham',
	'Wiltshire',
	'United Kingdom',
	'SN14 0GT',
	'01249 751000',
	'01249 751000', 
	'Active', 
	current_timestamp, 
	current_timestamp, 
	aa."Id"
FROM "Applicants"."Agency" aa where "IsFcAgency" = true limit 1);


