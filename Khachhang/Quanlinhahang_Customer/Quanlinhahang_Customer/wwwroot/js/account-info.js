$(document).ready(function () {
    const $fieldset = $("#infoFieldset");
    const $form = $("#infoForm");
    const $btnEdit = $("#btnEdit");
    const $btnSave = $("#btnSave");
    const $btnCancel = $("#btnCancel");
    const $errorMsg = $("#infoError");
    const $successMsg = $("#infoSuccess");

    let originalData = {};

    function loadUserData() {
        console.log("Hàm loadUserData() đã được gọi.");

        const authRaw = getAuthState();
        if (!authRaw) {
            showError("Vui lòng đăng nhập để xem thông tin.");
            return;
        }

        const auth = JSON.parse(authRaw);

        $("#regUsername").val(auth.username);

        $.ajax({
            url: "/Account/GetUserInfo",
            type: "GET",
            data: { username: auth.username },
            success: function (data) {
                console.log("AJAX Success (Đã nhận được dữ liệu):", data);
                originalData = data;
                populateForm(data); 
            },
            error: function (xhr) {
                console.error("AJAX Error (Lỗi server):", xhr.status, xhr.responseJSON);
                showError(xhr.responseJSON?.message || "Lỗi tải thông tin người dùng.");
            }
        });

        console.log("Đã gửi yêu cầu AJAX...");
    }

    function populateForm(data) {

        $("#regFullName").val(data.fullName);
        $("#regEmail").val(data.email);
        $("#regPhone").val(data.phone);
        $("#regAddress").val(data.address);
    }

    function setEditMode(isEditing) {
        $fieldset.prop("disabled", !isEditing);
        if (isEditing) {
            $btnEdit.hide();
            $btnSave.show();
            $btnCancel.show();
            $errorMsg.hide();
            $successMsg.hide();
        } else {
            $btnEdit.show();
            $btnSave.hide();
            $btnCancel.hide();
        }
    }

    $btnEdit.on("click", function () {
        setEditMode(true);
    });

    $btnCancel.on("click", function () {
        populateForm(originalData); 
        setEditMode(false);
    });

    $form.on("submit", function (e) {
        e.preventDefault();
        const auth = JSON.parse(getAuthState());
        const updatedData = {
            username: auth.username,
            fullName: $("#regFullName").val(),
            email: $("#regEmail").val(),
            phone: $("#regPhone").val(),
            address: $("#regAddress").val()
        };

        $.ajax({
            url: "/Account/UpdateUserInfo",
            type: "POST",
            contentType: "application/json",
            data: JSON.stringify(updatedData),
            beforeSend: function () {
                $btnSave.prop("disabled", true).text("Đang lưu...");
            },
            success: function (response) {
                if (response.success) {
                    showSuccess(response.message);

                    originalData = {
                        fullName: updatedData.fullName,
                        email: updatedData.email,
                        phone: updatedData.phone,
                        address: updatedData.address
                    };
                    auth.fullName = response.newFullName;

                    if (response.newUsername) {
                        auth.username = response.newUsername;
                    }
                    saveAuthState(auth, localStorage.getItem("authUser") != null);
                    applyAuthUI();

                    setEditMode(false);
                } else {
                    showError(response.message);
                }
            },
            error: function (xhr) {
                showError(xhr.responseJSON?.message || "Lỗi server.");
            },
            complete: function () {
                $btnSave.prop("disabled", false).text("Lưu thay đổi");
            }
        });
    });

    function showError(msg) {
        $errorMsg.text(msg).slideDown();
        $successMsg.slideUp();
    }
    function showSuccess(msg) {
        $successMsg.text(msg).slideDown();
        $errorMsg.slideUp();
    }

    loadUserData();
});