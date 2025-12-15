/* dangki-jquery.js - Chỉ dành cho trang Dangki.cshtml */

$(document).ready(function () {
    // Chỉ chọn các thành phần tồn tại trên trang này
    const $registerForm = $("#registerForm");
    const $registerError = $("#registerError");
    const $registerSuccess = $("#registerSuccess");

    // --- Hàm hiển thị thông báo ---
    function showRegisterError(msg) {
        $registerSuccess.slideUp();
        $registerError.text(msg).slideDown();
    }
    function showRegisterSuccess(msg) {
        $registerError.slideUp();
        $registerSuccess.text(msg).slideDown();
    }

    // =========================================
    // Xử lý submit Form Đăng ký (AJAX)
    // =========================================
    $registerForm.on("submit", function (e) {
        e.preventDefault(); // Ngăn form gửi theo cách truyền thống

        // 1. Thu thập dữ liệu từ Form
        const formData = {
            FullName: $("#regFullName").val().trim(),
            Email: $("#regEmail").val().trim(),
            Phone: $("#regPhone").val().trim(),
            Username: $("#regUsername").val().trim(),
            Password: $("#regPassword").val(),
            ConfirmPassword: $("#regConfirmPassword").val(),
            Address: $("#regAddress").val().trim()
        };

        // 2. Kiểm tra phía Client
        if (formData.Phone === "") return showRegisterError("Số điện thoại không được để trống!");
        if (formData.Username.length < 4) return showRegisterError("Tên đăng nhập phải có ít nhất 4 ký tự!");
        if (formData.Password.length < 6) return showRegisterError("Mật khẩu phải có ít nhất 6 ký tự!");
        if (formData.Password !== formData.ConfirmPassword) return showRegisterError("Mật khẩu xác nhận không khớp!");

        // 3. Gửi dữ liệu qua AJAX (POST) đến Controller
        $.ajax({
            url: "/Account/Register", // Đảm bảo URL này là đúng
            type: "POST",
            contentType: "application/json",
            data: JSON.stringify(formData),
            beforeSend: function () {
                $registerForm.find("button[type='submit']").prop("disabled", true).text("Đang xử lý...");
                $registerError.slideUp();
            },
            success: function (response) {
                if (response.success) {
                    showRegisterSuccess(response.message || "Đăng ký thành công! Đang chuyển hướng...");

                    // 4. THAY ĐỔI: Chuyển hướng về trang chủ (hoặc trang đăng nhập)
                    // thay vì cố gắng đóng một modal không tồn tại.
                    setTimeout(() => {
                        window.location.href = "/Home/GioiThieu"; // Chuyển về trang Giới thiệu
                    }, 2000);
                } else {
                    showRegisterError(response.message || "Lỗi đăng ký không xác định.");
                }
            },
            error: function (xhr) {
                let errorMsg = "Lỗi kết nối Server. Vui lòng thử lại sau.";
                if (xhr.responseJSON && xhr.responseJSON.message) {
                    errorMsg = xhr.responseJSON.message;
                }
                showRegisterError(errorMsg);
            },
            complete: function () {
                // Kích hoạt lại nút bấm nếu đăng ký thất bại
                if (!$registerSuccess.is(":visible")) {
                    $registerForm.find("button[type='submit']").prop("disabled", false).text("Đăng ký tài khoản");
                }
            }
        });
    });

    // --- Logic Toggle Password ---
    $("#togglePasswordReg").on("click", function () {
        const $passwordInput = $("#regPassword");
        if ($passwordInput.attr("type") === "password") {
            $passwordInput.attr("type", "text");
            $(this).text("🙈");
        } else {
            $passwordInput.attr("type", "password");
            $(this).text("👁");
        }
    });
});