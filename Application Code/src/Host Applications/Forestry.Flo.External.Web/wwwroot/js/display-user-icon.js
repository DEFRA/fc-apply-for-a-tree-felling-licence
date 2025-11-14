$(function () {
    $(document).ready(setAvatarsActivity);

    var colourLookup = [
        { role : "AdminOfficer", colour : "#EFBE7D" },
        { role : "FcStaffMember", colour : "#B1A2CA" },
        { role : "WoodlandOfficer", colour : "#8BD3E6" },
        { role : "FieldManager", colour : "#FF6D6A" },
        { role: "AdminHubManager", colour: "#E6E0CE" },
        { role: "EvidenceAndAnalysisOfficer", colour: "#D2E6D4" },
        { role: "AccountAdministrator", colour: "#C9DCAF" }
    ];
    

    function getInitials(forename, surname) {
        var forenames = forename.split(' ');
        var surnames = surname.split(' ');
        var firstInitial = forenames[0].substring(0, 1).toUpperCase();
        if (surnames.length > 0) {
            // For surnames containing multiple names such as: ap Dafydd
            if (surnames.length > 1) {
                return firstInitial + surnames[0].substring(0, 1).toUpperCase() + surnames[1].substring(0, 1).toUpperCase();
            }
            // For double-barelled surnames
            if (surnames[0].includes("-")) {
                doubleBarelled = surnames[0].split("-");
                return firstInitial + doubleBarelled[0].substring(0, 1).toUpperCase() + doubleBarelled[1].substring(0, 1).toUpperCase();
            }

            // For names with multiple joined parts such as: O'Dowd or MacGregor
            multipleParts = surnames[0].split(/(?=[A-Z])/);
            if (multipleParts.length > 1) {
                return firstInitial + multipleParts[0].substring(0, 1).toUpperCase() + multipleParts[1].substring(0, 1).toUpperCase();
            }

            return firstInitial + surnames[0].substring(0,1).toUpperCase();
        }
        // Simply return the first initial if no surname is present
        return firstInitial;
    }


    function setAvatarsActivity() {
        $(".activity-avatar-button").removeAttr("hidden");
        $(".activity-avatar-button").removeAttr("aria-hidden");

        var avatars = document.querySelectorAll(".activity-avatar");
        for (var avatar of avatars) {
            var forename = avatar.getAttribute("data-forename");
            var surname = avatar.getAttribute("data-surname");
            var role = avatar.getAttribute("data-role");

            var iconColourObject = colourLookup.find(o => o.role === role);
            iconColour = iconColourObject === undefined ? "#B1A2CA" : iconColourObject.colour;

            let initials = getInitials(forename, surname);
            avatar.src = generateAvatar(initials, "Black", iconColour);
        }
    }

    function generateAvatar(text, foregroundColor, backgroundColor) {
        const canvas = document.createElement("canvas");
        const context = canvas.getContext("2d");

        canvas.width = 50;
        canvas.height = 50;

        // Draw background
        context.fillStyle = backgroundColor;
        context.beginPath();
        context.arc(25, 25, 25, 0, 2 * Math.PI);
        context.fill();

        // Draw text
        context.font = "bold 20px Arial";
        context.fillStyle = foregroundColor;
        context.textAlign = "center";
        context.textBaseline = "middle";
        context.fillText(text, canvas.width / 2, canvas.height / 2);

        return canvas.toDataURL("image/png");
    }
});