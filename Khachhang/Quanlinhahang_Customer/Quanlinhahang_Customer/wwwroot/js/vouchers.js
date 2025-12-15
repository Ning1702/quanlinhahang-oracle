// vouchers.js
const vnd = (n) => (n || 0).toLocaleString("vi-VN", { style: "currency", currency: "VND" });

$(document).ready(function () {
    const $voucherList = $("#voucher-list");
    const $memberRank = $("#memberRank");
    const $memberPoints = $("#memberPoints");

    // Lấy thông tin người dùng (từ navbar.js)
    const authRaw = getAuthState();
    if (!authRaw) {
        $voucherList.html('<p class="no-results">Vui lòng đăng nhập để xem ưu đãi.</p>');
        return;
    }
    const auth = JSON.parse(authRaw);

    // --- Hàm tải dữ liệu Voucher ---
    function fetchVouchers() {
        $voucherList.html('<div class="loading-spinner">Đang tải Voucher...</div>');

        $.ajax({
            url: "/Account/GetUserVouchers",
            type: "GET",
            data: { username: auth.username },
            success: function (res) {
                if (!res.success) {
                    $voucherList.html(`<p class="no-results">Lỗi: ${res.message}</p>`);
                    return;
                }

                $memberRank.text(res.hangThanhVien);
                $memberPoints.text(res.diemTichLuy.toLocaleString('vi-VN'));

                const vouchers = res.list || [];
                $voucherList.empty();

                if (vouchers.length === 0) {
                    $voucherList.html('<p class="no-results">Hiện không có mã giảm giá nào cho bạn.</p>');
                    return;
                }

                // Render danh sách Voucher
                vouchers.forEach(item => {
                    const valueDisplay = item.type === "Phần trăm" ? `${item.value}%` : item.description;

                    const itemHtml = `
                        <div class="history-item voucher-card">
                            <div class="history-item-info">
                                <h4>${valueDisplay} giảm giá</h4>
                                <p>${item.description}</p>
                                <p style="color: #999;">Đơn tối thiểu: ${item.minOrder > 0 ? vnd(item.minOrder) : 'Không yêu cầu'}</p>
                                <p style="color: #aaa; font-size: 0.85rem;">Hạn sử dụng: ${item.expiry}</p>
                            </div>
                            <div class="voucher-code-section">
                                <div class="voucher-code">${item.code}</div>
                                <button class="order-btn" style="margin-top: 10px; padding: 5px 15px;">Sao chép</button>
                            </div>
                        </div>
                    `;
                    $voucherList.append(itemHtml);
                });

            },
            error: function (xhr) {
                $voucherList.html(`<p class="no-results">Lỗi tải dữ liệu: ${xhr.responseJSON?.message || 'Lỗi kết nối server'}</p>`);
            }
        });
    }

    // Tải dữ liệu khi trang được mở
    fetchVouchers();

    // Gắn sự kiện sao chép (delegation)
    $(document).on("click", ".voucher-code-section button", function () {
        const code = $(this).siblings('.voucher-code').text();
        navigator.clipboard.writeText(code).then(() => {
            alert(`Đã sao chép mã ${code}`);
        });
    });
});