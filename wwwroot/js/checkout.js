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
function getUser() {
    const u = localStorage.getItem('user');
    return u ? JSON.parse(u) : null;
}

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

    console.log('=== CHECKOUT LOAD PRODUCT DEBUG ===');
    console.log('🔍 Slug from URL:', slug);

    if (!slug) {
        console.log('❌ No slug found, redirecting to browse');
        window.location.href = '/pages/browse.html';
        return;
    }

    try {
        console.log(`📡 Fetching: ${API}/products/${slug}`);
        const res = await fetch(`${API}/products/${slug}`);
        console.log('📡 Response status:', res.status);

        if (!res.ok) {
            const errorData = await res.json();
            console.log('❌ Error response:', errorData);
            throw new Error('Product not found');
        }

        product = await res.json();
        console.log('✅ Product loaded:', product);
        console.log('📦 Product shopOwnerId:', product.shopOwnerId);
        console.log('📦 Product shopId:', product.shopId);
        console.log('📦 Product status:', product.status);

        // ✅ CHECK: If user owns this shop, block checkout
        const user = getUser();
        console.log('👤 Current user:', user);
        console.log('👤 User ID:', user?.id);
        console.log('👤 User hasShop:', user?.hasShop);

        const isOwnProduct = user && user.hasShop && product.shopOwnerId === user.id;
        console.log('🔍 isOwnProduct check:', {
            userExists: !!user,
            hasShop: user?.hasShop,
            shopOwnerId: product.shopOwnerId,
            userId: user?.id,
            match: user && user.hasShop && product.shopOwnerId === user.id
        });
        console.log('✅ isOwnProduct result:', isOwnProduct);

        if (isOwnProduct) {
            console.log('🚫 User owns this product - blocking checkout');
            document.getElementById('product-preview').innerHTML = `
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
            document.getElementById('checkout-form').style.display = 'none';
            console.log('=== END LOAD PRODUCT (BLOCKED) ===');
            return;
        }

        console.log('✅ User can buy this product - showing checkout');
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
            </div>
        `;

        document.getElementById('qty-stock').textContent = remaining + ' available';
        document.getElementById('qty-row').style.display = 'flex';
        document.getElementById('checkout-form').style.display = 'block';

        updateSummary();
        console.log('=== END LOAD PRODUCT (SUCCESS) ===');

    } catch (e) {
        console.error('❌ Catch error:', e);
        console.log('❌ Product not found, showing error message');
        document.getElementById('product-preview').innerHTML = `
            <p style="color:#991B1B;font-size:13px">Product not found or no longer available.</p>
        `;
        document.getElementById('checkout-form').style.display = 'none';
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
            const addr = addresses.find(a => a.id === selectedAddressId);
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
            body.shippingPhoneNumber = document.getElementById('new-phone').value.trim();
        }

        const res = await fetch(`${API}/orders`, {
            method: 'POST',
            headers: authHeaders(),
            body: JSON.stringify(body)
        });

        const data = await res.json();
        if (!res.ok) {
            errEl.textContent = data.message || 'Could not place order.';
            errEl.classList.add('show');
            return;
        }

        // Redirect to payment page
        window.location.href = `/pages/payment.html?orderId=${data.id}&orderNumber=${data.orderNumber}&amount=${data.totalAmount}`;

    } catch (e) {
        errEl.textContent = 'Something went wrong. Please try again.';
        errEl.classList.add('show');
    } finally {
        btn.disabled = false;
        btn.innerHTML = '<i class="ti ti-lock" aria-hidden="true"></i> Place order securely';
    }
}

// ── INIT ─────────────────────────────────────────────────
loadProduct();
loadAddresses();