$(document).ready(function () {
    const $tabs = $(".history-tabs .history-tab-item");
    const $resultsContainer = $("#history-results");
    let currentStatus = "tất cả";

    const authRaw = getAuthState();
    if (!authRaw) {
        $resultsContainer.html('<p class="no-results">Vui lòng đăng nhập để xem lịch sử.</p>');
        return;
    }
    const auth = JSON.parse(authRaw);

    // --- 1. Hàm gọi AJAX để tải dữ liệu ---
    function fetchHistory(status) {
        currentStatus = status;
        $resultsContainer.html('<div class="loading-spinner">Đang tải...</div>');

        $.ajax({
            url: "/Account/GetHistoryData",
            type: "GET",
            data: {
                username: auth.username,
                status: status
            },
            success: function (res) {
                // SỬA LỖI: Đọc dữ liệu từ res.list
                if (!res.success || !res.list) {
                    $resultsContainer.html(`<p class="no-results">Lỗi: ${res.message || 'Không thể tải dữ liệu.'}</p>`);
                    return;
                }

                $resultsContainer.empty();
                const data = res.list;

                if (data.length === 0) {
                    $resultsContainer.html('<p class="no-results">Không tìm thấy lịch sử nào.</p>');
                    return;
                }

                data.forEach(item => {
                    let statusClass = "status-cho-xac-nhan";
                    // Ưu tiên trạng thái Hóa đơn (Đã thanh toán, Đã hủy, v.v.)
                    let statusText = item.trangThaiHoaDon || item.trangThaiDatBan;
                    let actionButtonsHtml = '';
                    let slideClass = '';

                    // Gán class CSS dựa trên text
                    switch (statusText) {
                        case "Đã xác nhận":
                            statusClass = "status-da-xac-nhan";
                            break;
                        case "Đang phục vụ":
                            statusClass = "status-dang-phuc-vu";
                            break;
                        case "Đã thanh toán":
                            statusClass = "status-da-thanh-toan";
                            break;
                        case "Đã hủy":
                            statusClass = "status-da-huy";
                            break;
                        default: // Chờ xác nhận
                            statusClass = "status-cho-xac-nhan";
                    }

                    // TẠO NÚT HÀNH ĐỘNG DỰA TRÊN TRẠNG THÁI
                    if (item.trangThaiDatBan === "Chờ xác nhận") {
                        actionButtonsHtml = `
                            <button class="action-btn view" data-id="${item.datBanId}">Xem</button>
                            <button class="action-btn cancel" data-id="${item.datBanId}">Hủy</button>
                        `;
                        slideClass = 'actions-open';
                    } else {
                        actionButtonsHtml = `
                            <button class="action-btn view" data-id="${item.datBanId}">Xem</button>
                        `;
                        slideClass = 'actions-open-single';
                    }

                    // TẠO HTML MỚI (Đảo ngược thứ tự)
                    const itemHtml = `
                        <div class="history-item" id="booking-item-${item.datBanId}">
                            <div class="history-item-content" data-slide-class="${slideClass}">
                                <div class="history-item-info">
                                    <h4>Bàn: ${item.tenBanPhong}</h4>
                                    <p>Ngày đến: ${item.ngayDen}</p>
                                    <p>Số người: ${item.soNguoi}</p>
                                </div>
                                <div class="history-item-status ${statusClass}">
                                    ${statusText}
                                </div>
                                <div class="history-item-action">
                                    <button class="btn-options" aria-label="Tùy chọn">⋮</button>
                                </div>
                            </div>

                            <div class="history-item-actions">
                                ${actionButtonsHtml}
                            </div>
                        </div>
                    `;
                    $resultsContainer.append(itemHtml);
                });
            },
            error: function (xhr) {
                $resultsContainer.html(`<p class="no-results">Lỗi tải dữ liệu: ${xhr.responseJSON?.message || 'Lỗi server'}</p>`);
            }
        });
    }

    // --- 2. Gắn sự kiện Click cho các Tab ---
    $tabs.on("click", function () {
        $tabs.removeClass("active");
        $(this).addClass("active");
        const status = $(this).data("status");
        fetchHistory(status);
    });

    // --- 3. SỰ KIỆN MỚI: Click nút Ba Chấm (Mở menu trượt) ---
    $resultsContainer.on("click", ".btn-options", function (e) {
        e.preventDefault();
        e.stopPropagation();

        const $content = $(this).closest('.history-item-content');
        const slideClass = $content.data('slide-class');

        // Đóng tất cả các item khác
        $('.history-item-content.actions-open, .history-item-content.actions-open-single').not($content).removeClass('actions-open actions-open-single');

        // Mở/đóng item này
        $content.toggleClass(slideClass);
    });

    // --- 4. SỰ KIỆN MỚI: Đóng menu khi click vào nội dung ---
    $resultsContainer.on("click", ".history-item-content", function (e) {
        const $content = $(this);
        if ($content.hasClass('actions-open') || $content.hasClass('actions-open-single')) {
            e.preventDefault();
            e.stopPropagation();
            $content.removeClass('actions-open actions-open-single');
        }
    });

    // --- 5. SỰ KIỆN MỚI: Click nút Hủy (bên trong panel) ---
    $resultsContainer.on("click", ".action-btn.cancel", function (e) {
        e.preventDefault();
        e.stopPropagation();

        const $btn = $(this);
        const datBanId = $btn.data("id");

        if (!confirm("Bạn có chắc chắn muốn hủy đơn đặt bàn này?")) {
            return;
        }

        $.ajax({
            url: "/Account/CancelBooking",
            type: "POST",
            contentType: "application/json",
            data: JSON.stringify({
                username: auth.username,
                datBanId: datBanId
            }),
            beforeSend: function () {
                $btn.prop("disabled", true).text("Đang hủy...");
            },
            success: function (res) {
                if (res.success) {
                    alert(res.message);
                    fetchHistory(currentStatus); // Tải lại danh sách
                } else {
                    alert("Lỗi: " + res.message);
                    $btn.prop("disabled", false).text("Hủy");
                }
            },
            error: function (xhr) {
                alert("Lỗi server: " + (xhr.responseJSON?.message || "Không thể kết nối."));
                $btn.prop("disabled", false).text("Hủy");
            }
        });
    });

    $resultsContainer.on("click", ".action-btn.view", function (e) {
        e.preventDefault();
        e.stopPropagation();
        const datBanId = $(this).data("id");

        // Chuyển hướng sang trang chi tiết
        window.location.href = `/Account/HistoryDetail/${datBanId}`;
    });

    // --- 7. Tải dữ liệu ban đầu ---
    fetchHistory("tất cả");

});

