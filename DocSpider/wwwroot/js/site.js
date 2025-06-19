// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
$(document).ready(function () {
    $('#sidebarCollapse').on('click', function () {
        $('#sidebar').toggleClass('active');
    });

    // Ativar tooltips
    $('[data-toggle="tooltip"]').tooltip();

    // Fechar menu ao clicar em um item (mobile)
    $(window).resize(function () {
        if ($(window).width() <= 768) {
            $('#sidebar a').on('click', function () {
                $('#sidebar').addClass('active');
            });
        } else {
            $('#sidebar a').off('click');
        }
    }).resize();
});