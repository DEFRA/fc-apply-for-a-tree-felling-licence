INSERT INTO "Applicants"."AgentAuthority"(
"CreatedTimestamp",
"CreatedByUserId",
"ChangedByUserId",
"WoodlandOwnerId",
"AgencyId",
"Status"
)
SELECT
NOW(),
u."Id",
NULL,
@woodlandOwnerId,
@agencyId,
'Created'
FROM
"Applicants"."UserAccount" u
WHERE
u."AgencyId" = @agencyId
RETURNING "Id";