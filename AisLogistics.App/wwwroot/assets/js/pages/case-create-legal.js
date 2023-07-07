$(function () {
    "use strict";
    const createCaseWizard = document.querySelector('#create-case-wizard'),
        createCaseWizardForm = createCaseWizard.querySelector('#create-case-wizard-form'),
        createCaseWizardFormStep1 = createCaseWizardForm.querySelector('#service-step'),
        createCaseWizardFormStep2 = createCaseWizardForm.querySelector('#legal-info-step'),
        createCaseWizardFormStep3 = createCaseWizardForm.querySelector('#additional-info-step'),
        createCaseWizardFormStep4 = createCaseWizardForm.querySelector('#service-provider-step'),
        createCaseWizardFormStep5 = createCaseWizardForm.querySelector('#final-step'),
        createCaseWizardNext = [].slice.call(createCaseWizardForm.querySelectorAll('.btn-next')),
        createCaseWizardPrev = [].slice.call(createCaseWizardForm.querySelectorAll('.btn-prev')),
        createCaseWizardAdditionalForm = $("#additional-data"),
        createCaseWizardRepresentativeInfo = createCaseWizardForm.querySelector('#representative-info'),
        pickadate = $(".pickadate"),
        btnNextSecondStep = $('#btn-next-second-step'),
        btnFormSubmit = $('#btn-form-submit'),
        servicesTable = $('#services-table'),
        serviceProcedureTable = $('#service-procedure-table'),
        serviceProcedureTableContainer = $('#service-procedure-table-container '),
        serviceTariffTable = $('#service-tariff-table'),
        serviceTariffTableContainer = $('#service-tariff-table-container '),
        serviceProviderTable = $('#service-provider-table'),
        servicesTableSearch = $("#services-table-search"),
        customer = {
            documentsIdentityId: $("#customer_SDocumentsIdentityId"),
            documentSerial: $("#customer_DocumentSerial"),
            documentNumber: $("#customer_DocumentNumber"),
            documentIssueDate: $("#customer_DocumentIssueDate"),
            documentCode: $("#customer_DocumentCode"),
            documentIssueBody: $("#customer_DocumentIssueBody"),
            documentBirthPlace: $("#customer_DocumentBirthPlace"),
            address: $("#customer_CustomerAddress"),
            addressResidence: $("#customer_CustomerAddressResidence"),
            addressTemp: $("#customer_CustomerAddressTemp"),
            firstName: $("#customer_FirstName"),
            lastName: $("#customer_LastName"),
            secondName: $("#customer_SecondName"),
            gender: $("[name='customer.CustomerGender']"),
            snils: $("#customer_CustomerSnils"),
            inn: $("#customer_CustomerInn"),
            phone1: $("#customer_CustomerPhone1"),
            phone2: $("#customer_CustomerPhone2")
        },
        customerLegal = {
            name: $("#customerLegal_CustomerName"),
            address: $("#customerLegal_CustomerAddress"),
            inn: $("#customerLegal_CustomerInn"),
            kpp: $("#customerLegal_CustomerKpp"),
            ogrn: $("#customerLegal_CustomerOgrn"),
            okato: $("#customerLegal_CustomerOkato"),
            oktmo: $("#customerLegal_CustomerOktmo"),
            firstName: $("#customerLegal_HeadFirstName"),
            lastName: $("#customerLegal_HeadLastName"),
            secondName: $("#customerLegal_HeadSecondName"),
            position: $("#customerLegal_HeadPosition"),
            phone1: $("#customerLegal_CustomerPhone1"),
            phone2: $("#customerLegal_CustomerPhone2")
        };

    var FormValidation1 = null,
        FormValidation2 = null,
        FormValidation3 = null;

    const setGender = {
        identify: (type) => {
            if (type === "MALE")
                $(customer.gender[0]).prop("checked", true);
            else
                $(customer.gender[1]).prop("checked", true);
        }
    };

    const serviceProcedureTableVisibility = {
        show: () => {
            serviceProcedureTableContainer.removeClass("d-none");
            serviceProcedureTable.DataTable().ajax.reload();
        },
        hide: () => {
            btnNextSecondStepActive.disabled();
            serviceProcedureTableContainer.addClass("d-none");
        }
    };

    const serviceTariffTableVisibility = {
        show: () => {
            serviceTariffTableContainer.removeClass("d-none");
            serviceTariffTable.DataTable().ajax.reload();
            serviceProviderTable.DataTable().ajax.reload();
        },
        hide: () => {
            btnNextSecondStepActive.disabled();
            serviceTariffTableContainer.addClass("d-none");
        }
    };

    const btnNextSecondStepActive = {
        enabled: () => {
            btnNextSecondStep.attr("disabled", false);
        },
        disabled: () => {
            btnNextSecondStep.attr("disabled", true);
        }
    };

    const btnFormSubmitActive = {
        enabled: () => {
            btnFormSubmit.attr("disabled", false);
        },
        disabled: () => {
            btnFormSubmit.attr("disabled", true);
        }
    };

    const findCustomerByDocument = {
        load: () => {
            if (customer.documentSerial.inputmask("isComplete") === false && customer.documentNumber.inputmask("isComplete") === false) return;
            $.ajax({
                url: '/Cases/FindCustomerByDocumentDataJson',
                type: 'POST',
                data: {
                    serial: customer.documentSerial.val(),
                    number: customer.documentNumber.val()
                },
                success: function (data) {
                    if (!data) return;
                    createCaseWizardRepresentativeInfo.querySelectorAll('input[type="text"]').forEach((e) => {
                        let name = e.name.split('.')[1],
                            newValue = data[name.charAt(0).toLowerCase() + name.slice(1)];
                        if (!newValue) return;
                        if (e.classList.contains("pickadate")) newValue = moment(newValue).format("DD.MM.YYYY");
                        e.value = newValue;
                    });
                    setGender.identify(data["customerGender"] === 1 ? "MALE" : "FEMALE");
                    toastr.success('<p style="line-height: 1.2;" class="mt-1 small">Данные заполнились автоматически.</p><button type="button" class="btn btn-sm btn-secondary">Отменить</button>', 'Найден заявитель', {
                        timeOut: 3500, extendedTimeOut: 3500, preventDuplicates: true,
                        onclick: function () { findCustomerByDocument.clear(); }
                    });
                }
            });
        },
        clear: () => {
            createCaseWizardRepresentativeInfo.querySelectorAll('input[type="text"]').forEach((e) => { e.value = ""; });
            setGender.identify("MALE");
        }
    }

    const additionalForms = {
        load: () => {
            let action = serviceProcedureTable.DataTable().rows({ selected: true }).data()[0]['additionalFormPath'];
            if (!action) return;
            $.ajax({
                url: `/Cases/${action}`,
                type: 'Get',
                success: function (content) {
                    if (!content) return;
                    createCaseWizardAdditionalForm.html(content);
                    additionalForms.initValidation();
                }
            });
        },
        clear: () => {
            additionalForms.removeValidation();
            createCaseWizardAdditionalForm.empty();
            if (typeof additionalFormValidation === "function")
                additionalFormValidation = null;
        },
        initValidation: () => {
            if (typeof additionalFormValidation !== "function") return;
            let rules = additionalFormValidation();
            rules.forEach(obj => {
                Object.entries(obj).forEach(([key, value]) => {
                    FormValidation2.addField(key, value);
                });
            });
        },
        removeValidation: () => {
            if (typeof additionalFormValidation !== "function") return;
            let rules = additionalFormValidation();
            rules.forEach(obj => {
                Object.entries(obj).forEach(([key]) => {
                    FormValidation2.removeField(key);
                });
            });
        }
    }

    const initComponents = () => {
        const validationStepper = new Stepper(createCaseWizard, { linear: true });
        FormValidation1 = FormValidation.formValidation(createCaseWizardFormStep1, {
            plugins: {
                trigger: new FormValidation.plugins.Trigger(),
                submitButton: new FormValidation.plugins.SubmitButton()
            }
        }).on('core.form.valid', () => { additionalForms.load(); validationStepper.next(); });

        FormValidation2 = FormValidation.formValidation(createCaseWizardFormStep2, {
            locale: 'ru_RU',
            localization: FormValidation.locales.ru_RU,
            fields: {
                'customerLegal.CustomerInn': {
                    validators: {
                        notEmpty: {}
                    }
                },
                'customerLegal.CustomerName': {
                    validators: {
                        notEmpty: {}
                    }
                },
                'customerLegal.CustomerAddress': {
                    validators: {
                        notEmpty: {}
                    }
                },
                'customer.DocumentSerial': {
                    validators: {
                        notEmpty: {}
                    }
                },
                'customer.DocumentNumber': {
                    validators: {
                        notEmpty: {}
                    }
                },
                'customer.DocumentIssueDate': {
                    validators: {
                        notEmpty: {}
                    }
                },
                'customer.CustomerAddress': {
                    validators: {
                        notEmpty: {}
                    }
                },
                'customer.CustomerAddressResidence': {
                    validators: {
                        notEmpty: {}
                    }
                },
                'customer.LastName': {
                    validators: {
                        notEmpty: {}
                    }
                },
                'customer.FirstName': {
                    validators: {
                        notEmpty: {}
                    }
                },
                'customer.DocumentBirthDate': {
                    validators: {
                        notEmpty: {}
                    }
                },
                'customer.DocumentBirthPlace': {
                    validators: {
                        notEmpty: {}
                    }
                },
                'customer.CustomerSnils': {
                    validators: {
                        notEmpty: {}
                    }
                }
            },
            plugins: {
                trigger: new FormValidation.plugins.Trigger(),
                bootstrap5: new FormValidation.plugins.Bootstrap5(),
                autoFocus: new FormValidation.plugins.AutoFocus(),
                submitButton: new FormValidation.plugins.SubmitButton()
            },
            init: instance => {
                instance.on('plugins.message.placed', function (e) {
                    //* Move the error message out of the `input-group` element
                    if (e.element.parentElement.classList.contains('input-group')) {
                        e.element.parentElement.insertAdjacentElement('afterend', e.messageElement);
                    }
                });
            }
        }).on('core.form.valid', () => { validationStepper.next(); });

        FormValidation3 = FormValidation.formValidation(createCaseWizardFormStep3, {
            locale: 'ru_RU',
            localization: FormValidation.locales.ru_RU,
            plugins: {
                trigger: new FormValidation.plugins.Trigger(),
                bootstrap5: new FormValidation.plugins.Bootstrap5(),
                autoFocus: new FormValidation.plugins.AutoFocus(),
                submitButton: new FormValidation.plugins.SubmitButton()
            }
        }).on('core.form.valid', () => { validationStepper.next(); });

        if (config.debug) {
            btnNextSecondStepActive.enabled();
            btnFormSubmitActive.enabled();
            for (const [key] of Object.entries(FormValidation3.getFields())) {
                FormValidation3.removeField(key);
            }
        }

        /* init Service DataTable */
        servicesTable.DataTable({
            sDom: "<'row'<'col-sm-12'tr>><'row'<'col-sm-12 col-md-5'i><'col-sm-12 col-md-7'p>>",
            processing: true,
            serverSide: true,
            ordering: false,
            filter: false,
            select: {
                info: false
            },
            drawCallback: function () {
                this.trigger("deselect");
            },
            ajax: {
                url: "/Cases/GetServicesForLegalDataJson",
                type: "POST",
                data: function (data) {
                    data.query = servicesTableSearch.val();
                }
            },
            columns: [
                {
                    title: "Услуга",
                    data: "name",
                },
                {
                    title: 'Действия',
                    render: function (data, type, row) {
                        return `<button type="button" class="btn btn-sm btn-link btn-icon p-0 text-reset" data-toggle="sidebar">
                        <i class="bi bi-info-circle cursor-pointer"></i></button>`;
                    }
                }
            ]
        })
            .on('select', () => { serviceProcedureTableVisibility.show(); })
            .on('deselect', () => {
                serviceProcedureTableVisibility.hide();
                serviceTariffTableVisibility.hide();
            });

        servicesTableSearch.on('keydown', _.debounce(() => { servicesTable.DataTable().ajax.reload(null, true); }, 500));

        /* init Service procedure DataTable */
        serviceProcedureTable.DataTable({
            sDom: "<'row'<'col-sm-12'tr>>",
            displayLength: 6,
            serverSide: true,
            ordering: false,
            filter: false,
            deferLoading: 0,
            select: {
                info: false
            },
            ajax: {
                url: "/Cases/GetServiceProceduresDataJson",
                type: "POST",
                data: function (data) {
                    data.serviceId = servicesTable.DataTable().rows({ selected: true }).data()[0]['id'];
                }
            },
            columns: [
                {
                    data: 'name'
                }
            ]
        })
            .on('select', () => { serviceTariffTableVisibility.show(); })
            .on('deselect', () => { serviceTariffTableVisibility.hide(); });

        /* init Service Tariff DataTable */
        serviceTariffTable.DataTable({
            sDom: "<'row'<'col-sm-12'tr>>",
            displayLength: 6,
            serverSide: true,
            ordering: false,
            filter: false,
            deferLoading: 0,
            select: {
                info: false
            },
            ajax: {
                url: "/Cases/GetServiceTariffDataJson",
                type: "POST",
                data: function (data) {
                    data.procedureId = serviceProcedureTable.DataTable().rows({ selected: true }).data()[0]['id'];
                }
            },
            columns: [
                {
                    data: 'tariffTypeName'
                },
                {
                    data: 'tariffOiv',
                    render: function (data, type, row) {
                        if (row.tariffOiv === 0)
                            return "<span>Бесплатно</span>";
                        return `${numeral(row.tariffOiv).format('0,0.00')} &#x20bd;`;
                    }
                },
                {
                    data: 'tariffMfc',
                    render: function (data, type, row) {
                        if (row.tariffMfc === 0)
                            return "-";
                        return `${numeral(row.tariffMfc).format('0,0.00')} &#x20bd;`;
                    }
                },
                {
                    data: 'countDayProcessing',
                    render: function (data, type, row) {
                        if (row.weekTypeId === 1)
                            return `${data} <small>${$.declOfNum(data, ["рабочий день", "рабочих дня", "рабочих дней"])}</small>`;
                        return `${data} <small>${$.declOfNum(data, ["календарный день", "календарных дня", "календарных дней"])}</small>`;
                    }
                },
                {
                    data: 'countDayExecuting',
                    render: function (data, type, row) {
                        if (row.weekTypeId === 1)
                            return `${data} <small>${$.declOfNum(data, ["рабочий день", "рабочих дня", "рабочих дней"])}</small>`;
                        return `${data} <small>${$.declOfNum(data, ["календарный день", "календарных дня", "календарных дней"])}</small>`;
                    }
                },
                {
                    data: 'countDayReturn',
                    render: function (data, type, row) {
                        if (row.weekTypeId === 1)
                            return `${data} <small>${$.declOfNum(data, ["рабочий день", "рабочих дня", "рабочих дней"])}</small>`;
                        return `${data} <small>${$.declOfNum(data, ["календарный день", "календарных дня", "календарных дней"])}</small>`;
                    }
                },
            ]
        })
            .on('select', () => { btnNextSecondStepActive.enabled(); })
            .on('deselect', () => { btnNextSecondStepActive.disabled(); });

        /* init Service Provider DataTable */
        serviceProviderTable.DataTable({
            sDom: "<'row'<'col-sm-12'tr>>",
            serverSide: true,
            ordering: false,
            filter: false,
            deferLoading: 0,
            select: {
                info: false
            },
            ajax: {
                url: "/Cases/GetServiceProvidersDataJson",
                type: "POST",
                data: function (data) {
                    data.serviceId = servicesTable.DataTable().rows({ selected: true }).data()[0]['id'];
                }
            },
            columns: [
                {
                    data: 'officeName'
                }
            ]
        })
            .on('select', () => { btnFormSubmitActive.enabled(); })
            .on('deselect', () => { btnFormSubmitActive.disabled(); });

        pickadate.datepicker({
            language: "ru",
            autoclose: true
        });

        /*suggestions*/
        customerLegal.name.suggestions({
            token: config.dadata.token,
            type: config.dadata.types.PARTY,
            onSelect: function (suggestion) {
                customerLegal.address.val(suggestion.data.address.value);
                customerLegal.inn.val(suggestion.data.inn);
                customerLegal.kpp.val(suggestion.data.kpp);
                customerLegal.ogrn.val(suggestion.data.ogrn);
                customerLegal.okato.val(suggestion.data.okato);
                customerLegal.oktmo.val(suggestion.data.oktmo);
                customerLegal.position.val("");
                if (suggestion.data.type === "LEGAL") {
                    customerLegal.position.val(suggestion.data.management.post);
                    customerLegal.firstName.val(suggestion.data.management.name.split(" ")[1]);
                    customerLegal.lastName.val(suggestion.data.management.name.split(" ")[0]);
                    customerLegal.secondName.val(suggestion.data.management.name.split(" ")[2]);
                }
                if (suggestion.data.type === "INDIVIDUAL") {
                    customerLegal.firstName.val(suggestion.data.fio.name);
                    customerLegal.lastName.val(suggestion.data.fio.surname);
                    customerLegal.secondName.val(suggestion.data.fio.patronymic);
                }
            }
        });
        customer.firstName.suggestions({
            token: config.dadata.token,
            type: config.dadata.types.NAME,
            params: {
                parts: ["NAME"]
            },
            onSelect: function (suggestion) {
                setGender.identify(suggestion.data.gender);
            }
        });
        customer.lastName.suggestions({
            token: config.dadata.token,
            type: config.dadata.types.NAME,
            params: {
                parts: ["SURNAME"]
            },
            onSelect: function (suggestion) {
                setGender.identify(suggestion.data.gender);
            }
        });
        customer.secondName.suggestions({
            token: config.dadata.token,
            type: config.dadata.types.NAME,
            params: {
                parts: ["PATRONYMIC"]
            },
            onSelect: function (suggestion) {
                setGender.identify(suggestion.data.gender);
            }
        });
        customer.documentCode.suggestions({
            token: config.dadata.token,
            type: config.dadata.types.FMS,
            onSelect: function (suggestion) {
                customer.documentCode.val(suggestion.data.code);
                customer.documentIssueBody.val(suggestion.value);
            }
        });
        customer.address.suggestions({
            token: config.dadata.token,
            type: config.dadata.types.ADDRESS
        });
        customer.addressResidence.suggestions({
            token: config.dadata.token,
            type: config.dadata.types.ADDRESS
        });
        customer.documentBirthPlace.suggestions({
            token: config.dadata.token,
            type: config.dadata.types.ADDRESS
        });
        customer.addressTemp.suggestions({
            token: config.dadata.token,
            type: config.dadata.types.ADDRESS
        });

        /*inputmask*/
        pickadate.inputmask("99.99.9999", { clearIncomplete: true, showMaskOnHover: false });
        customer.documentSerial.inputmask("99 99", { clearIncomplete: true, showMaskOnHover: false, oncomplete: function () { findCustomerByDocument.load(); } });
        customer.documentNumber.inputmask("999999", { clearIncomplete: true, showMaskOnHover: false, oncomplete: function () { findCustomerByDocument.load(); } });
        customer.documentCode.inputmask("999-999", { clearIncomplete: true, showMaskOnHover: false });
        customer.snils.inputmask("999-999-999 99", { clearIncomplete: true, showMaskOnHover: false });
        customer.inn.inputmask("999999999999", { clearIncomplete: true, showMaskOnHover: false });
        customer.phone1.inputmask("+7(999) 999-99-99", { clearIncomplete: true, showMaskOnHover: false });
        customer.phone2.inputmask("+7(999) 999-99-99", { clearIncomplete: true, showMaskOnHover: false });
        customerLegal.phone1.inputmask("+7(999) 999-99-99", { clearIncomplete: true, showMaskOnHover: false });
        customerLegal.phone2.inputmask("+7(999) 999-99-99", { clearIncomplete: true, showMaskOnHover: false });

        createCaseWizardNext.forEach(item => {
            item.addEventListener('click', event => {
                switch (validationStepper._currentIndex) {
                    case 0:
                        FormValidation1.validate();
                        break;

                    case 1:
                        FormValidation2.validate();
                        break;

                    case 2:
                        FormValidation3.validate();
                        break;
                }
            });
        });

        createCaseWizardPrev.forEach(item => {
            item.addEventListener('click', event => {
                switch (validationStepper._currentIndex) {
                    case 3:
                        validationStepper.previous();
                        break;

                    case 2:
                        validationStepper.previous();
                        break;

                    case 1:
                        additionalForms.clear();
                        validationStepper.previous();
                        break;

                    case 0:

                    default:
                        break;
                }
            });
        });

        btnFormSubmit.on("click", () => {
            Swal.fire({
                title: 'Вы уверены?',
                text: 'Внимательно проверьте введенные данные',
                showCancelButton: true,
                confirmButtonText: 'Сохранить',
                cancelButtonText: 'Отмена',
                showLoaderOnConfirm: true,
                customClass: {
                    confirmButton: 'btn btn-success me-3',
                    cancelButton: 'btn btn-label-secondary'
                },
                backdrop: true,
                allowOutsideClick: () => !Swal.isLoading(),
                preConfirm: () => {
                    const formData = new FormData(createCaseWizardForm);
                    if (config.debug == false) {
                        formData.append("serviceId", servicesTable.DataTable().rows({ selected: true }).data()[0]['id']);
                        formData.append("procedureId", serviceProcedureTable.DataTable().rows({ selected: true }).data()[0]['id']);
                        formData.append("tariffId", serviceTariffTable.DataTable().rows({ selected: true }).data()[0]['id']);
                        formData.append("providerId", serviceProviderTable.DataTable().rows({ selected: true }).data()[0]['id']);
                    } else {
                        formData.append("serviceId", "00000000-0000-0000-0000-000000000000");
                        formData.append("procedureId", "00000000-0000-0000-0000-000000000000");
                        formData.append("tariffId", "00000000-0000-0000-0000-000000000000");
                        formData.append("providerId", "00000000-0000-0000-0000-000000000000");
                    }
                    $.ajax({
                        url: '/Cases/CreateLegalSave',
                        type: 'POST',
                        data: formData,
                        processData: false,
                        contentType: false,
                        beforeSend: () => {
                            $.pageBlock();
                            btnFormSubmit.spinnerBtn("start");
                        },
                        complete: () => {
                            $.unblockUI();
                            btnFormSubmit.spinnerBtn("stop");
                        },
                        success: function (data) {
                            var resultJson = JSON.parse(data);
                            if (resultJson.CreateCaseStatus.IsCreated) {
                                var tariff = resultJson.Tariff === 0 ? "<span>Бесплатно</span>" : `${numeral(resultJson.Tariff).format('0,0.00')} &#x20bd;`;
                                var htmlContent = `<dd class="col-12 mb-0">
                                            <h3>Готово! 🚀</h3>
                                            <p>Услуга <a href="javascript:void(0)">#${resultJson.CaseId}</a> создана.</p>
                                        </dd>

                                        <dt class="col-sm-3">Услуга</dt>
                                        <dd class="col-sm-9">${resultJson.Service}</dd>

                                        <dt class="col-sm-3">Стоимость</dt>
                                        <dd class="col-sm-9">${tariff}</dd>

                                        <dt class="col-sm-3">Заявитель</dt>
                                        <dd class="col-sm-9">${resultJson.Customer}</dd>

                                        <dt class="col-sm-3">Сотрудник</dt>
                                        <dd class="col-sm-9">${resultJson.Employee}</dd>

                                        <dt class="col-sm-3">Регламентная дата</dt>
                                        <dd class="col-sm-9"><span class="b-s-16">${moment(resultJson.DataReg).format("DD.MM.yyyy")}</span></dd>`;
                                createCaseWizardFormStep5.querySelector('dl').insertAdjacentHTML("beforeend", htmlContent);
                                validationStepper.next();
                                createCaseWizardFormStep1.remove();
                                createCaseWizardFormStep3.remove();
                                createCaseWizardFormStep4.remove();
                            }
                            else {
                                $.unblockUI();
                                btnFormSubmit.spinnerBtn("stop");
                            }
                        }
                    });
                }
            });
        });
    };

    const initMain = () => {
        initComponents();
    }

    initMain();
});