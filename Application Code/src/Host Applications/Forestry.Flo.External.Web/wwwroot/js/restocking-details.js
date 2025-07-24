$(function () {

    $('#Area').change(function () {

        var compartmentHectares = document.getElementById("Restocking-Compartment-Total-Hectares-hidden").textContent;

        if (compartmentHectares === "") {
            return;
        }

        var percentageOfRestocking = (Math.round((document.getElementById("Area").value / compartmentHectares) * 10000) / 10000) * 100;
        document.getElementById("Percentage-Of-Restock-Area").value = percentageOfRestocking.toFixed(2);
    });
});