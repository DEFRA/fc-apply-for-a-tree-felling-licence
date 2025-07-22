--See https://quicksilva.atlassian.net/browse/FLOV2-316?focusedCommentId=20286
--this script assumes that seed-admin-hubs.sql has been run first

UPDATE "AdminHubs"."AdminHub" SET "Name" = 'Bullers Hill', "Address" = E'Bullers Hill\nKennford\nExeter\nEX6 7XR\nPhone: 0300 067 4960\nEmail: adminhub.bullershill@forestrycommission.gov.uk' WHERE "Id" = '44F20062-E77F-4C1C-BD47-0CC30DD965E9';  --Bullers Hill
UPDATE "AdminHubs"."AdminHub" SET "Address" = E'Bucks Horn Oak\nFarnham\nSurrey\nGU10 4LS\nPhone: 0300 067 4420\nEmail: adminhub.buckshornoak@forestrycommission.gov.uk' WHERE "Id" = '1A394600-23CD-4EAE-89CC-C47890AD4047';  --Bucks Horn Oak

UPDATE "AdminHubs"."Area" SET "Code" = '010' WHERE "Id" = '8BC3F047-DA34-493C-9A2D-5A4926A482F9';     --North West & West Midlands
UPDATE "AdminHubs"."Area" SET "Code" = '018' WHERE "Id" = '983C2D0B-C56B-4B65-99EA-5B14C00545C4';     --South West
UPDATE "AdminHubs"."Area" SET "Code" = '022' WHERE "Id" = '4DA35A0F-9CB3-4C55-829F-9589F04BF790';     --Yorkshire & North East
UPDATE "AdminHubs"."Area" SET "Code" = '017' WHERE "Id" = '0DB4755F-A80D-4068-9D2D-BC3D950EAB76';     --East & East Midlands
UPDATE "AdminHubs"."Area" SET "Code" = '019' WHERE "Id" = '5A1C130E-A671-4CCC-9112-1C41CBFF181D';     --South East & London

insert into "AdminHubs"."Area" ("Id", "Name", "Code","AdminHubId") values ('60BA0E1A-DF24-4740-A3E0-4AA46DDC2680','Yorkshire and The Humber','012','1A394600-23CD-4EAE-89CC-C47890AD4047');
insert into "AdminHubs"."Area" ("Id", "Name", "Code","AdminHubId") values ('106B316A-F1C0-4C11-9A75-59FB0A350D42','West Midlands','015','44F20062-E77F-4C1C-BD47-0CC30DD965E9');
