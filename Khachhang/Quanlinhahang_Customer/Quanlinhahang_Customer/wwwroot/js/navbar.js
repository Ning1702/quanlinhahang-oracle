/* ================================
   NAVBAR.JS - SỬA LỖI PHẠM VI (SCOPE)
   ================================ */

// === CÁC HÀM NÀY PHẢI ĐẶT BÊN NGOÀI $(document).ready ===
function getAuthState() {
    return sessionStorage.getItem("authUser") ||
        localStorage.getItem("authUser") ||
        null;
}

function saveAuthState(user, remember) {
    const data = JSON.stringify({
        username: user.username,
        fullName: user.fullName || user.username,
        ts: Date.now(),
    });
    if (remember) {
        localStorage.setItem("authUser", data);
        sessionStorage.removeItem("authUser");
    } else {
        sessionStorage.setItem("authUser", data);
        localStorage.removeItem("authUser");
    }
}

function clearAuthState() {
    localStorage.removeItem("authUser");
    sessionStorage.removeItem("authUser");
}

/* Áp giao diện theo trạng thái */
function applyAuthUI() {
    const authRaw = getAuthState();
    const $loginBtn = $("#loginBtn");
    const $profileContainer = $("#profileDropdownContainer");
    const $userGreetingName = $("#userGreetingName");

    if (authRaw) {
        const auth = JSON.parse(authRaw);
        if ($userGreetingName.length) {
            $userGreetingName.text(auth.fullName);
        }
        $profileContainer.show();
        $loginBtn.hide();
    } else {
        $profileContainer.hide();
        $loginBtn.show();
    }
}

// === PHẦN LOGIC KHỞI CHẠY CỦA NAVBAR ===
$(document).ready(function () {

    // 1. KHAI BÁO BIẾN (chỉ các biến dùng trong file này)
    const $loginModal = $("#loginModal");
    const $loginForm = $("#loginForm");
    const $usernameInput = $("#username");
    const $passwordInput = $("#password");
    const $rememberMe = $("#rememberMe");
    const $errorMessage = $("#loginError");
    const $loginSuccess = $("#loginSuccess");
    const $registerModal = $("#registerModal");
    const $profileToggleBtn = $("#profileToggleBtn");
    const $profileDropdownMenu = $("#profileDropdownMenu");
    const $navbar = $(".navbar");

    // 2. HÀM TRỢ GIÚP (Helpers)
    function showError(msg) {
        $errorMessage.text(msg).slideDown();
        $loginSuccess.slideUp();
    }
    function showSuccess(msg) {
        $loginSuccess.text(msg).slideDown();
        $errorMessage.slideUp();
    }
    function handleLogout() {
        if (confirm("Bạn có chắc muốn đăng xuất?")) {
            clearAuthState();
            applyAuthUI();
            window.location.href = "/Home/GioiThieu";
        }
    }
    function setupMobileNav() {
        const $navMenu = $(".nav-menu");
        const $navToggle = $("#navToggle");
        if (!$navMenu.length || !$navToggle.length) return;

        $navToggle.on("click", function (e) {
            e.stopPropagation();
            if ($navMenu.hasClass("open")) {
                $navMenu.removeClass("open");
                $navToggle.text("☰");
                $("body").css("overflow", "");
            } else {
                $navMenu.addClass("open");
                $navToggle.text("✖");
                $("body").css("overflow", "hidden");
            }
        });
        $navMenu.on("click", "a", function () {
            $navMenu.removeClass("open");
            $navToggle.text("☰");
            $("body").css("overflow", "");
        });
        $(window).on("resize", function () {
            if ($(window).width() > 815 && $navMenu.hasClass("open")) {
                $navMenu.removeClass("open");
                $navToggle.text("☰");
                $("body").css("overflow", "");
            }
        });
    }

    // 3. KHỞI CHẠY (Initialization)
    const savedUsername = localStorage.getItem("rememberedUsername");
    if (savedUsername) {
        $usernameInput.val(savedUsername);
        $rememberMe.prop("checked", true);
    }
    setupMobileNav();
    applyAuthUI(); // Chạy ngay khi tải trang

    // 4. GẮN SỰ KIỆN (Event Handlers)
    $("#loginBtn").on("click", function () {
        $loginModal.addClass("active");
        $errorMessage.hide().empty();
        $loginSuccess.hide().empty();
    });

    $("#closeModal, #loginModal").on("click", function (e) {
        if ($(e.target).is("#closeModal") || $(e.target).is("#loginModal")) {
            $loginModal.removeClass("active");
        }
    });

    $("#togglePassword").on("click", function () {
        if ($passwordInput.attr("type") === "password") {
            $passwordInput.attr("type", "text");
            $(this).text("🙈");
        } else {
            $passwordInput.attr("type", "password");
            $(this).text("👁️");
        }
    });

    $loginForm.on("submit", function (e) {
        e.preventDefault();
        const formData = {
            Username: $usernameInput.val().trim(),
            Password: $passwordInput.val(),
            RememberMe: $rememberMe.is(":checked")
        };
        if (formData.Username === "" || formData.Password === "") {
            return showError("Vui lòng nhập tài khoản và mật khẩu.");
        }
        $.ajax({
            url: "/Account/Login",
            type: "POST",
            contentType: "application/json",
            data: JSON.stringify(formData),
            beforeSend: function () {
                $loginForm.find("button[type='submit']").prop("disabled", true).text("Đang đăng nhập...");
                $errorMessage.slideUp(); $loginSuccess.slideUp();
            },
            success: function (response) {
                if (response.success && response.user) {
                    showSuccess("Đăng nhập thành công! Chào mừng " + response.user.fullName + " 👋");
                    if (formData.RememberMe) {
                        localStorage.setItem("rememberedUsername", formData.Username);
                    } else {
                        localStorage.removeItem("rememberedUsername");
                    }
                    saveAuthState(response.user, formData.RememberMe);
                    setTimeout(() => {
                        applyAuthUI();
                        $loginModal.removeClass("active");
                        $loginForm.trigger("reset");
                        $loginSuccess.hide();
                    }, 1500);
                } else {
                    showError(response.message || "Đăng nhập thất bại.");
                    $loginForm.find("button[type='submit']").prop("disabled", false).text("Đăng nhập");
                }
            },
            error: function (xhr) {
                let errorMsg = "Lỗi kết nối Server.";
                if (xhr.responseJSON && xhr.responseJSON.message) { errorMsg = xhr.responseJSON.message; }
                showError(errorMsg);
                $loginForm.find("button[type='submit']").prop("disabled", false).text("Đăng nhập");
            }
        });
    });

    $(document).on("click", "#logoutLink", function (e) {
        e.preventDefault();
        $profileDropdownMenu.slideUp(200);
        handleLogout();
    });

    /* === SỬA LỖI MENU TỰ ĐÓNG === */
    $profileToggleBtn.on("click", function (e) {
        e.stopPropagation(); // Ngăn sự kiện click lan ra document
        $profileDropdownMenu.slideToggle(200);
    });

    $(document).on("click", function (e) {
        // Đóng menu profile VÀ đóng menu mobile nếu click ra ngoài
        if (!$profileDropdownMenu.is(":hidden") && !$(e.target).closest('#profileDropdownContainer').length) {
            $profileDropdownMenu.slideUp(200);
        }
    });
    /* === KẾT THÚC SỬA LỖI MENU === */

    $("#registerLink").on("click", function (e) {
        e.preventDefault();
        $loginModal.removeClass("active");
        $registerModal.addClass("active");
    });

    if ($navbar.length) {
        $(window).on("scroll", function () {
            if ($(window).scrollTop() > 50) {
                $navbar.addClass("scrolled");
            } else {
                $navbar.removeClass("scrolled");
            }
        });
    }

    $('a[href="#lien-he"]').on("click", function (e) {
        e.preventDefault();
        const target = $(this).attr("href");
        const $targetSection = $(target);
        if ($targetSection.length) {
            $("html, body").animate({ scrollTop: $targetSection.offset().top }, 500);
        } else {
            $("html, body").animate({ scrollTop: $(document).height() }, 500);
        }
    });

    // (Code cho Reset Password Modal)
    const $forgotPasswordLink = $("#forgotPasswordLink");
    const $resetPasswordModal = $("#resetPasswordModal");
    const $closeResetModal = $("#closeResetModal");
    const $resetStep1Form = $("#resetStep1Form");
    const $resetStep2Form = $("#resetStep2Form");
    const $resetUsernameInput = $("#resetUsername");
    const $resetError = $("#resetError");
    const $resetSuccess = $("#resetSuccess");
    const $backToLoginFromReset = $("#backToLoginFromReset");
    const $toggleNewPassword = $("#toggleNewPassword");
    let currentResetUsername = '';

    function showResetError(msg) { $resetError.text(msg).slideDown(); $resetSuccess.slideUp(); }
    function showResetSuccess(msg) { $resetSuccess.text(msg).slideDown(); $resetError.slideUp(); }

    $forgotPasswordLink.on("click", function (e) { e.preventDefault(); $loginModal.removeClass("active"); $resetPasswordModal.addClass("active"); $resetStep1Form.show(); $resetStep2Form.hide(); $("#resetHeader").text("Khôi phục mật khẩu"); $("#resetSubheader").text("Bước 1: Nhập Tên đăng nhập của bạn"); $resetError.empty().hide(); $resetSuccess.empty().hide(); });
    $backToLoginFromReset.on("click", function (e) { e.preventDefault(); $resetPasswordModal.removeClass("active"); $loginModal.addClass("active"); });
    $closeResetModal.on("click", function () { $resetPasswordModal.removeClass("active"); });
    $resetPasswordModal.on("click", function (e) { if ($(e.target).is($resetPasswordModal)) { $resetPasswordModal.removeClass("active"); } });
    $toggleNewPassword.on("click", function () { const $input = $("#newPassword"); if ($input.attr("type") === "password") { $input.attr("type", "text"); $(this).text("🙈"); } else { $input.attr("type", "password"); $(this).text("👁"); } });
    $resetStep1Form.on("submit", function (e) { e.preventDefault(); currentResetUsername = $resetUsernameInput.val().trim(); if (currentResetUsername.length < 4) return showResetError("Tên đăng nhập không hợp lệ."); $.ajax({ url: "/Account/CheckUsername", type: "POST", contentType: "application/json", data: JSON.stringify({ Username: currentResetUsername }), beforeSend: function () { $("#continueBtn").prop("disabled", true).text("Đang kiểm tra..."); }, success: function (res) { if (res.success) { $resetStep1Form.slideUp(200, function () { $("#resetHeader").text("Đặt lại mật khẩu"); $("#resetSubheader").text(`Bước 2: Nhập mật khẩu mới cho ${currentResetUsername}`); $resetStep2Form.slideDown(200); $resetSuccess.empty().hide(); $resetError.empty().hide(); }); } else { showResetError(res.message || "Tên đăng nhập không tồn tại."); } }, error: function (xhr) { showResetError(xhr.responseJSON?.message || "Lỗi kết nối server."); }, complete: function () { $("#continueBtn").prop("disabled", false).text("Tiếp tục"); } }); });
    $resetStep2Form.on("submit", function (e) { e.preventDefault(); const newPassword = $("#newPassword").val(); const confirmPassword = $("#confirmNewPassword").val(); if (newPassword.length < 6) return showResetError("Mật khẩu phải có ít nhất 6 ký tự."); if (newPassword !== confirmPassword) return showResetError("Mật khẩu xác nhận không khớp."); $.ajax({ url: "/Account/ResetPassword", type: "POST", contentType: "application/json", data: JSON.stringify({ Username: currentResetUsername, NewPassword: newPassword }), beforeSend: function () { $resetStep2Form.find("button[type='submit']").prop("disabled", true).text("Đang đổi..."); }, success: function (res) { if (res.success) { showResetSuccess(res.message || "Mật khẩu đã được thay đổi thành công!"); setTimeout(() => { $resetPasswordModal.removeClass("active"); $loginModal.addClass("active"); $("#username").val(currentResetUsername); $resetStep2Form.trigger("reset"); }, 2000); } else { showResetError(res.message || "Lỗi khi đổi mật khẩu."); } }, error: function (xhr) { showResetError(xhr.responseJSON?.message || "Lỗi kết nối server."); }, complete: function () { $resetStep2Form.find("button[type='submit']").prop("disabled", false).text("Đổi mật khẩu"); } }); });
});