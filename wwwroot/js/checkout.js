//const API = 'https://localhost:7237/api';
const API = '/api';
const EMOJIS = {
    'Clothing': '🧥', 'Electronics': '📱',
    'Food & Drinks': '🥤', 'Furniture': '🛋️',
    'Hardware': '🔧', 'Sport & Fitness': '🏋️',
    'Beauty & Health': '💄', 'Other': '📦'
};

const CAT_COLORS = {
    'Clothing': '#E1F5EE', 'Electronics': '#E6F1FB',
    'Food & Drinks': '#FAECE7', 'Furniture': '#FAEEDA',
    'Hardware': '#EEEDFE', 'Sport & Fitness': '#EAF3DE',
    'Beauty & Health': '#FBEAF0', 'Other': '#F1EFE8'
};

// ── STATE ────────────────────────────────────────────────
let product = null;
let quantity = 1;
let selectedAddressId = null;
let useNewAddress = false;
let addresses = [];

// ── AUTH ─────────────────────────────────────────────────
function getToken() { return localStorage.getItem('token'); }
function getUser() {
    const u = localStorage.getItem('user');
    return u ? JSON.parse(u) : null;
}

if (!getToken()) window.location.href = '/pages/login.html';

function logout() {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    window.location.href = '/pages/login.html';
}

function authHeaders() {
    return {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer ' + getToken()
    };
}

function fmt(n) { return 'R' + Number(n).toLocaleString('en-ZA'); }

// Anything rendered via innerHTML has to go through this first. Product
// title/shop name/category come from the seller, not the buyer checking out,
// so a malicious value there could still execute script in the buyer's
// browser during checkout - the single worst place for that to happen.
function escapeHtml(value) {
    if (value === null || value === undefined) return '';
    return String(value)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}

// South African phone numbers in local format: a leading 0 followed by 9
// digits (10 digits total). Spaces/dashes are stripped before checking.
function isValidSaPhone(value) {
    const digitsOnly = value.replace(/[\s-]/g, '');
    return /^0\d{9}$/.test(digitsOnly);
}

// SA postal codes are 4 digits.
function isValidSaPostalCode(value) {
    return /^\d{4}$/.test(value.trim());
}

// Centralizes fetch + auth headers + error handling for the authenticated
// calls in this file (loadAddresses, placeOrder). loadProduct deliberately
// doesn't use this - GET /products/{slug} needs no auth header, and forcing
// one on would be wrong, not an improvement.
async function apiRequest(path, options = {}) {
    const res = await fetch(`${API}${path}`, {
        ...options,
        headers: { ...authHeaders(), ...(options.headers || {}) }
    });

    if (res.status === 401 || res.status === 403) {
        logout();
        throw new Error('Session expired. Please log in again.');
    }

    let data = null;
    try {
        data = await res.json();
    } catch {
        data = null; // e.g. a 204 No Content response has no body to parse
    }

    if (!res.ok) {
        throw new Error(data?.message || `Request failed (${res.status}).`);
    }

    return data;
}

// ── LOAD PRODUCT FROM URL SLUG ────────────────────────────
async function loadProduct() {
    const params = new URLSearchParams(window.location.search);
    const slug = params.get('slug');

    if (!slug) {
        window.location.href = '/pages/browse.html';
        return;
    }

    try {
        const res = await fetch(`${API}/products/${slug}`);
        if (!res.ok) throw new Error('Product not found');

        product = await res.json();

        // If the current user owns this shop, block checkout entirely.
        const user = getUser();
        const isOwnProduct = user && user.hasShop && product.shopOwnerId === user.id;

        if (isOwnProduct) {
            const preview = document.getElementById('product-preview');
            if (preview) {
                preview.innerHTML = `
                    <div style="background: #FEF3C7; padding: 20px; border-radius: 12px; text-align: center;">
                        <div style="font-size: 48px; margin-bottom: 10px;">🚫</div>
                        <h3 style="color: #B45309; margin-bottom: 8px;">You cannot buy your own products</h3>
                        <p style="color: #6b7280; font-size: 14px;">As a seller, you cannot purchase items from your own shop.</p>
                        <button class="btn-buy" onclick="window.location.href='/pages/browse.html'" 
                                style="margin-top: 16px; background: #1D9E75; color: #fff; border: none; padding: 10px 24px; border-radius: 10px; cursor: pointer;">
                            Browse other deals
                        </button>
                    </div>
                `;
            }
            const form = document.getElementById('checkout-form');
            if (form) form.style.display = 'none';
            return;
        }

        const remaining = product.remainingQuantity;

        const preview = document.getElementById('product-preview');
        if (preview) {
            preview.innerHTML = `
                <div class="pp-img" style="background:${CAT_COLORS[product.categoryName] || '#f5f5f3'}">
                    ${EMOJIS[product.categoryName] || '📦'}
                </div>
                <div class="pp-info">
                    <div class="pp-cat">${escapeHtml(product.categoryName || 'General')}</div>
                    <div class="pp-title">${escapeHtml(product.title)}</div>
                    <div class="pp-shop">
                        <i class="ti ti-building-store" style="font-size:11px" aria-hidden="true"></i>
                        ${escapeHtml(product.shopName)}${product.shopCity ? ', ' + escapeHtml(product.shopCity) : ''}
                    </div>
                    <div class="pp-pricing">
                        <span class="pp-price">${fmt(product.salePrice)}</span>
                        <span class="pp-orig">${fmt(product.originalPrice)}</span>
                        <span class="pp-off">-${Math.round(product.discountPercentage)}% off</span>
                    </div>
                </div>
            `;
        }

        const qtyStock = document.getElementById('qty-stock');
        if (qtyStock) qtyStock.textContent = remaining + ' available';

        const qtyRow = document.getElementById('qty-row');
        if (qtyRow) qtyRow.style.display = 'flex';

        const form = document.getElementById('checkout-form');
        if (form) form.style.display = 'block';

        updateSummary();

    } catch (e) {
        console.error('Could not load product:', e);
        const preview = document.getElementById('product-preview');
        if (preview) {
            preview.innerHTML = `
                <div style="background: #FEE2E2; padding: 30px 20px; border-radius: 12px; text-align: center;">
                    <div style="font-size: 48px; margin-bottom: 12px;">🔍</div>
                    <h3 style="color: #991B1B; margin-bottom: 8px;">Product Not Available</h3>
                    <p style="color: #6b7280; font-size: 14px;">The product you're looking for is no longer available.</p>
                    <button onclick="window.location.href='/pages/browse.html'" 
                            style="margin-top: 16px; background: #1D9E75; color: #fff; border: none; padding: 10px 24px; border-radius: 10px; cursor: pointer;">
                        Browse deals
                    </button>
                </div>
            `;
        }
        const form = document.getElementById('checkout-form');
        if (form) form.style.display = 'none';
    }
}

// ── LOAD ADDRESSES ───────────────────────────────────────
async function loadAddresses() {
    try {
        const profile = await apiRequest('/user/profile');
        addresses = profile.addresses;

        const container = document.getElementById('addr-options');

        if (addresses.length === 0) {
            container.innerHTML = `
                <p style="font-size:13px;color:#6b7280;margin-bottom:0.75rem">
                    No saved addresses. Enter one below.
                </p>`;
            showNewAddrForm();
            return;
        }

        // Auto select default address
        const def = addresses.find(a => a.isDefault) || addresses[0];
        selectedAddressId = def.id;

        container.innerHTML = addresses.map(a => `
            <div class="addr-opt ${a.id === selectedAddressId ? 'selected' : ''}"
                 onclick="selectAddress(${a.id})">
                <div class="addr-opt-type">
                    <i class="ti ti-${a.addressType === 'Home' ? 'home' : 'building'}" aria-hidden="true"></i>
                    ${escapeHtml(a.addressType)}
                    ${a.isDefault ? '· <span style="color:#0F6E56">Default</span>' : ''}
                </div>
                <div class="addr-opt-line">${escapeHtml(a.addressLine1)}</div>
                <div class="addr-opt-sub">${escapeHtml(a.city)}, ${escapeHtml(a.province)}, ${escapeHtml(a.postalCode)}</div>
                <div class="addr-radio"></div>
            </div>`).join('') + `
            <button class="new-addr-btn" onclick="toggleNewAddrForm()">
                <i class="ti ti-plus" aria-hidden="true"></i>
                Use a different address
            </button>`;

    } catch (e) {
        console.error('Could not load addresses', e);
    }
}

function selectAddress(id) {
    selectedAddressId = id;
    useNewAddress = false;
    document.getElementById('new-addr-form').style.display = 'none';
    loadAddresses();
}

function toggleNewAddrForm() {
    useNewAddress = !useNewAddress;
    document.getElementById('new-addr-form').style.display = useNewAddress ? 'block' : 'none';
    if (useNewAddress) selectedAddressId = null;
}

function showNewAddrForm() {
    useNewAddress = true;
    document.getElementById('new-addr-form').style.display = 'block';
}

// ── QUANTITY ─────────────────────────────────────────────
function changeQty(delta) {
    if (!product) return;
    const max = product.remainingQuantity;
    const newQty = quantity + delta;
    if (newQty < 1 || newQty > max) return;
    quantity = newQty;
    document.getElementById('qty-val').textContent = quantity;
    updateSummary();
}

// ── UPDATE SUMMARY ───────────────────────────────────────
function updateSummary() {
    if (!product) return;
    const subtotal = product.salePrice * quantity;
    document.getElementById('os-price').textContent = fmt(product.salePrice);
    document.getElementById('os-qty').textContent = '× ' + quantity;
    document.getElementById('os-subtotal').textContent = fmt(subtotal);
    document.getElementById('os-total').textContent = fmt(subtotal);
}

// ── PLACE ORDER ──────────────────────────────────────────
async function placeOrder() {
    const errEl = document.getElementById('checkout-err');
    const btn = document.getElementById('place-btn');

    errEl.classList.remove('show');

    if (!product) {
        errEl.textContent = 'Product data is missing. Please try again.';
        errEl.classList.add('show');
        return;
    }

    // Validate address
    if (!selectedAddressId && !useNewAddress) {
        errEl.textContent = 'Please select a delivery address.';
        errEl.classList.add('show');
        return;
    }

    if (useNewAddress) {
        const line1 = document.getElementById('new-line1').value.trim();
        const city = document.getElementById('new-city').value.trim();
        const province = document.getElementById('new-province').value;
        const postal = document.getElementById('new-postal').value.trim();
        const phone = document.getElementById('new-phone').value.trim();

        if (!line1 || !city || !province || !postal || !phone) {
            errEl.textContent = 'Please fill in all address fields.';
            errEl.classList.add('show');
            return;
        }
        if (!isValidSaPostalCode(postal)) {
            errEl.textContent = 'Please enter a valid 4-digit South African postal code.';
            errEl.classList.add('show');
            return;
        }
        if (!isValidSaPhone(phone)) {
            errEl.textContent = 'Please enter a valid South African phone number, e.g. 082 123 4567.';
            errEl.classList.add('show');
            return;
        }
    }

    btn.disabled = true;
    btn.innerHTML = '<i class="ti ti-loader" aria-hidden="true"></i> Placing order...';

    try {
        const body = {
            productId: product.id,
            quantity: quantity,
            buyerNotes: document.getElementById('buyer-notes').value.trim() || null
        };

        if (selectedAddressId) {
            const addr = addresses.find(a => a.id === selectedAddressId);
            if (!addr) {
                throw new Error('Selected address not found.');
            }
            // Sent alongside savedAddressId as a fallback, not redundantly:
            // OrderService looks the saved address up again server-side and
            // uses that instead whenever the lookup succeeds, but if the
            // address were ever deleted between loading this page and
            // placing the order, these are what the order falls back to
            // rather than failing outright. Don't remove this thinking it's
            // dead weight - it isn't.
            body.savedAddressId = selectedAddressId;
            body.shippingAddressLine1 = addr.addressLine1;
            body.shippingCity = addr.city;
            body.shippingProvince = addr.province;
            body.shippingPostalCode = addr.postalCode;
            body.shippingPhoneNumber = addr.phoneNumber;
        } else {
            body.shippingAddressLine1 = document.getElementById('new-line1').value.trim();
            body.shippingCity = document.getElementById('new-city').value.trim();
            body.shippingProvince = document.getElementById('new-province').value;
            body.shippingPostalCode = document.getElementById('new-postal').value.trim();
            body.shippingPhoneNumber = document.getElementById('new-phone').value.trim().replace(/[\s-]/g, '');
        }

        const data = await apiRequest('/orders', {
            method: 'POST',
            body: JSON.stringify(body)
        });

        // Redirect to payment page
        window.location.href = `/pages/payment.html?orderId=${data.id}&orderNumber=${encodeURIComponent(data.orderNumber)}&amount=${data.totalAmount}`;

    } catch (e) {
        // Out-of-stock errors from OrderService read "Only {n} units available."
        // - matched here to refresh stock and give a friendlier message.
        if (e.message && e.message.includes('Only')) {
            errEl.textContent = 'Sorry! ' + e.message;
            errEl.classList.add('show');
            await loadProduct();
            return;
        }
        errEl.textContent = e.message || 'Could not place order.';
        errEl.classList.add('show');
    } finally {
        btn.disabled = false;
        btn.innerHTML = '<i class="ti ti-lock" aria-hidden="true"></i> Place order securely';
    }
}

// ── INIT ─────────────────────────────────────────────────
loadProduct();
loadAddresses();