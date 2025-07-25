﻿// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

$(function () {
    var controllerName = window.location.pathname.split("/")[1];

    $('#site-nav-home').removeClass('govuk-service-navigation__item--active');
    $('#site-nav-profile').removeClass('govuk-service-navigation__item--active');
    $('#site-nav-properties').removeClass('govuk-service-navigation__item--active');

    switch (controllerName.toLowerCase()) {
        case "account":
            $('#site-nav-profile').addClass('govuk-service-navigation__item--active');
            break;
        case "propertyprofile":
            $('#site-nav-properties').addClass('govuk-service-navigation__item--active');
            break;
        case "home": 
            $('#site-nav-home').addClass('govuk-service-navigation__item--active');
            break;
        default:
            break;
    }

    $('.validation-summary-errors ul').addClass("govuk-list govuk-error-summary__list");

    $('.validation-summary-errors li').each(function (i, obj) {
        var errorText = $(this).text();
        var errorCausedBy = $("p:contains('" + errorText + "')").attr("data-valmsg-for");
        if (errorCausedBy != null) {
            errorCausedBy = errorCausedBy.replaceAll('.', '_');
        }
        $(this).html('<a href="#' + errorCausedBy + '">' + errorText + '</a>')
    });

    $(document).ready(function () {
        $('.validation-summary-errors ul').addClass('govuk-list govuk-error-summary__list');
        $('.input-validation-error').closest("div.govuk-form-group").addClass('govuk-form-group--error');
        $('.govuk-input input-validation-error').closest("div.govuk-form-group").addClass('govuk-form-group--error');
        $('.input-validation-error').addClass('govuk-input--error');
        $('select.input-validation-error').addClass('govuk-select--error');
    });
});