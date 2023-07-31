var sendingForm = $('#reportForm'),
    btnSubmit = $('#reportSubmit'),
    btnPrint = $('#reportPrint'),
    btnDownload = $('#reportDownload'),
    datePeriod = $('#date-range'),
    providers = $('#providerId'),
    mfc = $('#mfcId'),
    employees = $('#employeeId'),
    services = $('#serviceId'),
    smev = $('#smevId'),
    smevRequest = $('#smevRequestId');
    

$(document).ready(function () {
   
    $('select.select2-search').select2({}); 
    $('select.select2-nosearch').select2({
        minimumResultsForSearch: Infinity
    });

    $('select.select2-tags').select2({
        tags: true
    });

    if (datePeriod.length) {
        datePeriod.datepicker({
            language: "ru",
            endDate: '-1d',
            toggleActive: true
        });
    }

    if (providers.length) {
        $.ajax({
            type: 'Get',
            url: 'GetProvidersDataJson',
            data: {},
            beforeSend: () => {
                providers.empty();
                providers.prop("disabled", true);
            },
            success: function (data) {
                data.forEach(function (item) {
                    let selected = item.id === '00000000-0000-0000-0000-000000000000';

                    providers.append($("<option></option>")
                    .attr("value", item.id)
                    .text(item.officeName)
                    .prop("selected", selected));
                });

                providers.trigger("change");
            },
            complete: () => {
                providers.prop("disabled", false);
            }
        });
    }


    if (services.length) {
        providers.change(function (e) { 
            let isAll = false;
            let guidEmpty = '00000000-0000-0000-0000-000000000000';
            let arr = [];

            $.each($(e.target).val(), function (index, val) {
                if (val === guidEmpty) {
                    isAll = true; 
                }
                arr.push({ name: 'providersId', value: val });
            })
             
            if (isAll && services.find('option[value="00000000-0000-0000-0000-000000000000"]').length == 0) {
                services.append($("<option></option>")
                .attr("value", guidEmpty)
                .text("Все")
                .prop("selected", "selected"));
                services.prop("disabled", false);
            }
            else {
                $.ajax({
                    type: 'Get',
                    url: 'GetServicesForProviderDataJson',
                    data: arr,
                    beforeSend: () => {
                        services.empty();
                        services.prop("disabled", true);
                    },
                    success: function (data) {
                        data.forEach(function (item) {

                            let selected = item.id === '00000000-0000-0000-0000-000000000000';

                            services.append($("<option></option>")
                            .attr("value", item.id)
                            .text(item.serviceName)
                            .prop("selected", selected));
                        });
                    },
                    complete: () => {
                        services.prop("disabled", false);
                    }
                });
            }
        });
    }

    if (mfc.length) {
        $.ajax({
            type: 'Get',
            url: 'GetMfcDataJson',
            data: { isAll: $(mfc).data("all") },
            beforeSend: () => {
                mfc.empty();
                mfc.prop("disabled", true);
            },
            success: function (data) {
                if (data.length === 1) {
                    data.forEach(function (item) {
                        mfc.append($("<option></option>")
                        .attr("value", item.id)
                        .text(item.officeName)
                        .prop("selected", "selected"));
                    });
                }
                else {
                    data.forEach(function (item) {

                        let selected = item.id === '00000000-0000-0000-0000-000000000000';

                        mfc.append($("<option></option>")
                        .attr("value", item.id)
                        .text(item.officeName)
                        .prop("selected", selected));
                    });
                }
                mfc.trigger("change");
            },
            complete: () => {
                mfc.prop("disabled", false);
            }
        });

    }

    if (employees.length) {
        mfc.change(function (e) {

            let isAll = false;
            let guidEmpty = '00000000-0000-0000-0000-000000000000';
            let arr = [];

            $.each($(e.target).val(), function (index, val) {
                if (val === guidEmpty) {
                    isAll = true;

                }
                arr.push({ name: 'employeeId', value: val });
            })

            if (isAll && employees.find('option[value="00000000-0000-0000-0000-000000000000"]').length == 0) {
                employees.append($("<option></option>")
                .attr("value", guidEmpty)
                .text("Все")
                .prop("selected", "selected"));

                employees.prop("disabled", false);
            }
            else {
                $.ajax({
                    type: 'Get',
                    url: 'GetEmployeesForMfcDataJson',
                    data: arr,
                    beforeSend: () => {
                        employees.empty();
                        employees.prop("disabled", true);
                    },
                    success: function (data) {
                        if (data.length === 1) {
                            data.forEach(function (item) {
                                employees.append($("<option></option>")
                                .attr("value", item.id)
                                .text(item.officeName)
                                .prop("selected", "selected"));
                            });
                        }
                        else {
                            data.forEach(function (item) {

                                let selected = item.id === '00000000-0000-0000-0000-000000000000';

                                employees.append($("<option></option>")
                                .attr("value", item.id)
                                .text(item.officeName)
                                .prop("selected", selected));
                            });
                        }
                    },
                    complete: () => {
                        employees.prop("disabled", false);
                    }
                });
            }
        });
    }

    if (smev.length) {
        $.ajax({
            type: 'Get',
            url: 'GetSmevDataJson',
            data: {},
            beforeSend: () => {
                smev.empty();
                smev.prop("disabled", true);
            },
            success: function (data) {
                data.forEach(function (item) {
                    let selected = item.id === '00000000-0000-0000-0000-000000000000';

                    smev.append($("<option></option>")
                    .attr("value", item.id)
                    .text(item.smevName)
                    .prop("selected", selected));
                });

                smev.trigger("change");
            },
            complete: () => {
                smev.prop("disabled", false);
            }
        });
    }

    if (smevRequest.length) {
        smev.change(function (e) {

            let isAll = false;
            let guidEmpty = '00000000-0000-0000-0000-000000000000';
            let arr = [];

            $.each($(e.target).val(), function (index, val) {
                if (val === guidEmpty) {
                    isAll = true;

                }
                arr.push({ name: 'smevId', value: val });
            })

            if (isAll && smevRequest.find('option[value="0"]').length == 0) {
                smevRequest.append($("<option></option>")
                .attr("value", 0)
                .text("Все")
                .prop("selected", "selected"));

                smevRequest.prop("disabled", false);

            }
            else {
                $.ajax({
                    type: 'Get',
                    url: 'GetRequestForSmevDataJson',
                    data: arr,
                    beforeSend: () => {
                        smevRequest.empty();
                        smevRequest.prop("disabled", true);
                    },
                    success: function (data) {
                        data.forEach(function (item) {
                            console.log(item.id);
                            let selected = item.id === 0;

                            smevRequest.append($("<option></option>")
                            .attr("value", item.id)
                            .text(item.requestName)
                            .prop("selected", selected));
                        });
                        
                    },
                    complete: () => {
                        smevRequest.prop("disabled", false);
                    }
                });
            }
        });
    }

    btnSubmit.on('click', function () {

        let formData = sendingForm.serialize();

        console.log(3333333, sendingForm.attr('action'),);

        $.ajax({
            type: sendingForm.attr('method'),
            url: sendingForm.attr('action'),
            data: formData ,
            beforeSend: () => {
                btnSubmit.html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span>');
                btnSubmit.prop('disabled', true);
                btnPrint.prop('disabled', true);
                btnPrint.addClass("d-none");
                btnDownload.prop('disabled', true);
                btnDownload.addClass("d-none");
            },
            success: function (responce) {
                $('#reportContent').html(responce);

                btnPrint.prop('disabled', false);
                btnPrint.removeClass("d-none");
                btnDownload.prop('disabled', false);
                btnDownload.removeClass("d-none");
            },
            complete: () => {
                btnSubmit.prop('disabled', false);
                btnSubmit.html('Cформировать')
            }
        });
    })

    btnPrint.on('click', function () {

        let formData = sendingForm.serialize();
        let $url = $(this).data('reportPrint')

        console.log(12312412, formData);

        $.ajax({
            type: 'Get',
            url: '/Reports/' + $url,
            data: formData,
            beforeSend: () => {
                btnPrint.html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span>');
                btnPrint.prop('disabled', true);
                btnSubmit.prop('disabled', true);
                btnDownload.prop('disabled', true);
            },
            success: function (responce) {
                console.log(responce)
                if (responce != null) printPDF(responce);
            },
            complete: () => {
                btnSubmit.prop('disabled', false);
                btnDownload.prop('disabled', false);
                btnPrint.prop('disabled', false);
                btnPrint.html('Печать')
            }
        });
    })

    btnDownload.on('click', function () {

        var $url = $(this).data('reportUpload')
        let formData = sendingForm.serializeArray();
        //let id = $(".active.active-color").attr("id");
        //formData.push({ name: "reportId", value: id });

        btnPrint.prop('disabled', true);
        btnSubmit.prop('disabled', true);
        btnDownload.prop('disabled', true);
      
        window.open('/Reports/' + $url + '?' + $.param(formData), '_blank');

        btnSubmit.prop('disabled', false);
        btnDownload.prop('disabled', false);
        btnPrint.prop('disabled', false);
    })

});