// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

(function () {
    'use strict'

    // Fetch all the forms we want to apply custom Bootstrap validation styles to
    var forms = document.querySelectorAll('.needs-validation')

    // Loop over them and prevent submission
    Array.prototype.slice.call(forms)
        .forEach(function (form) {
            form.addEventListener('submit', function (event) {
                if (!form.checkValidity()) {
                    event.preventDefault()
                    event.stopPropagation()
                }

                form.classList.add('was-validated')
            }, false)
        })

    $("input").bind('cssClassChanged', function () {
        // cleanup
        if ($(this).hasClass("is-valid")) {
            $(this).removeClass("is-valid");
        }
        if ($(this).hasClass("is-invalid")) {
            $(this).removeClass("is-invalid");
        }

        // remap the css classes to that of BootStrap 
        if ($(this).hasClass("input-validation-error")) {
            $(this).addClass("is-invalid");
        }

        if ($(this).hasClass("valid")) {
            $(this).addClass("is-valid");
        }
    });
})()