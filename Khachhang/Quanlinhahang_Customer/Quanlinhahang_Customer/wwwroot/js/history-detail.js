const vnd = (n) => (n || 0).toLocaleString("vi-VN", { style: "currency", currency: "VND" });

$(document).ready(function () {
    const $fieldset = $("#infoFieldset");
    const $form = $("#updateBookingForm");
    const $btnEdit = $("#btnEdit");
    const $btnSave = $("#btnSave");
    const $btnCancel = $("#btnCancel");
    const $errorMsg = $("#infoError");
    const $successMsg = $("#infoSuccess");
    const $summaryBody = $("#summaryBodyItems");
    const $summaryTotal = $("#summaryTotal");

    const $tableMapModal = $("#tableMapModal");
    const $hiddenInput = $("#selectedBanPhongId");
    const $displayArea = $("#selectedTableDisplay");

    const $addMonAnModal = $("#addMonAnModal");
    const $openAddMonAnModal = $("#openAddMonAnModal");

    // Lấy ID từ biến server hoặc URL
    const datBanId = (typeof serverDatBanId !== 'undefined') ? serverDatBanId : parseInt(window.location.pathname.split('/').pop());

    let currentItems = [];
    if (typeof originalItemsData !== 'undefined') {
        currentItems = JSON.parse(JSON.stringify(originalItemsData));
    }

    // --- CÁC HÀM RENDER VÀ SỰ KIỆN (GIỮ NGUYÊN) ---
    function renderItems() {
        $summaryBody.empty();
        let total = 0;
        const isViewing = $("#infoFieldset").is(":disabled");
        const displayStyle = isViewing ? 'display:none;' : '';

        if (isViewing) $(".order-details-table th:last-child").hide();
        else $(".order-details-table th:last-child").show();

        if (currentItems.length === 0) {
            const colspan = isViewing ? 4 : 5;
            $summaryBody.html(`<tr><td colspan="${colspan}" class="empty-cart">Chưa có món ăn nào.</td></tr>`);
            $summaryTotal.text(vnd(0));
            return;
        }

        currentItems.forEach((item) => {
            const donGia = item.donGia || item.Gia || 0;
            const thanhTien = donGia * item.soLuong;
            total += thanhTien;

            $summaryBody.append(`
                <tr data-id="${item.monAnId}">
                    <td>${item.tenMon || 'Món không xác định'}</td>
                    <td class="align-right">${vnd(donGia)}</td>
                    <td style="text-align:center;">
                        <div class="qty-controls">
                            <button class="qty-btn minus" data-id="${item.monAnId}" disabled>−</button>
                            <span class="qty">${item.soLuong}</span>
                            <button class="qty-btn plus" data-id="${item.monAnId}" disabled>+</button>
                        </div>
                    </td>
                    <td class="align-right">${vnd(thanhTien)}</td>
                    <td style="text-align:center; ${displayStyle}">
                        <button class="remove-btn" data-id="${item.monAnId}" disabled>&times;</button>
                    </td>
                </tr>
            `);
        });
        $summaryTotal.text(vnd(total));
    }

    function setEditMode(isEditing) {
        $fieldset.prop("disabled", !isEditing);
        $summaryBody.find(".qty-btn, .remove-btn").prop("disabled", !isEditing);
        if (isEditing) {
            $(".order-details-table th:last-child, .order-details-table td:last-child").show();
            $openAddMonAnModal.show();
            $btnEdit.hide(); $btnSave.show(); $btnCancel.show();
            $errorMsg.hide(); $successMsg.hide();
        } else {
            $(".order-details-table th:last-child, .order-details-table td:last-child").hide();
            $openAddMonAnModal.hide();
            $btnEdit.show(); $btnSave.hide(); $btnCancel.hide();
        }
    }

    // Gắn sự kiện cơ bản
    $btnEdit.on("click", () => setEditMode(true));
    $btnCancel.on("click", () => {
        $form[0].reset();
        if (typeof originalItemsData !== 'undefined') currentItems = JSON.parse(JSON.stringify(originalItemsData));
        renderItems();
        setEditMode(false);
    });

    $summaryBody.on("click", ".qty-btn", function (e) {
        e.preventDefault();
        const id = $(this).data("id");
        const item = currentItems.find(i => i.monAnId === id);
        if (item) {
            if ($(this).hasClass("plus")) item.soLuong++;
            else if ($(this).hasClass("minus")) item.soLuong--;
            if (item.soLuong <= 0) item.soLuong = 1;
            renderItems();
        }
    });

    $summaryBody.on("click", ".remove-btn", function (e) {
        e.preventDefault();
        if (confirm("Xóa món này?")) {
            currentItems = currentItems.filter(i => i.monAnId !== $(this).data("id"));
            renderItems();
        }
    });

    // Modal Bàn
    $("#openTableMapBtn").on("click", function (e) {
        e.preventDefault();
        if (!$fieldset.is(':disabled')) $tableMapModal.addClass("active");
    });
    $("#closeTableMapModal").click(() => $tableMapModal.removeClass("active"));
    $("#tableMapModal").on("click", ".table-card", function () {
        if ($(this).data("available") !== true) { alert("Bàn bận!"); return; }
        $(".table-card.selected").removeClass("selected");
        $(this).addClass("selected");
        $hiddenInput.val($(this).data("id"));
        $displayArea.text(`Đã chọn: ${$(this).data("name")}`);
        setTimeout(() => $tableMapModal.removeClass("active"), 200);
    });

    // Modal Món
    $openAddMonAnModal.click(() => { $addMonAnModal.addClass("active"); $(".category-btn[data-category-id='all']").click(); });
    $("#closeAddMonAnModal").click(() => $addMonAnModal.removeClass("active"));
    $(".menu-modal-sidebar").on("click", ".category-btn", function () {
        $(".category-btn.active").removeClass("active"); $(this).addClass("active");
        const cid = $(this).data("category-id");
        if (cid === "all") $("#addMonAnList .monan-item").show();
        else { $("#addMonAnList .monan-item").hide(); $(`#addMonAnList .monan-item[data-monan-category-id="${cid}"]`).show(); }
    });
    $("#addMonAnList").on("click", ".btn-add-mon", function () {
        const id = $(this).data("id");
        const exist = currentItems.find(i => i.monAnId === id);
        if (exist) exist.soLuong++;
        else currentItems.push({ monAnId: id, tenMon: $(this).data("name"), soLuong: 1, donGia: parseFloat($(this).data("price")) });
        renderItems(); $addMonAnModal.removeClass("active");
    });

    // --- [QUAN TRỌNG] SỬA LỖI NÚT LƯU & BẮT LỖI 400 ---
    // --- ĐOẠN CODE SỬA LỖI 400 (ÉP KIỂU DỮ LIỆU) ---
    $btnSave.on("click", function (e) {
        e.preventDefault();

        // 1. Lấy thông tin User
        let username = "Guest";
        if (typeof getAuthState === 'function') {
            const authRaw = getAuthState();
            if (authRaw) {
                const auth = JSON.parse(authRaw);
                username = auth.username || "Guest";
            }
        }

        // 2. Lấy và ÉP KIỂU dữ liệu (Quan trọng nhất)
        // ParseInt để đảm bảo là số nguyên, nếu lỗi thì về 0 hoặc 1
        let valGuestCount = parseInt($("#guestCount").val());
        if (isNaN(valGuestCount) || valGuestCount < 1) valGuestCount = 1;

        let valBanPhongId = parseInt($("#selectedBanPhongId").val());
        if (isNaN(valBanPhongId)) valBanPhongId = null; // Bàn có thể null

        // 3. Tạo Payload chuẩn (Khớp với ViewModel C#)
        const payload = {
            Username: String(username),         // Đảm bảo là chuỗi
            DatBanId: parseInt(datBanId) || 0,  // Đảm bảo là số
            BookingDate: String($("#bookingDate").val()), // YYYY-MM-DD
            TimeSlot: String($("#timeSlot").val()),
            GuestCount: valGuestCount,          // Đã xử lý ở trên
            BanPhongId: valBanPhongId,          // Đã xử lý ở trên
            Items: currentItems.map(i => ({
                MonAnId: parseInt(i.monAnId) || 0,
                SoLuong: parseInt(i.soLuong) || 1,
                // Ép kiểu giá tiền cẩn thận
                DonGia: parseFloat(i.donGia || i.Gia || 0)
            }))
        };

        // [DEBUG] In ra console để bạn soi xem có field nào bị NaN không
        console.log("Dữ liệu chuẩn bị gửi đi:", payload);

        // 4. Gửi Ajax
        $.ajax({
            url: "/Account/UpdateBooking",
            type: "POST",
            contentType: "application/json",
            data: JSON.stringify(payload),
            beforeSend: function () {
                $btnSave.prop("disabled", true).text("Đang lưu...");
            },
            success: function (res) {
                if (res.success) {
                    showSuccess(res.message || "Thành công!");
                    setEditMode(false);
                    if (typeof originalItemsData !== 'undefined') {
                        originalItemsData = JSON.parse(JSON.stringify(currentItems));
                    }
                } else {
                    showError(res.message);
                }
            },
            error: function (xhr) {
                console.error("Lỗi 400 Chi tiết:", xhr.responseText);

                let errorMsg = "Lỗi dữ liệu không hợp lệ (400).";

                // Cố gắng moi móc lý do lỗi từ Server
                if (xhr.responseJSON) {
                    if (xhr.responseJSON.message) {
                        errorMsg = xhr.responseJSON.message;
                    } else if (xhr.responseJSON.errors) {
                        // Lỗi validation mặc định của .NET (Ví dụ: GuestCount required)
                        const errors = xhr.responseJSON.errors;
                        const firstField = Object.keys(errors)[0];
                        errorMsg = `Lỗi tại trường [${firstField}]: ${errors[firstField][0]}`;
                    }
                }

                showError(errorMsg);
                alert(errorMsg); // Hiện popup cảnh báo
            },
            complete: function () {
                $btnSave.prop("disabled", false).text("Lưu thay đổi");
            }
        });
    });

    function showError(msg) { $errorMsg.text(msg).slideDown(); $successMsg.slideUp(); }
    function showSuccess(msg) {
        $successMsg.text(msg).slideDown();
        $errorMsg.slideUp();

        $('html, body').animate({
            scrollTop: $successMsg.offset().top - 100
        }, 500); }

    renderItems();
});