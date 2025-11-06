delete from "Audit"."AuditEvent";
delete from "Applicants"."UserAccount";
delete from "Applicants"."WoodlandOwner";
delete from "Applicants"."Agency";
delete from "PropertyProfiles"."PropertyProfile";
delete from "PropertyProfiles"."Compartment";
delete from "FellingLicenceApplications"."AssigneeHistory";
delete from "FellingLicenceApplications"."Document";
delete from "FellingLicenceApplications"."FellingOutcome";
delete from "FellingLicenceApplications"."FellingSpecies";
delete from "FellingLicenceApplications"."ProposedFellingDetail";
delete from "FellingLicenceApplications"."RestockingOutcome";
delete from "FellingLicenceApplications"."RestockingSpecies";
delete from "FellingLicenceApplications"."StatusHistory";
delete from "FellingLicenceApplications"."ProposedRestockingDetail";
delete from "FellingLicenceApplications"."LinkedPropertyProfile";
delete from "FellingLicenceApplications"."SubmittedFlaPropertyCompartment";
delete from "FellingLicenceApplications"."SubmittedFlaPropertyDetail";
delete from "FellingLicenceApplications"."FellingLicenceApplicationStepStatus";
delete from "FellingLicenceApplications"."FellingLicenceApplication";
delete from "FellingLicenceApplications"."__EFMigrationsHistory";

/*
	seed data for API Tests, 

	1) registrered applicant (woodland owner) having:
		1 x draft application
		1 x submitted application
*/

/*
	schema : Applicants
*/

INSERT INTO "Applicants"."WoodlandOwner" ("Id","ContactAddress_Line1",
"ContactAddress_Line2","ContactAddress_Line3","ContactAddress_Line4", "ContactAddress_PostalCode","ContactEmail", "ContactName","IsOrganisation", "OrganisationName","OrganisationAddress_Line1", "OrganisationAddress_Line2", "OrganisationAddress_Line3","OrganisationAddress_Line4", "OrganisationAddress_PostalCode" ) 
VALUES ( 
	'd18a424a-4946-4e78-94b0-f56152186593','Shabbington and Waterperry Wood', 'Aylesbury', 'Oxford','','OX33 1BJ','louisa.niu@qxlva.com','','true','Bernwood Forest','Methuen South','Methuen Park','Chippenham','','SN14 OGT');

INSERT INTO "Applicants"."UserAccount" ("Id","IdentityProviderId", "AccountType", "Title","FirstName", "LastName","Email","PreferredContactMethod","ContactAddress_Line1",
"ContactAddress_Line2","ContactAddress_Line3","ContactAddress_Line4", "ContactAddress_PostalCode","ContactTelephone","ContactMobileTelephone", "Status",
"DateAcceptedTermsAndConditions", "WoodlandOwnerId", "DateAcceptedPrivacyPolicy")
VALUES ('8C87BA6B-91C8-4631-AC4B-B714F668A16F',
		'd64be4ad-24ec-48d9-88bd-cebc0fadd297',
		'1',
		'Judge',
		'Pref_1',
		'Flov2',
		'flov2_perf_2@qxlva.com',
		'Email',
		'Shabbington and Waterperry Wood',
		'Horton-cum-Studley',
		'Oxford',
		'Oxfordshire', 
		'OX33 1BJ', 
		'01296 625825','',
		'Active', 
		'2022-07-21 17:46:56.213259+01', 
		'd18a424a-4946-4e78-94b0-f56152186593',
		'2022-07-21 17:46:56.213259+01');



/*
	schema : PropertyProfiles
*/

-- Data for PropertyProfile
INSERT INTO "PropertyProfiles"."PropertyProfile" ("Id", "Name", "OSGridReference", "NearestTown", "HasWoodlandManagementPlan", "WoodlandManagementPlanReference", "IsWoodlandCertificationScheme", "WoodlandCertificationSchemeReference", "WoodlandOwnerId", "NameOfWood")VALUES ('e25cbc1d-4088-404a-a974-c63e02939de6', 'Hyrule Castle', NULL, 'Bristol', false, NULL, false, NULL, 'd18a424a-4946-4e78-94b0-f56152186593', 'Forest of Illusion3');

-- Data for Compartment
INSERT INTO "PropertyProfiles"."Compartment" ("Id", "CompartmentNumber", "SubCompartmentName", "TotalHectares", "Designation", "GISData", "PropertyProfileId") VALUES ('910200f0-ebac-48ea-b8aa-09207bcc1fe3', '3', 'a', 0.1446119449381902, NULL, '{"spatialReference":{"wkid":27700},"rings":[[[410469.54957596987,287821.21099951817],[410481.5821284441,287786.48436756496],[410448.97216739075,287786.48436756496],[410434.257696674,287847.17329102615],[410469.54957596987,287821.21099951817]]]}', 'e25cbc1d-4088-404a-a974-c63e02939de6');

/*
	schema : FellingLicenceApplications
*/

-- Sequence starting at 3 - as are 3 FLAs on the system, must set accordingly if we add more FLAs in to test data - or we get dupe business ids with any created during normal use of webapp.
SELECT pg_catalog.setval('"FellingLicenceApplications"."AppRefIdCounter2022"', 3, true);

-- Data for FellingLicenceApplication
INSERT INTO "FellingLicenceApplications"."FellingLicenceApplication" ("Id", "ApplicationReference", "CreatedTimestamp", "CreatedById", "ProposedFellingStart", "ProposedFellingEnd", "ActualFellingStart", "ActualFellingEnd", "Source", "ApproverId", "WoodlandOwnerId", "Measures", "ProposedTiming", "TermsAndConditionsAccepted", "FinalActionDate", "OSGridReference") VALUES ('940f3d72-06bf-4e42-9829-fd4c4b61e14d', '000/1/2022', '2022-10-18 16:37:43.214979+01', '8C87BA6B-91C8-4631-AC4B-B714F668A16F', '2023-12-12 00:00:00+00', '2023-12-14 00:00:00+00', NULL, NULL, 'ApplicantUser', NULL, 'd18a424a-4946-4e78-94b0-f56152186593', 'fghfgh', 'fhfgh', true, '2023-01-16 15:39:18.283067+00',NULL);
INSERT INTO "FellingLicenceApplications"."FellingLicenceApplication" ("Id", "ApplicationReference", "CreatedTimestamp", "CreatedById", "ProposedFellingStart", "ProposedFellingEnd", "ActualFellingStart", "ActualFellingEnd", "Source", "ApproverId", "WoodlandOwnerId", "Measures", "ProposedTiming", "TermsAndConditionsAccepted", "FinalActionDate", "OSGridReference") VALUES ('c9bff098-f508-4c34-9d80-b7a0387623fb', '000/2/2022', '2022-10-18 16:41:54.146478+01', '8C87BA6B-91C8-4631-AC4B-B714F668A16F', NULL, NULL, NULL, NULL, 'ApplicantUser', NULL, 'd18a424a-4946-4e78-94b0-f56152186593', NULL, NULL, false, NULL, NULL);

--rejected
INSERT INTO "FellingLicenceApplications"."FellingLicenceApplication" ("Id", "ApplicationReference", "CreatedTimestamp", "CreatedById", "ProposedFellingStart", "ProposedFellingEnd", "ActualFellingStart", "ActualFellingEnd", "Source", "ApproverId", "WoodlandOwnerId", "Measures", "ProposedTiming", "TermsAndConditionsAccepted", "FinalActionDate") VALUES ('E1EF6E7B-3F36-41F9-BA26-A5A6196CF943', '000/3/2022', '2022-10-18 16:37:43.214979+01', '8C87BA6B-91C8-4631-AC4B-B714F668A16F', '2023-12-12 00:00:00+00', '2023-12-14 00:00:00+00', NULL, NULL, 'ApplicantUser', NULL, 'd18a424a-4946-4e78-94b0-f56152186593', 'fghfgh', 'fhfgh', true, '2023-01-16 15:39:18.283067+00');

-- Data for AssigneeHistory
INSERT INTO "FellingLicenceApplications"."AssigneeHistory" ("Id", "FellingLicenceApplicationId", "AssignedUserId", "TimestampAssigned", "TimestampUnassigned", "Role") VALUES ('98b441d4-e2a7-4d73-8fcc-7c0f5f971ef4', '940f3d72-06bf-4e42-9829-fd4c4b61e14d', '8C87BA6B-91C8-4631-AC4B-B714F668A16F', '2022-10-18 16:37:43.216485+01', NULL, 0);
INSERT INTO "FellingLicenceApplications"."AssigneeHistory" ("Id", "FellingLicenceApplicationId", "AssignedUserId", "TimestampAssigned", "TimestampUnassigned", "Role") VALUES ('d7fc9146-289c-418e-a40c-d0c5ce549ff8', 'c9bff098-f508-4c34-9d80-b7a0387623fb', '8C87BA6B-91C8-4631-AC4B-B714F668A16F', '2022-10-18 16:41:54.146481+01', NULL, 0);
INSERT INTO "FellingLicenceApplications"."AssigneeHistory" ("Id", "FellingLicenceApplicationId", "AssignedUserId", "TimestampAssigned", "TimestampUnassigned", "Role") VALUES ('06DEEF77-23CE-41C6-8FCD-47FF8A590B32', 'E1EF6E7B-3F36-41F9-BA26-A5A6196CF943', '8C87BA6B-91C8-4631-AC4B-B714F668A16F', '2022-10-18 16:37:43.216485+01', NULL, 0);

-- Data for Documents
INSERT INTO "FellingLicenceApplications"."Document" ("Id", "FellingLicenceApplicationId", "CreatedTimestamp", "Purpose", "FileName", "FileSize", "FileType", "Description", "MimeType", "Location", "VisibleToApplicant", "VisibleToConsultee") VALUES ('dacca468-4f82-41d1-a386-a5f55dcd4ab7', 'c9bff098-f508-4c34-9d80-b7a0387623fb', '2022-10-20 08:08:45.652893+01', 'ExternalLisConstraintReport', 'LisReport.pdf', 422135, '.pdf', NULL, 'application/pdf', 'C:\temp\fla\c9bff098-f508-4c34-9d80-b7a0387623fb\ExternalLisConstraintReport\kjdahggh.nlx', true, true);

-- Data for FellingLicenceApplicationStepStatus
INSERT INTO "FellingLicenceApplications"."FellingLicenceApplicationStepStatus" ("Id", "ApplicationDetailsStatus", "SelectCompartmentsStatus", "OperationsStatus", "SupportingDocumentationStatus", "TermsAndConditionsStatus", "CompartmentFellingRestockingStatuses", "FellingLicenceApplicationId") VALUES ('aa7c02ae-ccae-4fad-a6fc-c819aaf5855c', true, true, true, true, true, '[{"CompartmentId":"910200f0-ebac-48ea-b8aa-09207bcc1fe3","Status":true}]', '940f3d72-06bf-4e42-9829-fd4c4b61e14d');
INSERT INTO "FellingLicenceApplications"."FellingLicenceApplicationStepStatus" ("Id", "ApplicationDetailsStatus", "SelectCompartmentsStatus", "OperationsStatus", "SupportingDocumentationStatus", "TermsAndConditionsStatus", "CompartmentFellingRestockingStatuses", "FellingLicenceApplicationId") VALUES ('9fc0abdf-ef19-412b-ae28-725565d489ad', true, NULL, NULL, true, NULL, '[]', 'c9bff098-f508-4c34-9d80-b7a0387623fb');
INSERT INTO "FellingLicenceApplications"."FellingLicenceApplicationStepStatus" ("Id", "ApplicationDetailsStatus", "SelectCompartmentsStatus", "OperationsStatus", "SupportingDocumentationStatus", "TermsAndConditionsStatus", "CompartmentFellingRestockingStatuses", "FellingLicenceApplicationId") VALUES ('0519E627-06D9-4CE4-976F-95A603A71B65', true, true, true, true, true, '[{"CompartmentId":"910200f0-ebac-48ea-b8aa-09207bcc1fe3","Status":true}]', 'E1EF6E7B-3F36-41F9-BA26-A5A6196CF943');

-- Data for LinkedPropertyProfile
INSERT INTO "FellingLicenceApplications"."LinkedPropertyProfile" ("Id", "PropertyProfileId", "FellingLicenceApplicationId") VALUES ('229ef2fe-4b02-448d-ab8a-f85c47ed2f6a', 'e25cbc1d-4088-404a-a974-c63e02939de6', '940f3d72-06bf-4e42-9829-fd4c4b61e14d');
INSERT INTO "FellingLicenceApplications"."LinkedPropertyProfile" ("Id", "PropertyProfileId", "FellingLicenceApplicationId") VALUES ('20409d19-0012-4a41-ae63-bccbdbe43063', 'e25cbc1d-4088-404a-a974-c63e02939de6', 'c9bff098-f508-4c34-9d80-b7a0387623fb');
INSERT INTO "FellingLicenceApplications"."LinkedPropertyProfile" ("Id", "PropertyProfileId", "FellingLicenceApplicationId") VALUES ('57F3F524-3D55-4469-952D-98C7995B3432', 'e25cbc1d-4088-404a-a974-c63e02939de6', 'E1EF6E7B-3F36-41F9-BA26-A5A6196CF943');

-- Data for FellingOutcome

-- Data for ProposedFellingDetail
INSERT INTO "FellingLicenceApplications"."ProposedFellingDetail" ("Id", "LinkedPropertyProfileId", "PropertyProfileCompartmentId", "OperationType", "AreaToBeFelled", "NumberOfTrees", "TreeMarking", "IsPartOfTreePreservationOrder", "IsWithinConservationArea") VALUES ('24117b7a-f1b4-4aed-b51d-65df33631f84', '229ef2fe-4b02-448d-ab8a-f85c47ed2f6a', '910200f0-ebac-48ea-b8aa-09207bcc1fe3', 'ClearFelling', 0.14, 1, '1', false, false);
INSERT INTO "FellingLicenceApplications"."ProposedFellingDetail" ("Id", "LinkedPropertyProfileId", "PropertyProfileCompartmentId", "OperationType", "AreaToBeFelled", "NumberOfTrees", "TreeMarking", "IsPartOfTreePreservationOrder", "IsWithinConservationArea") VALUES ('1148C1F8-A4B8-446D-BE8F-93975CD56395', '229ef2fe-4b02-448d-ab8a-f85c47ed2f6a', '910200f0-ebac-48ea-b8aa-09207bcc1fe3', 'ClearFelling', 0.14, 1, '1', false, false);

-- Data for FellingSpecies
INSERT INTO "FellingLicenceApplications"."FellingSpecies" ("Id", "ProposedFellingDetailsId", "Species", "Percentage", "Volume") VALUES ('a7c61b89-6d73-4c77-a39a-4660b7bbefbf', '24117b7a-f1b4-4aed-b51d-65df33631f84', 'ALW', 100, 1);
INSERT INTO "FellingLicenceApplications"."FellingSpecies" ("Id", "ProposedFellingDetailsId", "Species", "Percentage", "Volume") VALUES ('EFAD4101-678D-4D3B-BEDF-6745BEB74FE0', '24117b7a-f1b4-4aed-b51d-65df33631f84', 'ALW', 100, 1);

-- Data for Name: ProposedRestockingDetail
INSERT INTO "FellingLicenceApplications"."ProposedRestockingDetail" ("Id", "LinkedPropertyProfileId", "PropertyProfileCompartmentId", "RestockingProposal", "Area", "PercentageOfRestockArea", "RestockingDensity") VALUES ('121c3e3b-abe8-4c74-90b5-d8a6141f5fb7', '229ef2fe-4b02-448d-ab8a-f85c47ed2f6a', '910200f0-ebac-48ea-b8aa-09207bcc1fe3', 'None', NULL, NULL, 0);
INSERT INTO "FellingLicenceApplications"."ProposedRestockingDetail" ("Id", "LinkedPropertyProfileId", "PropertyProfileCompartmentId", "RestockingProposal", "Area", "PercentageOfRestockArea", "RestockingDensity") VALUES ('7A063E02-F93B-42B3-9033-957F8B3A1C01', '229ef2fe-4b02-448d-ab8a-f85c47ed2f6a', '910200f0-ebac-48ea-b8aa-09207bcc1fe3', 'None', NULL, NULL, 0);

-- Data for RestockingOutcome
-- Data for RestockingSpecies

-- Data for StatusHistory
INSERT INTO "FellingLicenceApplications"."StatusHistory" ("Id", "FellingLicenceApplicationId", "Created", "Status") VALUES ('d40b065c-7fd2-48a6-bdae-7b1b8ea6eceb', '940f3d72-06bf-4e42-9829-fd4c4b61e14d', '2022-10-18 16:37:43.215954+01', 'Draft');
INSERT INTO "FellingLicenceApplications"."StatusHistory" ("Id", "FellingLicenceApplicationId", "Created", "Status") VALUES ('4736febc-456e-4b26-9c18-d196d73bf121', '940f3d72-06bf-4e42-9829-fd4c4b61e14d', '2022-10-18 16:39:18.868661+01', 'Submitted');
INSERT INTO "FellingLicenceApplications"."StatusHistory" ("Id", "FellingLicenceApplicationId", "Created", "Status") VALUES ('2a149aec-3a00-4a0e-8aaf-c263f28b1d47', 'c9bff098-f508-4c34-9d80-b7a0387623fb', '2022-10-18 16:41:54.14648+01', 'Draft');

--rejected FLA
INSERT INTO "FellingLicenceApplications"."StatusHistory" ("Id", "FellingLicenceApplicationId", "Created", "Status") VALUES ('a20b065c-7fd2-48a6-bdae-7b1b8ea6eceb', 'E1EF6E7B-3F36-41F9-BA26-A5A6196CF943', '2022-10-18 16:37:43.215954+01', 'Draft');
INSERT INTO "FellingLicenceApplications"."StatusHistory" ("Id", "FellingLicenceApplicationId", "Created", "Status") VALUES ('b336febc-456e-4b26-9c18-d196d73bf121', 'E1EF6E7B-3F36-41F9-BA26-A5A6196CF943', '2022-10-18 16:39:18.868661+01', 'Submitted');
INSERT INTO "FellingLicenceApplications"."StatusHistory" ("Id", "FellingLicenceApplicationId", "Created", "Status") VALUES ('de149aec-3a00-4a0e-8aaf-c263f28b1d47', 'E1EF6E7B-3F36-41F9-BA26-A5A6196CF943', '2022-10-18 16:41:54.14648+01', 'Refused');

-- Data for SubmittedFlaPropertyDetail
INSERT INTO "FellingLicenceApplications"."SubmittedFlaPropertyDetail" ("Id", "FellingLicenceApplicationId", "PropertyProfileId", "Name",  "NearestTown", "HasWoodlandManagementPlan", "WoodlandManagementPlanReference", "IsWoodlandCertificationScheme", "WoodlandCertificationSchemeReference", "WoodlandOwnerId") VALUES ('8d30fcf6-f6d4-4e76-abf2-0715dc73f8ef', '940f3d72-06bf-4e42-9829-fd4c4b61e14d', 'e25cbc1d-4088-404a-a974-c63e02939de6', 'Hyrule Castle', 'bristol', false, NULL, false, NULL, 'd18a424a-4946-4e78-94b0-f56152186593');
INSERT INTO "FellingLicenceApplications"."SubmittedFlaPropertyDetail" ("Id", "FellingLicenceApplicationId", "PropertyProfileId", "Name", "NearestTown", "HasWoodlandManagementPlan", "WoodlandManagementPlanReference", "IsWoodlandCertificationScheme", "WoodlandCertificationSchemeReference", "WoodlandOwnerId") VALUES ('5e30fcf6-f6d4-4e76-abf2-0715dc73f8ef', 'E1EF6E7B-3F36-41F9-BA26-A5A6196CF943', 'e25cbc1d-4088-404a-a974-c63e02939de6', 'Hyrule Castle', 'bristol', false, NULL, false, NULL, 'd18a424a-4946-4e78-94b0-f56152186593');

-- Data for SubmittedFlaPropertyCompartment
INSERT INTO "FellingLicenceApplications"."SubmittedFlaPropertyCompartment" ("Id", "SubmittedFlaPropertyDetailId", "CompartmentId", "CompartmentNumber", "SubCompartmentName", "TotalHectares", "WoodlandName", "Designation", "GISData", "PropertyProfileId") VALUES ('41224178-cc26-4f27-b6e7-c40522579540', '8d30fcf6-f6d4-4e76-abf2-0715dc73f8ef', '910200f0-ebac-48ea-b8aa-09207bcc1fe3', '3', 'a', 0.1446119449381902, 'Forest of Illusion3', NULL, '{"spatialReference":{"wkid":27700},"rings":[[[410469.54957596987,287821.21099951817],[410481.5821284441,287786.48436756496],[410448.97216739075,287786.48436756496],[410434.257696674,287847.17329102615],[410469.54957596987,287821.21099951817]]]}', 'e25cbc1d-4088-404a-a974-c63e02939de6');
INSERT INTO "FellingLicenceApplications"."SubmittedFlaPropertyCompartment" ("Id", "SubmittedFlaPropertyDetailId", "CompartmentId", "CompartmentNumber", "SubCompartmentName", "TotalHectares", "WoodlandName", "Designation", "GISData", "PropertyProfileId") VALUES ('88824178-cc26-4f27-b6e7-c40522579540', '8d30fcf6-f6d4-4e76-abf2-0715dc73f8ef', '910200f0-ebac-48ea-b8aa-09207bcc1fe3', '3', 'a', 0.1446119449381902, 'Forest of Illusion3', NULL, '{"spatialReference":{"wkid":27700},"rings":[[[410469.54957596987,287821.21099951817],[410481.5821284441,287786.48436756496],[410448.97216739075,287786.48436756496],[410434.257696674,287847.17329102615],[410469.54957596987,287821.21099951817]]]}', 'e25cbc1d-4088-404a-a974-c63e02939de6');
