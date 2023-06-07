(function (window, undefined) {
    'use strict';
    /***** local dataTable *****/
    $.extend(true, $.fn.dataTable.defaults, {
        "language": {
            "url": "/assets/vendor/libs/datatables/i18n/ru.json"
        }
    });

    
    
    $(document).on("click", ".sidebar-overlay, .sidebar-content .close", function () {
        $(".sidebar-content").removeClass("show");
        $(".sidebar-overlay").remove();
    });

    /***** show modal *****/
    $(document).on("click", "[data-btn-type='modal']", function (e) {
        e.stopPropagation();
        e.preventDefault();
        $('[data-bs-toggle="tooltip"], .tooltip').tooltip("hide");
        var url = $(this).attr("href"),
            modalContainerId = $("#mainModal");
        $.ajax({
            url: url,
            beforeSend: () => {
                modalContainerId.html('<div class="modal-dialog modal-dialog-centered"><div class="modal-content"><div class="modal-body"><div class="d-flex h-100 justify-content-center align-items-center"><div class="text-center"><p class="mb-0">Пожалуйста, подождите...</p> <div class="sk-wave sk-dark m-0 d-inline-flex"><div class="sk-rect sk-wave-rect"></div> <div class="sk-rect sk-wave-rect"></div> <div class="sk-rect sk-wave-rect"></div> <div class="sk-rect sk-wave-rect"></div> <div class="sk-rect sk-wave-rect"></div></div></div></div></div></div></div>').modal("show");
            },
            success: _.debounce((data) => {
                modalContainerId.html(data);
            }, 400), //TODO для красоты (¬‿¬)
            error: (data) => {
                setTimeout(function () { modalContainerId.modal("hide"); }, 400)
                //console.error(data);
            }
        });
    });

    /***** add table modal *****/
    $(document).on("click", "[data-btn-type='add']", function () {
        var tableName = $(this).data("for-table");
        var $thisTable = $(`#${tableName}`);
        var params = $(this).data("btn-params");
        var addUrl = $thisTable.data("action-add");
        var changingModalContainerId = $thisTable.data("changingModalContainerId");
        changingModalContainerId = changingModalContainerId ? changingModalContainerId : "mainModal";
        $.ajax({
            url: addUrl,
            data: params,
            async: false,
            success: (data) => {
                $("#" + changingModalContainerId).html(data);
                $("#" + changingModalContainerId).modal("show");
            },
            error: (data, textStatus) => {
            }
        });
    });

    /***** edit table modal *****/
    $(document).on("click", "a[data-btn-type='edit']", function (e) {
        e.stopPropagation();
        e.preventDefault();
        var parentId = $(this).data("parent");
        var $parent = $(parentId);
        if ($parent.length <= 0) {
            $parent = $(this).closest("table");
        }
        var params = $(this).data("btn-params");
        var editUrl = $parent.data("action-edit");
        var changingModalContainerId = $parent.data("changingModalContainerId");
        changingModalContainerId = changingModalContainerId ? changingModalContainerId : "mainModal";
        $.ajax({
            url: editUrl,
            data: params,
            async: false,
            success: (data) => {
                $("#" + changingModalContainerId).html(data);
                $("#" + changingModalContainerId).modal("show");
            },
            error: (data, textStatus) => {
            }
        });
    });

    /***** delete *****/
    $(document).on("click", "a[data-btn-type='remove']", function (e) {
        e.stopPropagation();
        e.preventDefault();
        var parentId = $(this).data("parent");
        var isParent = $(parentId).length <= 0;
        var $parent = $(parentId);
        if ($parent.length <= 0) {
            $parent = $(this).closest("table");
        }
        var params = $(this).data("btn-params");
        var removeUrl = $parent.data("action-remove");
        if (typeof removeUrl == "undefined") {
            removeUrl = $(this).data("actionRemove");
        };
        Swal.fire({
            title: 'Причина удаления записи',
            input: 'textarea',
            showCancelButton: true,
            confirmButtonText: 'Удалить',
            cancelButtonText: 'Отмена',
            showLoaderOnConfirm: true,
            confirmButtonColor: '#3085d6',
            cancelButtonColor: '#d33',
            customClass: {
                confirmButton: 'btn btn-danger',
                cancelButton: 'btn btn-secondary ms-2'
            },
            buttonsStyling: false,
            returnInputValueOnDeny: true,
            preConfirm: (comment) => {
                if (comment === '') {
                    Swal.showValidationMessage(`Введите комменетарий`);
                } else {
                    params.comment = comment;
                    $.ajax({
                        type: 'POST',
                        url: removeUrl,
                        data: params,
                        async: false,
                        beforeSend: () => {
                            Swal.showLoading();
                        },
                        complete: () => {
                            Swal.hideLoading();
                        },
                        success: (data) => {
                            if (!isParent) {
                                $parent.trigger("reload");
                            }
                            else {
                                document.location.reload();
                            }
                        },
                        error: (data, textStatus) => {
                            $.notifi('error', 'Ошибка', data.responseText);
                        }
                    });
                }
            },
            allowOutsideClick: () => !Swal.isLoading()
        }).then((comment) => {
            //$.notifi('success', 'Готово', 'Запись успешно удалена');
        });
    });

    $(document).on("click", "a[data-btn-type='copy']", function (e) {
        e.stopPropagation();
        e.preventDefault();
        var parentId = $(this).data("parent");
        var isParent = $(parentId).length <= 0;
        var $parent = $(parentId);
        if ($parent.length <= 0) {
            $parent = $(this).closest("table");
        }
        var params = $(this).data("btn-params");
        var copyUrl = $parent.data("action-copy");
        if (typeof copyUrl == "undefined") {
            copyUrl = $(this).data("actionCopy");
        };
        Swal.fire({
            title: 'Копировать услугу',
            /*input: 'textarea',*/
            showCancelButton: true,
            confirmButtonText: 'Скопировать',
            cancelButtonText: 'Отмена',
            showLoaderOnConfirm: true,
            confirmButtonColor: '#3085d6',
            cancelButtonColor: '#d33',
            customClass: {
                confirmButton: 'btn btn-success',
                cancelButton: 'btn btn-secondary ms-2'
            },
            buttonsStyling: false,
            returnInputValueOnDeny: true,
            preConfirm: () => {
                /*if (comment === '') {
                    Swal.showValidationMessage(`Введите комменетарий`);
                } else {
                    params.comment = comment;*/
                    $.ajax({
                        type: 'POST',
                        url: copyUrl,
                        data: params,
                        async: false,
                        beforeSend: () => {
                            Swal.showLoading();
                        },
                        complete: () => {
                            Swal.hideLoading();
                        },
                        success: (data) => {
                            if (!isParent) {
                                $parent.trigger("reload");
                            }
                            else {
                                document.location.reload();
                            }
                        },
                        error: (data, textStatus) => {
                            $.notifi('error', 'Ошибка', data.responseText);
                        }
                    });
                /*}*/
            },
            allowOutsideClick: () => !Swal.isLoading()
        }).then((comment) => {
            //$.notifi('success', 'Готово', 'Запись успешно удалена');
        });
    });


})(window);

(function ($) {
    $.extend({
        pageBlock: function () {
            $.blockUI({
                message:
                    '<div><p class="mb-0">Пожалуйста, подождите...</p> <div class="sk-wave m-0 d-inline-flex"><div class="sk-rect sk-wave-rect"></div> <div class="sk-rect sk-wave-rect"></div> <div class="sk-rect sk-wave-rect"></div> <div class="sk-rect sk-wave-rect"></div> <div class="sk-rect sk-wave-rect"></div></div> </div>',
                css: {
                    backgroundColor: 'transparent',
                    color: '#fff',
                    border: '0'
                },
                overlayCSS: {
                    opacity: 0.5
                }
            });
        },
        /**/
        nameof: (element) => {
            return element.attr("name");
        },
        /***** russian plural *****/
        declOfNum: function (n, titles) {
            return titles[(n % 10 === 1 && n % 100 !== 11) ? 0 : n % 10 >= 2 && n % 10 <= 4 && (n % 100 < 10 || n % 100 >= 20) ? 1 : 2]
        },
        /*Отображение уведомления.*/
        notifi: function (type, title, text) {
            toastr[type](text, title, {
                closeButton: true,
                tapToDismiss: false,
                timeOut: "6000"
            });
        },
        customValidate: {
            snils: (snils) => {
                var result = false;
                var error;
                if (typeof snils === 'number') {
                    snils = snils.toString();
                } else if (typeof snils !== 'string') {
                    snils = '';
                }
                if (!snils.length) {
                    error = 'СНИЛС пуст';
                } else if (/[^0-9]/.test(snils)) {
                    error = 'СНИЛС может состоять только из цифр';
                } else if (snils.length !== 11) {
                    error = 'СНИЛС может состоять только из 11 цифр';
                } else {
                    var sum = 0;
                    for (var i = 0; i < 9; i++) {
                        sum += parseInt(snils[i]) * (9 - i);
                    }
                    var checkDigit = 0;
                    if (sum < 100) {
                        checkDigit = sum;
                    } else if (sum > 101) {
                        checkDigit = parseInt(sum % 101);
                        if (checkDigit === 100) {
                            checkDigit = 0;
                        }
                    }
                    if (checkDigit === parseInt(snils.slice(-2))) {
                        result = true;
                    } else {
                        error = 'Неправильное контрольное число';
                    }
                }
                console.log(result);
                console.log(error);
                return {
                    valid: result,
                    message: error,
                };
            }
        }
    });
})(jQuery);

/***** custom spinnerBtn plugin *****/
(function ($) {
    const params = {
        content: ""
    };
    var methods = {
        init: function (options) {
            console.log(options);
            console.log('init');
        },
        start: function () {
            var id = this.attr('id');
            var $btn = $(`button[id="${id}"]`);
            if ($btn.length <= 0) { $btn = $(`button[type="submit"][form="${id}"]`); }
            params.content = $btn.html();
            $btn.prop('disabled', true);
            $btn.html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Подождите...');

        },
        stop: function () {
            var id = this.attr('id');
            var $btn = $(`button[id="${id}"]`);
            if ($btn.length <= 0) { $btn = $(`button[type="submit"][form="${id}"]`); }
            $btn.prop('disabled', false);
            $btn.html(params.content);
        }
    };

    $.fn.spinnerBtn = function (methodOrOptions) {
        if (methods[methodOrOptions]) {
            return methods[methodOrOptions].apply(this, Array.prototype.slice.call(arguments, 1));
        } else if (typeof methodOrOptions === 'object' || !methodOrOptions) {
            return methods.init.apply(this, arguments);
        } else {
            $.error('Method ' + methodOrOptions + ' does not exist on jQuery.tooltip');
        }
    };
})(jQuery);

// jQuery SerializeToJSON v1.4.1
// github.com/raphaelm22/jquery.serializeToJSON
!function (e) { "function" == typeof define && define.amd ? define(["jquery"], e) : "object" == typeof module && module.exports ? module.exports = function (r, t) { return void 0 === t && (t = "undefined" != typeof window ? require("jquery") : require("jquery")(r)), e(t), t } : e(jQuery) }(function (e) { "use strict"; e.fn.serializeToJSON = function (r) { var t = { settings: e.extend(!0, {}, e.fn.serializeToJSON.defaults, r), getValue: function (r) { var t = r.val(); if (r.is(":radio") && (t = r.filter(":checked").val() || null), r.is(":checkbox") && (t = e(r).prop("checked")), this.settings.parseBooleans) { var n = (t + "").toLowerCase(); "true" !== n && "false" !== n || (t = "true" === n) } var i = this.settings.parseFloat.condition; return void 0 !== i && ("string" == typeof i && r.is(i) || "function" == typeof i && i(r)) && (t = this.settings.parseFloat.getInputValue(r), t = Number(t), this.settings.parseFloat.nanToZero && isNaN(t) && (t = 0)), t }, createProperty: function (r, t, n, i) { for (var a = r, s = 0; s < n.length; s++) { var u = n[s]; if (s === n.length - 1) { var l = i.is("select") && i.prop("multiple"); l && null !== t ? (a[u] = new Array, Array.isArray(t) ? e(t).each(function () { a[u].push(this) }) : a[u].push(t)) : "undefined" != typeof a[u] ? i.is("[type='hidden']") || (a[u] = t) : a[u] = t } else { var o = /\[\w+\]/g.exec(u), c = null != o && o.length > 0; if (c) { u = u.substr(0, u.indexOf("[")), this.settings.associativeArrays ? a.hasOwnProperty(u) || (a[u] = {}) : Array.isArray(a[u]) || (a[u] = new Array), a = a[u]; var f = o[0].replace(/[\[\]]/g, ""); u = f } a.hasOwnProperty(u) || (a[u] = {}), a = a[u] } } }, includeUncheckValues: function (r, t) { e(":radio", r).each(function () { var r = 0 === e("input[name='" + this.name + "']:radio:checked").length; r && t.push({ name: this.name, value: null }) }), e("select[multiple]", r).each(function () { null === e(this).val() && t.push({ name: this.name, value: null }) }) }, serializeArray: function (e) { var r = /\r?\n/g, t = /^(?:submit|button|image|reset|file)$/i, n = /^(?:input|select|textarea|keygen)/i, i = /^(?:checkbox|radio)$/i; return e.map(function () { var e = jQuery.prop(this, "elements"); return e ? jQuery.makeArray(e) : this }).filter(function () { var e = this.type; return this.name && !jQuery(this).is(":disabled") && n.test(this.nodeName) && !t.test(e) && (this.checked || !i.test(e)) }).map(function (e, t) { var n = jQuery(this).val(); return null == n ? null : Array.isArray(n) ? jQuery.map(n, function (e) { return { name: t.name, value: e.replace(r, "\r\n"), elem: t } }) : { name: t.name, value: n.replace(r, "\r\n"), elem: t } }).get() }, serializer: function (r) { var t = this, n = this.serializeArray(e(r)); this.includeUncheckValues(r, n); var i = {}; for (var a in n) if (n.hasOwnProperty(a)) { var s = n[a], u = e(s.elem), l = t.getValue(u), o = s.name.split("."); t.createProperty(i, l, o, u) } return i } }; return t.serializer(this) }, e.fn.serializeToJSON.defaults = { associativeArrays: !0, parseBooleans: !0, parseFloat: { condition: void 0, nanToZero: !0, getInputValue: function (e) { return e.val().split(",").join("") } } } });