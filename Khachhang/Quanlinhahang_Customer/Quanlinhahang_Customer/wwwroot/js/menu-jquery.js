$(function () {
    /* -------------------- HELPERS -------------------- */
    const LS_KEY = "cart"; 
    const $search = $('#search-box');
    const $filter = $('#filter-cat');

    function formatVND(n) {
        if (!isFinite(n)) return "0₫";
        return (Number(n) || 0).toLocaleString('vi-VN') + " ₫";
    }

    function safeParse(raw) {
        try {
            return JSON.parse(raw);
        } catch (e) {
            return null;
        }
    }

    function readCartObject() {
        const raw = localStorage.getItem(LS_KEY);
        const parsed = safeParse(raw);
        if (!parsed || typeof parsed !== 'object') return {};
        return parsed;
    }

    function writeCartObject(obj) {
        try {
            localStorage.setItem(LS_KEY, JSON.stringify(obj));
        } catch (e) {
            console.warn("Lưu cart lỗi:", e);
        }
    }

    function getCartArray() {
        const obj = readCartObject();
        return Object.values(obj);
    }

    /* -------------------- FILTERS & REVEAL -------------------- */
    function getCards() {
        return $('.menu-card');
    }

    function ensureCardAttributes($card) {
        if (!$card.attr('data-search')) {
            const txt = ($card.find('h3, h4').text() + ' ' + $card.find('.desc').text()).trim();
            $card.attr('data-search', txt);
        }
        if (!$card.attr('data-cat')) {
            const sect = $card.closest('section').attr('id') || '';
            if (sect.indexOf('combo') !== -1) $card.attr('data-cat', 'combo');
            else if (sect.indexOf('alacarte') !== -1) $card.attr('data-cat', 'alacarte');
            else $card.attr('data-cat', '');
        }
    }

    function applyFiltersClient() {
        const q = ($search.val() || '').trim().toLowerCase();
        const cat = (($filter.val() || '') + '').toLowerCase();
        let visibleCount = 0;

        getCards().each(function () {
            const $c = $(this);
            ensureCardAttributes($c);
            const hay = (($c.attr('data-search') || $c.text()) + '').toLowerCase();
            const itemCat = (($c.attr('data-cat') || '') + '').toLowerCase();
            const matchText = !q || hay.indexOf(q) !== -1;
            const matchCat = !cat || cat === '' || cat === 'all' || itemCat === cat || itemCat === (cat.toLowerCase());

            if (matchText && matchCat) {
                $c.show();
                visibleCount++;
            } else {
                $c.hide();
            }
        });

        if (visibleCount === 0) {
            if ($('#no-results').length === 0) {
                $('<p id="no-results" style="color:#b45c00;margin-top:10px">Không tìm thấy món phù hợp.</p>')
                    .insertAfter('#combo-section .menu-grid:first, #alacarte-section .menu-grid:first');
            }
        } else {
            $('#no-results').remove();
        }
    }

    $search.on('input', applyFiltersClient);
    $filter.on('change', function () {
        applyFiltersClient();
    });

    $(window).on('scroll load', function () {
        getCards().each(function () {
            const $c = $(this);
            const cardTop = $c.offset().top;
            const windowBottom = $(window).scrollTop() + $(window).height();
            if (cardTop < windowBottom - 100) $c.addClass('revealed');
        });
    });

    getCards().each(function () { ensureCardAttributes($(this)); });
    applyFiltersClient();

    /* -------------------- CART -------------------- */
    let cart = readCartObject();

    function updateCartDisplay() {
        const $items = $("#cartItems");
        $items.empty();
        let total = 0;
        let count = 0;
        let uniqueItemCount = 0;

        const values = Object.values(cart);
        if (values.length === 0) {
            $items.html('<div class="empty-cart" style="padding:12px;color:#666">Giỏ hàng trống.</div>');
        } else {
            uniqueItemCount = values.length;
            values.forEach(item => {
                const lineTotal = Number(item.qty || 0) * Number(item.price || 0);
                total += lineTotal;
                count += Number(item.qty || 0);

                $items.append(`
                    <div class="cart-item" data-id="${item.id}">
                        <div class="cart-info">
                            <div class="cart-name">${item.name}</div>
                            <div class="cart-price">${formatVND(item.price)}</div>
                        </div>
                        <div class="cart-controls">
                            <button class="qty-btn minus" data-id="${item.id}" aria-label="Giảm">−</button>
                            <span class="qty">${item.qty}</span>
                            <button class="qty-btn plus" data-id="${item.id}" aria-label="Tăng">+</button>
                            <span class="line-total">${formatVND(lineTotal)}</span>
                            <button class="remove-btn" data-id="${item.id}" aria-label="Xóa">&times;</button>
                        </div>
                    </div>
                `);
            });
        }

        $("#cartTotal").text(formatVND(total));

        $("#cart-count").text(uniqueItemCount); 
        $("#cartCount").text(uniqueItemCount); 
        writeCartObject(cart);
    }

    function addToCart(id, name, price, qty = 1) {
        id = String(id);
        if (!cart[id]) cart[id] = { id, name, price: Number(price) || 0, qty: 0 };
        cart[id].qty = (Number(cart[id].qty) || 0) + Number(qty || 1);
        updateCartDisplay();
    }

    $(document).on("click", ".order-btn", function (e) {
        e.preventDefault();
        const $btn = $(this);
        const id = $btn.data("id");
        const name = $btn.data("name") || $btn.closest('.menu-card').find('h3, h4').first().text().trim();
        const price = Number($btn.data("price")) || Number($btn.attr('data-price')) || 0;
        if (!id) {
            const gen = name ? name.trim().toLowerCase().replace(/\s+/g, '-') : 'item-' + Date.now();
            addToCart(gen, name, price, 1);
        } else {
            addToCart(id, name, price, 1);
        }
        const $t = $('#toast');
        if ($t.length) {
            $t.text('Đã thêm "' + (name || 'món') + '" vào giỏ');
            $t.addClass('show');
            setTimeout(() => $t.removeClass('show'), 1400);
        }
    });

    $(document).on("click", ".qty-btn", function (e) {
        e.stopPropagation();
        const id = String($(this).data("id"));
        if (!id || !cart[id]) return;
        if ($(this).hasClass("plus")) cart[id].qty = Number(cart[id].qty || 0) + 1;
        if ($(this).hasClass("minus")) {
            cart[id].qty = Number(cart[id].qty || 0) - 1;
            if (cart[id].qty <= 0) delete cart[id];
        }
        updateCartDisplay();
    });

    $(document).on("click", ".remove-btn", function (e) {
        e.stopPropagation();
        const id = String($(this).data("id"));
        if (!id) return;
        delete cart[id];
        updateCartDisplay();
    });

    $("#clearCart").on("click", function (e) {
        e.stopPropagation();
        if (confirm("Xoá toàn bộ giỏ hàng?")) {
            cart = {};
            updateCartDisplay();
        }
    });

    $("#checkoutBtn").on("click", function (e) {
        e.stopPropagation();
        if (Object.keys(cart).length === 0) {
            alert("Giỏ hàng trống!");
            return;
        }
        writeCartObject(cart);
        window.location.href = "/Home/DatBan/Index";
    });

    updateCartDisplay();

    /* -------------------- CART DRAWER TOGGLE -------------------- */
    $(function () {
        const $drawer = $("#cartDrawer");
        const $btnOpen = $("#cartBtn");
        const $btnClose = $("#close-cart");

        $btnOpen.on("click", function (e) {
            e.preventDefault();
            e.stopPropagation();
            $drawer.addClass("active");
        });

        $btnClose.on("click", function (e) {
            e.preventDefault();
            e.stopPropagation();
            $drawer.removeClass("active");
        });

        $(document).on("click", function (e) {
            if ($(e.target).closest("#cartDrawer, #cartBtn").length === 0) {
                $drawer.removeClass("active");
            }
        });
    });
});