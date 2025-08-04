UPDATE "Applicants"."WoodlandOwner"
SET "[Flov1ManagedOwnerIdColumn]" = @managedOwnerId
WHERE "Id" = (
	SELECT "WoodlandOwnerId"
	FROM "Applicants"."UserAccount"
	WHERE "[Flov1IdColumnName]" = @ownerId
)