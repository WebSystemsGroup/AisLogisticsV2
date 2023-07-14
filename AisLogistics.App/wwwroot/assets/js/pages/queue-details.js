let closeSubmenu = null;
let openSubmenu = null;



$(document).ready(function () {
    let hubConnection = new signalR.HubConnectionBuilder().withUrl("/NotificationHub").build();
    hubConnection.on("Queue", function (message) {
        queueRefresh();
    });

    hubConnection.start().catch(function (err) {
        return console.error(err.toString());
    });

    let submenuBtns = document.querySelectorAll('.scoreboard-dropdown .btn-submenu');
    let queuewidth = document.getElementById('electronicQueue');
    openSubmenu = (el) => {
        let openedSubmenu = document.querySelector('.scoreboard-submenu--open');
        
        if (openedSubmenu) {
            let btn = openedSubmenu.previousElementSibling.querySelector('.btn-submenu');
            openedSubmenu.classList.remove('scoreboard-submenu--open');
            queuewidth.classList.remove('dropdown-queue-max');
            btn.addEventListener('click', openSubmenu);
            btn.removeEventListener('click', closeSubmenu);
        }

        let currentScoreboard = el.target.closest('.btn-group').nextElementSibling;
        currentScoreboard.classList.add('scoreboard-submenu--open'); 
        queuewidth.classList.add('dropdown-queue-max');

        let submenuCurrent = el.target.closest('.btn-submenu')

        submenuCurrent.removeEventListener('click', openSubmenu);
        submenuCurrent.addEventListener('click', closeSubmenu);
    };

    closeSubmenu = (el) => {
        let currentScoreboard = el.target.closest('.btn-group').nextElementSibling;
        currentScoreboard.classList.remove('scoreboard-submenu--open');
        queuewidth.classList.remove('dropdown-queue-max');

        let submenuCurrent = el.target.closest('.btn-submenu')

        submenuCurrent.addEventListener('click', openSubmenu);
        submenuCurrent.removeEventListener('click', closeSubmenu);
    };

    submenuBtns.forEach(function (it) {
        it.addEventListener('click', openSubmenu);
    });
});

function closeAllSubmenu() {

    const myDropdown = document.getElementById('queueDropdown')
    myDropdown.addEventListener('hide.bs.dropdown', event => {
        $('#scoreboard').hide();
        $('#scoreboard ul').empty();
    })


    //let queuewidth = document.getElementById('electronicQueue');
    //queuewidth.classList.remove('dropdown-queue-max');

    //document.querySelectorAll(`${id} .scoreboard-submenu`).forEach(item => {
    //    item.classList.remove('scoreboard-submenu--open');
    //})

    //document.querySelectorAll(`${id} .btn-submenu`).forEach(item => {
    //    item.addEventListener('click', openSubmenu);
    //    item.removeEventListener('click', closeSubmenu);
    //})
};


var $divQueueMain = $("#electronicQueueContent");
refreshNum($divQueueMain);
$(document).on('show.bs.dropdown', '#electronicQueue', function () {//показываем блок            
    refreshNum($divQueueMain);
});
$(document).on('click', '[data-redirect-abon]', function () { //перенаправить заявителя в окно 
    var id = $(this).data('redirectAbon'),
        Num = $('#queue_num').val();
    redirectAbon(Num, id);
});
$('[data-queue-next]').on('click', function () { //вызов следующего заявителя или завершить
    var EndCall = $(this).data('queueNext'),
        Num = $('#queue_num').val();
    nextAbon(Num, EndCall);
});
$('[data-queue-next-vip]').on('click', function () { //вызов следующего заявителя за справкой или завершить
    var EndCall = $(this).data('queueNextVip'),
        Num = $('#queue_num').val();
    nextAbonVip(Num, EndCall);
});

$('.queue-redirect').on('click', function () { //список окон для передачи заявителя 
    var $target = $('#scoreboard-redirect-list ul');
    if ($('#queue_num').val()) {
        $.ajax({
            url: "/Queue/jsonListWindow",
            type: "GET",
            beforeSend: function () {
                //$target.modal('show');
                $target.html('<div style="-webkit-box-pack: center;justify-content : center;-webkit-box-align: center;align-items : center;display: flex;height: 255px; margin-right: -5px;"><i class="fa fa-spin fa-spinner fa-2x" ></i></div>');
            },
            success: function (data) {
                if (data.errorCode === 500) {
                    $target.html('Ошибка получения списка');
                }
                else if (data.windows) {
                    //$target.find('.modal-body').html('<ul class="list-unstyled"></ul>');
                    $target.empty();
                     $.map(data.windows, function (item, index) {
                        $('<li>', {
                            class: "d-flex justify-content-between align-items-center p-2 border-bottom",
                            append: $('<b>', {
                                text: `${item.name}`,
                            }).add($('<button>', {
                                class: "btn btn-info btn-sm waves-effect waves-light",
                                "data-toggle": "tooltip",
                                "data-placement": "left",
                                "data-container": "#scoreboard-redirect-list",
                                "data-redirect-abon": `${item.id}`,
                                "title": "Передать",
                                append: $('<i>', {
                                    class: "md md-exit-to-app",
                                }),
                                on: {
                                    click: function () {
                                        var id = $(this).data('redirectAbon'),
                                            Num = $('#queue_num').val();
                                        redirectAbon(Num, id);
                                    }
                                }
                            }).tooltip())
                        }).appendTo($target);
                     })

                    new PerfectScrollbar('#scoreboard-redirect-list ul');
                }
                else
                {
                   $target.empty().append('<li class="d-flex justify-content-between align-items-center p-2 border-bottom"><span class="text-muted">Нет данных<span></li>')
                }
            }
        });
    } else {
        $target.html('Абонент еще не вызван');
    }
});
$('.queue-deferred').on('click', function () { //отложить заявителя 
    if ($('#queue_num').val() !== "") {
        $.ajax({
            url: "/Queue/jsonDelayAbon",
            data: { num: $('#queue_num').val() },
            type: "POST",
            success: function (data) {
                if (data.errorCode === 500) {
                } else {
                    refreshNum($("#electronicQueueContent"));
                }
            }
        });
    }
});

$('.count-abon').on('click', function () { // список заявителей в очереди
    var $target = $('#scoreboard ul');
    $('#scoreboard').show();

    $.ajax({
        url: "/Queue/jsonListAbonInQueue",
        type: "GET",
        beforeSend: function () {
            $target.html('<div style="-webkit-box-pack: center;justify-content : center;-webkit-box-align: center;align-items : center;display: flex;height: 255px; margin-right: -5px;"><i class="fa fa-spin fa-spinner fa-2x" ></i></div>');
        },
        success: function (data) {
            if (data.errorCode === 500) {
                $target.empty().append('<li class="text-center"><span class="text-muted">Ошибка получения списка</span></li>')
            }
            else if (data.abonents)
            {
                $target.empty();
                $.map(data.abonents, function (value, index) {
                    $('<li>', {
                        class: "list-group-item text-primary ps-0",
                        text: `${value.num}`,
                    }).appendTo($target);
                });
            }
            else {
                $target.empty().append('<li class="text-center"><span class="text-muted">Нет данных</span></li>')
            }
        }
    });
});

$('.redirect-abon').on('click', function () { //список переданных заявителей
    var $target = $('#scoreboard ul');
    $('#scoreboard').show();

    $.ajax({
        url: "/Queue/jsonListAbonRedirect",
        type: "GET",
        beforeSend: function () {
            $target.html('<div style="-webkit-box-pack: center;justify-content : center;-webkit-box-align: center;align-items : center;display: flex;height: 255px; margin-right: -5px;"><i class="fa fa-spin fa-spinner fa-2x" ></i></div>');
        },
        success: function (data) {
            if (data.errorCode === 500) {
                $target.empty().append('<li class="text-center"><span class="text-muted">Ошибка получения списка</span></li>')
            } else if (data.abonents) {
                $target.empty();
                $.map(data.abonents, function (value, index) {
                    $('<li>', {
                        class: "list-group-item text-primary ps-0",
                        text: `${value.num}`,
                    }).appendTo($target);
                });
            } else {
                $target.empty().append('<li class="text-center"><span class="text-muted">Ошибка получения списка</span></li>')
            }
        }
    });
});
$('.deferred-abon').on('click', function () { //список отложенных заявителей
    var $target = $('#scoreboard ul');
    $('#scoreboard').show();

    $.ajax({
        url: "/Queue/jsonListAbonDelay",
        type: "GET",
        beforeSend: function () {
            $target.html('<div style="-webkit-box-pack: center;justify-content : center;-webkit-box-align: center;align-items : center;display: flex;height: 255px; margin-right: -5px;"><i class="fa fa-spin fa-spinner fa-2x" ></i></div>');
        },
        success: function (data) {
            if (data.errorCode === 500) {
                $target.empty().append('<li class="text-center"><span class="text-muted">Ошибка получения списка</span></li>')
            } else if (data.abonents) {
                $target.empty();
                $.map(data.abonents, function (value, index) {
                    $('<li>', {
                        class: "list-group-item text-primary ps-0",
                        text: `${value.num}`,
                    }).appendTo($target);
                });

            } else {
                $target.empty().append('<li class="text-center"><span class="text-muted">Ошибка получения списка</span></li>')
            }
        }
    });
});
$('[data-queue-reject-btn]').on('click', function () { // отклонить клиента
    let $this = $(this);
    if ($this.data('queueRejectBtn') == "yes") {
        $.ajax({
            url: "/Queue/jsonRejectAbon",
            type: "GET",
            success: function (data) {
                if (data.errorCode === 500) {
                    Lobibox.notify('error', {
                        size: 'mini',
                        delay: false,
                        position: 'top right',
                        msg: data.ErrorText,
                        soundPath: '../Content/libs/lobibox/sounds/',
                        sound: 'error_sound'
                    });
                } else {
                    Lobibox.notify('success', {
                        size: 'mini',
                        delay: false,
                        position: 'top right',
                        msg: "Клиент отклонен!",
                        soundPath: '../Content/libs/lobibox/sounds/',
                        sound: 'error_sound'
                    });
                    $('[data-queue-reject-btn]').trigger('click');
                    refreshNum($("#electronicQueueContent"));
                }
            }
        });
    }
    else {
        $('[data-queue-reject-btn]').trigger('click');
    }

});

$('#predRegistration').on('click', function () { // предзапись
    $.ajax({
        url: '/Queue/AddPreRegestrationModal',
        type: 'Post',
        beforeSend: function () {
            $.blockUI(LoaderContent);
        },
        success: function (content) {
            $('#mainModal').html(content).modal('show');
        },
        error: function (XMLHttpRequest, textStatus, errorThrown) {
            $.unblockUI();
            
        },
        complete: function () {
            $.unblockUI();
        }
    });
});

$('#cancelRegistration').on('click', function () { // отмена
    $.ajax({
        url: '/Queue/AddCancelPreRegestrationModal',
        type: 'Post',
        beforeSend: function () {
            $.blockUI(LoaderContent);
        },
        success: function (content) {
            $('#mainModal').html(content).modal('show');
        },
        error: function (XMLHttpRequest, textStatus, errorThrown) {
            $.unblockUI();
          
        },
        complete: function () {
            $.unblockUI();
        }
    });
});

function refreshNum($divUl) { //обновление данных 
    RefreshCountsQueue($divUl);
    $.ajax({
        url: "/Queue/jsonNumAbonOnWindow",
        type: "GET",
        beforeSend: function () {
            $divUl.find('.line_text').html(' --- ');
        },
        success: function (data) {
            RefreshDataQueue($divUl, data)
        },

    });
    closeAllSubmenu();
}

function queueTimer(el, t, method, serverTime) {
    $(el).countdown({
        since: -((serverTime - t) / 1000),
        //until: +t,
        format: 'M:S',
        compact: true,
        //onExpiry: method
    });
}

function queueDateFormatter(date) {
    let dtArray = date.split(' ');
    let str = "";
    let dateArray = dtArray[0].split('.');
    str = dateArray[2] + '/' + dateArray[1] + '/' + dateArray[0] + " " + dtArray[1];
    let qTime = new Date(str);
    return qTime;
}

function queueRefresh() {
    let divUI = $("#electronicQueueContent");
    let $target = $('#scoreboard-next ul');
    $.ajax({
        url: "/Queue/jsonListAbonInQueue",
        type: "GET",
        beforeSend: function () {
        },
        success: function (data) {
            if (data.errorCode === 500) {
            } else {
                queueRefreshCount();
                if ($target.closest('.scoreboard-submenu').hasClass('scoreboard-submenu--open')
                    && data.abonents != null) {
                    if ($target.find('li').length > 0) {
                        $target.find('li').each(function (i, item) {
                            if (data.abonents.filter(a => a.num == $(item).find('button').data("nextAbon")).length == 0) {
                                $(item).find('button').attr('disabled', 'disabled').removeClass('btn-success');
                            }
                        })
                        $.map(data.abonents, function (value, index) {
                            if ($target.find('button[data-next-abon="' + value.num + '"]').length == 0) {
                                $('<li>', {
                                    class: "d-flex justify-content-between align-items-center p-2 border-bottom",
                                    append: $('<b>', {
                                        text: `${value.num}`,
                                    })
                                        .add($('<button>', {
                                            class: "btn btn-success btn-sm waves-effect waves-light",
                                            "data-toggle": "tooltip",
                                            "data-placement": "left",
                                            "data-container": "#scoreboard-next",
                                            "data-next-abon": `${value.num}`,
                                            "title": "Вызвать",
                                            append: $('<i>', {
                                                class: "md md-system-update-tv",
                                            }),
                                            on: {
                                                click: function () {
                                                    nextAbon(`${value.num}`, 0);
                                                }
                                            }
                                        })
                                        )
                                }).appendTo($target);
                            }
                        });
                    }
                }
            }
        },
        complete: function () {

        }
    });
}

function queueRefreshCount() {
    let divUI = $("#electronicQueueContent");
    let count_abon = divUI.find('span.count-abon');
    let count_abon_spravka = divUI.find('span.count-abon-spravka');
    let redirect_abon = divUI.find('span.redirect-abon');
    let deferred_abon = divUI.find('span.deferred-abon');
    let head_count = $('#queueHeadCountAbon');
    let head_all = divUI.find('#queueAllAbonentsCount');
    let count_abon_val = count_abon.html();
    let count_abon_spravka_val = count_abon_spravka.html();
    let redirect_abon_val = redirect_abon.html();
    let deferred_abon_val = deferred_abon.html();
    let head_count_val = head_count.html();
    let head_all_val = head_all.html();
    $.ajax({
        url: "/Queue/jsonCountAbon",
        type: "GET",
        beforeSend: function () {
            //console.log('ЭО: Идет запрос количества абонентов');
        },
        success: function (data) {
            if (data.errorCode === 500) {
            } else {
                count_abon.addClass('delay-3s animated flash');
                count_abon.html(data.countAbon);

                count_abon_spravka.addClass('delay-3s animated flash');
                count_abon_spravka.html(data.countAbonVIP);

                if (redirect_abon_val != data.countRedirect) {
                    redirect_abon.addClass('delay-3s animated flash');
                    redirect_abon.html(data.countRedirect);
                }
                if (deferred_abon_val != data.countDelay) {
                    deferred_abon.addClass('delay-3s animated flash');
                    deferred_abon.html(data.countDelay);
                }
                if (head_all_val != Number(data.countAbon) + Number(data.countAbonVIP)) {
                    head_all.addClass('delay-3s animated flash');
                    head_all.html(Number(data.countAbon) + Number(data.countAbonVIP));
                }
                //if (head_count_val != Number(data.CountAbon) + Number(data.CountAbonVIP)) {
                head_count.addClass('delay-3s animated flash');
                head_count.text(Number(data.countAbon) + Number(data.countAbonVIP));
                //}

                setTimeout(function () {
                    divUI.find('span.badge').removeClass('flash');
                    head_count.removeClass('flash');
                }, 2000)
            }
        },
        complete: function () {

        }
    });
}


function RefreshDataQueue(divUI, data) {
    if (data.errorCode === 500) {
        divUI.html('<span style="color: black!important;">Сервис временно недоступен.</span>');
    }
    else
    {
       /* $('#scoreboard').show();*/
        $('#queueMyWindowNum').html(data.window_id == null ? '' : data.window_id)
        divUI.find('.line_text').html(data.num == null ? ' --- ' : data.num);
        $('#electronicQueueActiveNum').text(data.num == null ? '' : data.num);
        $('#queue_num').val(data.num);
        //счетчик времени 
        $("#electronicQueueActiveTime").countdown('destroy');
        $("#queueActiveTime").countdown('destroy');
        if (data.ticket_сalled_timestamp != null) {
            let qTime = queueDateFormatter(data.ticket_сalled_timestamp);
            let qsTime = queueDateFormatter(data.server_time);
            queueTimer("#electronicQueueActiveTime", qTime, "none", qsTime);
            queueTimer("#queueActiveTime", qTime, "none", qsTime);
        }

        //глобальный id талона
        //globalTicketCalledId = data.ticket_mfc_uuid;
    }
}
function RefreshCountsQueue(divUI) {
    $.ajax({
        url: "/Queue/jsonCountAbon",
        type: "GET",
        beforeSend: function () {
            divUI.find('button').attr('disabled', true);

        },
        success: function (data) {
            if (data.errorCode === 500) {
                divUI.html('<span style="color: black!important;">Сервис временно недоступен.</span>');
            } else {
                divUI.find('span.count-abon').html(data.countAbon);
                divUI.find('span.count-abon-spravka').html(data.countAbonVIP);
                divUI.find('span.redirect-abon').html(data.countRedirect);
                divUI.find('span.deferred-abon').html(data.countDelay);
                divUI.find('#queueAllAbonentsCount').html(Number(data.countAbon) + Number(data.countAbonVIP));
                $('#queueHeadCountAbon').text(Number(data.countAbon) + Number(data.countAbonVIP));
            }
        },
        complete: function () {
            divUI.find('button').attr('disabled', false);
        }
    });
}

function nextAbon(Num, EndCall) { //вызов следующего заявителя
    $.ajax({
        url: "/Queue/jsonNextAbon",
        type: "POST",
        data: { EndCall: EndCall, Num: Num },
        success: function (data) {
            $('#electronicQueueContent .line_text').html(data.num);
        },
        complete: function (data) {
            refreshNum($("#electronicQueueContent"));
        }
    });
}
function nextAbonVip(Num, EndCall) { //вызов следующего заявителя за справкой
    $.ajax({
        url: "/Queue/jsonNextAbonVip",
        type: "POST",
        data: { EndCall: EndCall, Num: Num },
        success: function (data) {
            $('#electronicQueueContent .line_text').html(data.num);
        },
        complete: function (data) {
            refreshNum($("#electronicQueueContent"));
        }
    });
}
function nextRedirectAbon(Num) { //вызов переданного заявителя
    $.ajax({
        url: "/Queue/jsonNextAbonRedirect",
        type: "POST",
        data: { Num: Num },
        success: function (data) {
            $('#electronicQueueContent .line_text').html(data.num);
        },
        complete: function () {
            refreshNum($("#electronicQueueContent"));
        }
    });
}
function nextDeferredAbon(Num) { //вызов отложенного заявителя
    $.ajax({
        url: "/Queue/jsonNextAbonDelay",
        type: "POST",
        data: { Num: Num },
        success: function (data) {
            $('#electronicQueueContent .line_text').html(data.num);
        },
        complete: function () {
            refreshNum($("#electronicQueueContent"));
        }
    });
}
function redirectAbon(Num, Id) { //метод перенаправления заявителя в окно
    $.ajax({
        url: "/Queue/jsonAbonRedirect",
        type: "POST",
        data: { window_id: Id, Num: Num },
        success: function (data) {
        },
        complete: function () {
            refreshNum($("#electronicQueueContent"));
        }
    });
}





