using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forestry.Flo.Services.Applicants.Migrations
{
    [ExcludeFromCodeCoverage]
    /// <inheritdoc />
    public partial class AddApplicantsView : Migration
    {

        private const string Sql = @"

CREATE OR REPLACE VIEW ""Applicants"".""Applicants""

AS

SELECT
	WO.""Id"" AS ""Id"",
	CASE WHEN UA.""Id"" IS NULL 
		THEN (CASE WHEN WO.""IsOrganisation"" = TRUE THEN WO.""OrganisationName"" ELSE WO.""ContactName"" END) 
		ELSE COALESCE(UA.""FirstName"" || ' ', '') || COALESCE(UA.""LastName"", '') 
	END AS ""Name"",
	COALESCE(UA.""Email"", WO.""ContactEmail"") AS ""Email"",
	CASE WHEN AGA.""Id"" IS NULL
		THEN (CASE 
			  WHEN UA.""Id"" IS NULL THEN 'Forestry Commission'
			  WHEN WO.""IsOrganisation"" = TRUE THEN WO.""OrganisationName"" 
			  ELSE WO.""ContactName"" END)
		ELSE (CASE WHEN AGA.""IsOrganisation"" = TRUE THEN AGA.""OrganisationName"" ELSE AGA.""ContactName"" END)
	END AS ""ManagedBy"",
	CASE WHEN WO.""IsOrganisation"" = TRUE THEN 3 ELSE 2 END AS ""Type""

FROM ""Applicants"".""UserAccount"" as UA

FULL OUTER JOIN ""Applicants"".""WoodlandOwner"" AS WO
ON UA.""WoodlandOwnerId"" = WO.""Id""
LEFT JOIN ""Applicants"".""AgentAuthority"" AA on AA.""WoodlandOwnerId"" = WO.""Id"" AND AA.""Status"" != 'Deactivated'
LEFT JOIN ""Applicants"".""Agency"" AGA ON AGA.""Id"" = AA.""AgencyId""

WHERE 
	(UA.""AccountType"" IS NULL OR UA.""AccountType"" IN (0,1))
	AND (UA.""Status"" IS NULL OR UA.""Status"" IN ('Invited','Active','Migrated'))

UNION ALL

SELECT 
	AG.""Id"" AS ""Id"",
	CASE WHEN UAA.""Id"" IS NULL 
		THEN (CASE WHEN AG.""IsOrganisation"" = TRUE THEN AG.""OrganisationName"" ELSE AG.""ContactName"" END) 
		ELSE COALESCE(UAA.""FirstName"" || ' ', '') || COALESCE(UAA.""LastName"", '') 
	END AS ""Name"",
	COALESCE(UAA.""Email"", AG.""ContactEmail"") AS ""Email"",
	CASE WHEN UAA.""Id"" IS NULL 
		THEN 'Forestry Commission'
		ELSE (CASE WHEN AG.""IsOrganisation"" = TRUE THEN AG.""OrganisationName"" ELSE AG.""ContactName"" END)
	END AS ""ManagedBy"",
	CASE WHEN AG.""IsOrganisation"" = TRUE THEN 1 ELSE 0 END AS ""Type""
FROM ""Applicants"".""UserAccount"" as UAA

FULL OUTER JOIN ""Applicants"".""Agency"" AS AG
ON UAA.""AgencyId"" = AG.""Id""

WHERE 
	(UAA.""AccountType"" IS NULL OR UAA.""AccountType"" IN (2,3))
	AND (UAA.""Status"" IS NULL OR UAA.""Status"" IN ('Invited','Active','Migrated'))
	AND (AG.""IsFcAgency"" = FALSE);
";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(Sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW \"Applicants\".\"Applicants\";");
        }
    }
}
