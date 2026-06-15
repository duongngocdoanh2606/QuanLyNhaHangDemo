(function () {
    if (typeof signalR === 'undefined') return;

    const connection = new signalR.HubConnectionBuilder()
        .withUrl('/hubs/order')
        .withAutomaticReconnect()
        .build();

    let notificationCount = 0;

    function updateBadge(count) {
        const badge = document.getElementById('notif-badge');
        if (!badge) return;
        notificationCount = count;
        if (count > 0) {
            badge.textContent = count > 99 ? '99+' : count;
            badge.classList.remove('d-none');
        } else {
            badge.classList.add('d-none');
        }
    }

    function renderNotifications(items) {
        const list = document.getElementById('notif-list');
        if (!list) return;

        if (!items || items.length === 0) {
            list.innerHTML = '<li class="dropdown-item text-muted small">Không có thông báo mới</li>';
            return;
        }

        list.innerHTML = items.map(n => `
            <li>
                <a class="dropdown-item small notif-item" href="/Admin/Table" data-id="${n.id}">
                    <i class="fas fa-bell text-success me-1"></i>
                    ${n.message}
                    <br><span class="text-muted" style="font-size:0.75rem">${new Date(n.createdAt).toLocaleTimeString('vi-VN')}</span>
                </a>
            </li>`).join('');
    }

    function loadNotifications() {
        fetch('/Admin/Notification/Unread')
            .then(r => r.json())
            .then(data => {
                updateBadge(data.count);
                renderNotifications(data.items);
            })
            .catch(() => {});
    }

    function showToast(message) {
        if (typeof Swal !== 'undefined') {
            const Toast = Swal.mixin({
                toast: true,
                position: 'top-end',
                showConfirmButton: false,
                timer: 5000,
                timerProgressBar: true
            });
            Toast.fire({ icon: 'success', title: message });
        }
    }

    connection.on('DishReady', function (payload) {
        loadNotifications();
        showToast(payload.message || 'Có món sẵn sàng phục vụ');
        if (typeof window.onTableReadyUpdate === 'function') {
            window.onTableReadyUpdate(payload);
        }
    });

    connection.on('FloorPlanRefresh', function (statuses) {
        if (typeof window.applyFloorPlanStatus === 'function') {
            window.applyFloorPlanStatus(statuses);
        }
    });

    connection.on('TableReadyUpdate', function (payload) {
        if (typeof window.onTableReadyUpdate === 'function') {
            window.onTableReadyUpdate(payload);
        }
    });

    connection.on('KitchenRefresh', function () {
        if (typeof window.onKitchenRefresh === 'function') {
            window.onKitchenRefresh();
        } else if (document.getElementById('kitchen-board')) {
            location.reload();
        }
    });

    connection.on('DishFired', function (payload) {
        showToast(payload.message || 'Yêu cầu fire / làm lại món');
        if (typeof window.onKitchenRefresh === 'function') {
            window.onKitchenRefresh();
        }
    });

    // Khi có đơn hàng mới hoặc thêm món từ điện thoại → auto refresh trang Order
    connection.on('OrderListRefresh', function () {
        if (document.getElementById('myTable')) {
            $.get(location.href, function (data) {
                var newDoc = new DOMParser().parseFromString(data, 'text/html');
                var newTableHTML = $(newDoc).find('#myTable').html();
                var dt = $('#myTable').DataTable();
                if (dt) dt.destroy();
                $('#myTable').html(newTableHTML);
                $('#myTable').DataTable({
                    "language": {
                        "url": "//cdn.datatables.net/plug-ins/1.13.6/i18n/vi.json"
                    }
                });
            });
        } else if (document.getElementById('detail_order')) {
            $.get(location.href, function (data) {
                var newDoc = new DOMParser().parseFromString(data, 'text/html');
                var newTableHTML = $(newDoc).find('#detail_order').html();
                var dt = $('#detail_order').DataTable();
                if (dt) dt.destroy();
                $('#detail_order').html(newTableHTML);
                new DataTable('#detail_order', {
                    layout: {
                        topStart: {
                            buttons: ['pdf', 'print']
                        }
                    }
                });
            });
        }
    });

    connection.start()
        .then(() => connection.invoke('JoinAdmin'))
        .then(loadNotifications)
        .catch(err => console.warn('SignalR:', err));

    document.getElementById('btn-mark-all-read')?.addEventListener('click', function (e) {
        e.preventDefault();
        fetch('/Admin/Notification/MarkAllRead', { method: 'POST' })
            .then(() => loadNotifications());
    });

    document.getElementById('notif-list')?.addEventListener('click', function (e) {
        const item = e.target.closest('.notif-item');
        if (!item) return;
        const id = item.dataset.id;
        if (id) {
            fetch(`/Admin/Notification/MarkRead/${id}`, { method: 'POST' });
        }
    });

    setInterval(loadNotifications, 60000);
})();
