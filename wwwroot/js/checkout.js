const API = 'https://localhost:7237/api';

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
if (!getToken()) window.location.href = '/pages/login.html';

function authHeaders() {
    return {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer ' + getToken()
    };
}

function fmt(n) { return 'R' + Number(n).toLocaleString('en-ZA'); }

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

        const remaining = product.remainingQuantity;

        document.getElementById('product-preview').innerHTML = `
      <div class="pp-img" style="background:${CAT_COLORS[product.categoryName] || '#f5f5f3'}">
        ${EMOJIS[product.categoryName] || '📦'}
      </div>
      <div class="pp-info">
        <div class="pp-cat">${product.categoryName || 'General'}</div>
        <div class="pp-title">${product.title}</div>
        <div class="pp-shop">
          <i class="ti ti-building-store" style="font-size:11px" aria-hidden="true"></i>
          ${product.shopName}${product.shopCity ? ', ' + product.shopCity : ''}
        </div>
        <div class="pp-pricing">
          <span class="pp-price">${fmt(product.salePrice)}</span>
          <span class="pp-orig">${fmt(product.originalPrice)}</span>
          <span class="pp-off">-${Math.round(product.discountPercentage)}% off</span>
        </div>
      </div>`;

        document.getElementById('qty-stock').textContent =
            remaining + ' available';
        document.getElementById('qty-row').style.display = 'flex';

        updateSummary();

    } catch (e) {
        document.getElementById('product-preview').innerHTML =
            '<p style="color:#991B1B;font-size:13px">Product not found or no longer available.</p>';
    }
}

// ── LOAD ADDRESSES ───────────────────────────────────────
async function loadAddresses() {
    try {
        const res = await fetch(`${API}/user/profile`, { headers: authHeaders() });
        const profile = await res.json();
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
          ${a.addressType}
          ${a.isDefault ? '· <span style="color:#0F6E56">Default</span>' : ''}
        </div>
        <div class="addr-opt-line">${a.addressLine1}</div>
        <div class="addr-opt-sub">${a.city}, ${a.province}, ${a.postalCode}</div>
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
    document.querySelectorAll('.addr-opt').forEach(el => {
        el.classList.toggle('selected', parseInt(el.onclick.toString().match(/\d+/)?.[0]) === id);
    });
    // Re-render to update selection
    loadAddresses();
}

function toggleNewAddrForm() {
    useNewAddress = !useNewAddress;
    document.getElementById('new-addr-form').style.display =
        useNewAddress ? 'block' : 'none';
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

    if (!product) return;

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
            body.savedAddressId = selectedAddressId;
            // Still need these fields even with saved address
            const addr = addresses.find(a => a.id === selectedAddressId);
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
            body.shippingPhoneNumber = document.getElementById('new-phone').value.trim();
        }

        const res = await fetch(`${API}/orders`, {
            method: 'POST',
            headers: authHeaders(),
            body: JSON.stringify(body)
        });

        const data = await res.json();
        console.log('Order response:', res.status, data);
        if (!res.ok) {
            errEl.textContent = data.message || 'Could not place order.';
            errEl.classList.add('show');
            return;
        }

        // Show success
        showSuccess(data);

    } catch (e) {
        errEl.textContent = 'Something went wrong. Please try again.';
        errEl.classList.add('show');
    } finally {
        btn.disabled = false;
        btn.innerHTML = '<i class="ti ti-lock" aria-hidden="true"></i> Place order securely';
    }
}

// ── SUCCESS STATE ────────────────────────────────────────
// ── SUCCESS STATE ────────────────────────────────────────
async function showSuccess(order) {
    try {
        console.log('Initiating payment for order:', order.id);

        const res = await fetch(`${API}/payments/initiate/${order.id}`, {
            method: 'POST',
            headers: authHeaders()
        });

        const payfast = await res.json();
        console.log('PayFast response:', res.status, payfast);

        // Build and auto-submit PayFast form
        const form = document.createElement('form');
        form.method = 'POST';
        form.action = payfast.paymentUrl;

        Object.entries(payfast.formData).forEach(([key, value]) => {
            const input = document.createElement('input');
            input.type = 'hidden';
            input.name = key;
            input.value = value;
            form.appendChild(input);
        });

        document.body.appendChild(form);

        // Show brief message before redirect
        const overlay = document.createElement('div');
        overlay.className = 'success-overlay open';
        overlay.innerHTML = `
            <div class="success-card">
                <div class="success-icon">
                    <i class="ti ti-lock" aria-hidden="true"></i>
                </div>
                <h2>Order placed!</h2>
                <p>Redirecting you to PayFast to complete your payment securely...</p>
                <div class="success-order">${order.orderNumber}</div>
                <div style="font-size:13px;color:#6b7280">Please do not close this page</div>
            </div>`;
        document.body.appendChild(overlay);

        // Submit form after 2 seconds
        setTimeout(() => form.submit(), 2000);

    } catch (e) {
        console.error('Payment initiation failed:', e);
        const overlay = document.createElement('div');
        overlay.className = 'success-overlay open';
        overlay.innerHTML = `
            <div class="success-card">
                <div class="success-icon">
                    <i class="ti ti-check" aria-hidden="true"></i>
                </div>
                <h2>Order placed!</h2>
                <p>Your order has been placed. Complete payment from your dashboard.</p>
                <div class="success-order">${order.orderNumber}</div>
                <button class="btn-go-dash"
                    onclick="window.location.href='/pages/buyer-dashboard.html'">
                    View my orders
                </button>
            </div>`;
        document.body.appendChild(overlay);
    }
}

// ── INIT ─────────────────────────────────────────────────
loadProduct();
loadAddresses();