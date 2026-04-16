// datban-jquery.js

const LS_CART_KEY = "cart";
const vnd = (n) => (n || 0).toLocaleString("vi-VN", { style: "currency", currency: "VND" });

// --- HELPER ---
function safeParse(raw) {
    try { return JSON.parse(raw); } catch { return null; }
}
function getAuthState() {
    return localStorage.getItem("authUser") || sessionStorage.getItem("authUser") || null;
}

// --- CART (Xử lý Giỏ hàng) ---
function getCartArray() {
    const raw = localStorage.getItem(LS_CART_KEY);
    if (!raw) return [];
    const parsed = safeParse(raw);
    if (!parsed) return [];
    // Hỗ trợ cả 2 định dạng lưu trữ cũ (Mảng) và mới (Object Map)
    if (Array.isArray(parsed)) return parsed;
    return Object.values(parsed);
}

function saveCartArray(arr) {
    const map = {};
    (arr || []).forEach(it => {
        if (!it || !it.id) return;
        // Đảm bảo dữ liệu số chuẩn xác
        map[it.id] = {
            id: it.id,
            name: it.name,
            price: Number(it.price || 0),
            qty: Number(it.qty || 1)
        };
    });
    try { localStorage.setItem(LS_CART_KEY, JSON.stringify(map)); } catch (e) { console.warn(e); }
}

// --- RENDER SUMMARY (Hiển thị Giỏ hàng bên phải) ---
function renderSummary() {
    const data = getCartArray();
    const $body = $("#summaryBody");
    const $total = $("#summaryTotal");

    if (!data || data.length === 0) {
        $body.html('<div class="empty-cart">Giỏ hàng đang trống. Vui lòng chọn món ở trang <a href="/Home/Menu">Thực đơn</a>.</div>');
        $total.text("0₫");
        return;
    }

    let html = "";
    let total = 0;
    data.forEach((item, idx) => {
        const thanhTien = Number(item.price || 0) * Number(item.qty || 0);
        total += thanhTien;
        html += `
            <div class="summary-row" data-id="${item.id}">
                <div class="summary-col">${idx + 1}</div>
                <div class="summary-col">${item.name}</div>
                <div class="summary-col">${vnd(item.price)}</div>
                <div class="summary-col qty-controls">
                    <button class="qty-btn minus" data-id="${item.id}">−</button>
                    <span class="qty">${item.qty}</span>
                    <button class="qty-btn plus" data-id="${item.id}">+</button>
                </div>
                <div class="summary-col">${vnd(thanhTien)}</div>
                <div class="summary-col">
                    <button class="remove-btn" data-id="${item.id}">&times;</button>
                </div>
            </div>`;
    });
    $body.html(html);
    $total.text(vnd(total));
}

// --- CHECK LOGIN (Hiển thị tên người dùng) ---
function renderUserGreeting() {
    const authRaw = getAuthState();
    const $box = $("#userGreetingBox");
    const $name = $("#loggedInName");
    if (authRaw) {
        const auth = safeParse(authRaw);
        if (auth && auth.fullName) {
            $name.text(auth.fullName);
            $box.show();
            return auth.username; // Trả về username để dùng khi submit
        }
    }
    $box.hide();
    return null;
}

$(document).ready(function () {
    // 1. Khởi tạo giao diện
    renderSummary();
    const username = renderUserGreeting();

    // Nếu chưa đăng nhập -> Ẩn form, hiện thông báo
    if (!username) {
        $("#bookingForm").hide();
        $("#userGreetingBox").html('<p class="alert-error">Bạn cần đăng nhập để đặt bàn.</p>').show();
        return;
    }

    // 2. XỬ LÝ MODAL CHỌN BÀN (GỌI API)
    const $tableMapModal = $("#tableMapModal");
    const $hiddenInput = $("#selectedBanPhongId");
    const $openTableMapBtn = $("#openTableMapBtn");
    const $tableMapGrid = $(".table-map-grid");

    $openTableMapBtn.on("click", function (e) {
        e.preventDefault();

        const dateVal = $("#bookingDate").val();
        const timeVal = $("#timeSlot").val();

        if (!dateVal || !timeVal) {
            alert("Vui lòng chọn 'Ngày đặt' và 'Khung giờ' trước khi chọn bàn.");
            return;
        }

        // Gọi API lấy trạng thái bàn
        $.ajax({
            url: "/DatBan/GetTableStatus",
            type: "GET",
            data: { date: dateVal, timeSlot: timeVal },
            beforeSend: function () {
                $openTableMapBtn.text("Đang tải sơ đồ...");
                $openTableMapBtn.prop("disabled", true);
            },
            success: function (res) {
                if (res.success) {
                    renderTableMap(res.data);

                    // Highlight bàn đang chọn (nếu có)
                    const currentId = $hiddenInput.val();
                    if (currentId) $(`.table-card[data-id='${currentId}']`).addClass("selected");

                    $tableMapModal.addClass("active");
                }
            },
            error: function (xhr) {
                alert(xhr.responseJSON?.message || "Lỗi tải sơ đồ bàn.");
            },
            complete: function () {
                const currentId = $hiddenInput.val();
                const currentText = currentId ? `Đã chọn: Bàn số ${currentId}` : "Chọn bàn từ sơ đồ";
                $openTableMapBtn.text(currentText);
                $openTableMapBtn.prop("disabled", false);
            }
        });
    });

    function renderTableMap(tables) {
        $tableMapGrid.empty();
        if (!tables || tables.length === 0) {
            $tableMapGrid.html('<p style="color:#fff; text-align:center; grid-column:1/-1;">Không có bàn nào.</p>');
            return;
        }

        let html = "";
        tables.forEach(ban => {
            const isAvailable = (ban.status === 0);
            const statusClass = isAvailable ? "trống" : "da-dat";
            const dataAvailable = isAvailable ? "true" : "false";

            html += `
                <div class="table-card ${statusClass}" 
                     data-id="${ban.id}" 
                     data-name="${ban.name}"
                     data-available="${dataAvailable}"
                     title="${ban.name} - ${ban.typeName} (${ban.capacity} ghế)">
                    <div class="table-card-icon">
                        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
                            <path d="M4 18v3h3v-3h10v3h3v-6H4v3zm16-8h-2V7c0-1.1-.9-2-2-2H8c-1.1 0-2 .9-2 2v3H4v6h16v-6z"/>
                        </svg>
                    </div>
                    <div class="table-card-name">${ban.name}</div>
                    <div class="table-card-capacity">(${ban.capacity} người)</div>
                </div>`;
        });
        $tableMapGrid.html(html);
    }

    $("#closeTableMapModal").on("click", function () { $tableMapModal.removeClass("active"); });

    // Bắt sự kiện click vào từng bàn
    $tableMapModal.on("click", ".table-card", function (e) {
        e.preventDefault();
        const $card = $(this);

        if (String($card.data("available")).toLowerCase() !== "true") {
            alert("Bàn này đang bận hoặc đã đặt.");
            return;
        }

        if ($card.hasClass("selected")) {
            // Bỏ chọn
            $card.removeClass("selected");
            $hiddenInput.val("");
            $openTableMapBtn.text("Chọn bàn từ sơ đồ");
            $openTableMapBtn.removeClass("selected");
        } else {
            // Chọn mới
            $(".table-card.selected").removeClass("selected");
            $card.addClass("selected");

            $hiddenInput.val($card.data("id"));
            $openTableMapBtn.text(`Đã chọn: ${$card.data("name")}`);
            $openTableMapBtn.addClass("selected");

            // Đóng modal sau khi chọn
            setTimeout(() => { $tableMapModal.removeClass("active"); }, 200);
        }
    });

    // 3. CART EVENTS (Tăng/Giảm/Xóa món)
    $("#summaryBody").on("click", ".qty-btn", function (e) {
        const $btn = $(this);
        const id = $btn.data("id");
        let cart = getCartArray();
        const item = cart.find(i => String(i.id) === String(id));
        if (!item) return;

        if ($btn.hasClass("plus")) item.qty++;
        else if ($btn.hasClass("minus")) item.qty--;

        if (item.qty <= 0) cart = cart.filter(i => String(i.id) !== String(id));
        saveCartArray(cart); renderSummary();
    });

    $("#summaryBody").on("click", ".remove-btn", function (e) {
        const id = $(this).data("id");
        if (confirm("Xóa món này?")) {
            let cart = getCartArray();
            cart = cart.filter(i => String(i.id) !== String(id));
            saveCartArray(cart); renderSummary();
        }
    });

    // 4. SUBMIT FORM (Gửi Đặt Bàn)
    $("#bookingForm").on("submit", function (e) {
        e.preventDefault();

        // Validate cơ bản
        let cart = getCartArray();
        if (!cart || cart.length === 0) { alert("Giỏ hàng trống!"); return; }

        const authRaw = getAuthState();
        if (!authRaw) { alert("Vui lòng đăng nhập."); return; }
        const auth = JSON.parse(authRaw);

        const bookingDate = $("#bookingDate").val();
        const timeSlot = $("#timeSlot").val();
        const guestCount = parseInt($("#guestCount").val() || "1", 10);
        const banPhongId = parseInt($("#selectedBanPhongId").val()) || null;

        if (!bookingDate || !timeSlot) { alert("Vui lòng chọn ngày và giờ."); return; }

        // Tạo Payload chuẩn JSON
        const payload = {
            username: auth.username,
            bookingDate: bookingDate,
            timeSlot: timeSlot,
            guestCount: guestCount,
            BanPhongId: banPhongId,
            note: $("#note").val(),
            items: cart.map(it => ({
                id: it.id,
                name: it.name,
                price: Number(it.price),
                qty: Number(it.qty)
            }))
        };

        const $submitBtn = $("#bookingForm .btn-submit");
        $submitBtn.prop("disabled", true).text("Đang xử lý...");

        // Gửi AJAX POST (Đã sửa lỗi 404/ContentType)
        $.ajax({
            url: "/DatBan/Submit",
            type: "POST", // BẮT BUỘC: POST
            contentType: "application/json", // BẮT BUỘC: JSON
            data: JSON.stringify(payload), // BẮT BUỘC: Stringify
            success: function (res) {
                if (res.success) {
                    // Thành công: Xóa giỏ hàng & Hiện modal thông báo
                    localStorage.removeItem(LS_CART_KEY);
                    renderSummary();
                    $("#bookingModal").addClass("active");
                } else {
                    alert("Lỗi: " + res.message);
                    $submitBtn.prop("disabled", false).text("Xác nhận đặt bàn");
                }
            },
            error: function (xhr) {
                // Xử lý lỗi server trả về
                const msg = xhr.responseJSON?.message || "Lỗi kết nối đến máy chủ.";
                alert("Thất bại: " + msg);
                $submitBtn.prop("disabled", false).text("Xác nhận đặt bàn");
            }
        });
    });

    $("#closeModalBtn").on("click", function () {
        $("#bookingModal").removeClass("active");
        window.location.href = "/Home/Menu";
    });
});