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

// ── AUTH CHECK ───────────────────────────────────────────
function getToken() { return localStorage.getItem('token'); }
function getUser() {
    const u = localStorage.getItem('user');
    return u ? JSON.parse(u) : null;
}

// Redirect to login if not logged in
const token = getToken();
if (!token) window.location.href = '/pages/login.html';

// ── HELPERS ──────────────────────────────────────────────
function fmt(n) { return 'R' + Number(n).toLocaleString('en-ZA'); }

function authHeaders() {
    return {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer ' + getToken()
    };
}

function statusClass(status) {
    const map = {
        'Pending': 'status-pending',
        'Processing': 'status-processing',
        'Shipped': 'status-shipped',
        'Delivered': 'status-delivered',
        'Cancelled': 'status-cancelled',
        'Refunded': 'status-refunded'
    };
    return map[status] || 'status-pending';
}

function logout() {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    window.location.href = '/pages/login.html';
}

// ── LOAD PROFILE ─────────────────────────────────────────
async function loadProfile() {
    try {
        const res = await fetch(`${API}/user/profile`, { headers: authHeaders() });
        const profile = await res.json();

        // Nav
        document.getElementById('nav-user').textContent =
            'Hi, ' + profile.fullName.split(' ')[0];

        // Hero
        document.getElementById('welcome-msg').textContent =
            'Welcome back, ' + profile.fullName.split(' ')[0];

        // Profile card
        document.getElementById('profile-initial').textContent =
            profile.fullName.charAt(0).toUpperCase();
        document.getElementById('profile-name').textContent = profile.fullName;
        document.getElementById('profile-email').textContent = profile.email;
        document.getElementById('profile-phone').textContent =
            profile.phoneNumber || 'Not set';
        document.getElementById('profile-role').textContent = profile.role;
        document.getElementById('profile-joined').textContent =
            new Date(profile.createdAt).toLocaleDateString('en-ZA', {
                month: 'long', year: 'numeric'
            });
        document.getElementById('profile-verified').textContent =
            profile.isEmailVerified ? '✅ Verified' : '⚠️ Not verified';

        // Pre-fill edit modal
        document.getElementById('edit-name').value = profile.fullName;
        document.getElementById('edit-phone').value = profile.phoneNumber || '';

        // Render addresses
        renderAddresses(profile.addresses);

    } catch (e) {
        console.error('Could not load profile', e);
    }
}

// ── LOAD ORDERS ──────────────────────────────────────────
async function loadOrders() {
    try {
        const res = await fetch(`${API}/orders`, { headers: authHeaders() });
        const orders = await res.json();

        // Update stat
        document.getElementById('stat-orders').textContent = orders.length;

        // Calculate total spent
        const spent = orders
            .filter(o => o.paymentStatus === 'Paid')
            .reduce((sum, o) => sum + o.totalAmount, 0);
        document.getElementById('stat-spent').textContent = fmt(spent);

        if (orders.length === 0) {
            document.getElementById('orders-list').innerHTML = `
        <div class="empty-state">
          <div class="empty-icon">📦</div>
          <h3>No orders yet</h3>
          <p>Find amazing deals and place your first order</p>
          <button class="btn-start" onclick="window.location.href='/pages/browse.html'">
            Browse deals
          </button>
        </div>`;
            return;
        }

        // Show latest 5 orders
        const recent = orders.slice(0, 5);
        document.getElementById('orders-list').innerHTML = recent.map(o => `
      <div class="order-item" onclick="window.location.href='/pages/order-detail.html?id=${o.id}'">
        <div class="order-img" style="background:${CAT_COLORS[o.categoryName] || '#f5f5f3'}">
          ${EMOJIS[o.categoryName] || '📦'}
        </div>
        <div class="order-info">
          <div class="order-title">${o.productTitle}</div>
          <div class="order-meta">
            <span>${o.shopName}</span>
            <span>·</span>
            <span class="order-num">${o.orderNumber}</span>
            <span>·</span>
            <span>${new Date(o.createdAt).toLocaleDateString('en-ZA')}</span>
          </div>
        </div>
        <div class="order-right">
          <div class="order-amount">${fmt(o.totalAmount)}</div>
          <div class="status-badge ${statusClass(o.orderStatus)}">${o.orderStatus}</div>
        </div>
      </div>`).join('');

    } catch (e) {
        console.error('Could not load orders', e);
    }
}

// ── LOAD SAVED PRODUCTS ──────────────────────────────────
async function loadSaved() {
    try {
        const res = await fetch(`${API}/saved-products`, { headers: authHeaders() });
        const saved = await res.json();

        document.getElementById('stat-saved').textContent = saved.length;
        document.getElementById('saved-count').textContent =
            saved.length + ' item' + (saved.length !== 1 ? 's' : '');

        if (saved.length === 0) {
            document.getElementById('saved-grid').innerHTML = `
        <div class="empty-state" style="grid-column:1/-1">
          <div class="empty-icon">🤍</div>
          <h3>No saved deals</h3>
          <p>Tap the heart on any deal to save it here</p>
        </div>`;
            return;
        }

        document.getElementById('saved-grid').innerHTML = saved.map(p => `
      <div class="saved-card" onclick="window.location.href='/pages/browse.html?search=${encodeURIComponent(p.title)}'">
        <div class="saved-img" style="background:${CAT_COLORS[p.categoryName] || '#f5f5f3'}">
          ${EMOJIS[p.categoryName] || '📦'}
        </div>
        <div class="saved-body">
          <div class="saved-title">${p.title}</div>
          <div>
            <span class="saved-price">${fmt(p.salePrice)}</span>
            <span class="saved-orig">${fmt(p.originalPrice)}</span>
          </div>
        </div>
        <button class="saved-remove" onclick="event.stopPropagation();unsave(${p.productId})">
          <i class="ti ti-x" aria-hidden="true"></i>
        </button>
      </div>`).join('');

    } catch (e) {
        console.error('Could not load saved', e);
    }
}

// ── UNSAVE PRODUCT ───────────────────────────────────────
async function unsave(productId) {
    try {
        await fetch(`${API}/saved-products/${productId}`, {
            method: 'DELETE',
            headers: authHeaders()
        });
        loadSaved(); // reload saved grid
    } catch (e) {
        console.error('Could not unsave', e);
    }
}

// ── RENDER ADDRESSES ─────────────────────────────────────
function renderAddresses(addresses) {
    const list = document.getElementById('addr-list');

    if (addresses.length === 0) {
        list.innerHTML = `
      <div style="text-align:center;padding:1rem;font-size:13px;color:#6b7280">
        No addresses yet
      </div>
      <button class="btn-add-addr" onclick="showAddrModal()">
        <i class="ti ti-plus" aria-hidden="true"></i> Add new address
      </button>`;
        return;
    }

    // Sort — default first
    const sorted = [...addresses].sort((a, b) => b.isDefault - a.isDefault);

    list.innerHTML = sorted.map(a => `
    <div class="addr-item ${a.isDefault ? 'default' : ''}"
         onclick="setDefault(${a.id})">
      <div class="addr-type">
        <i class="ti ti-${a.addressType === 'Home' ? 'home' : a.addressType === 'Work' ? 'building' : 'map-pin'}" aria-hidden="true"></i>
        ${a.addressType}
      </div>
      <div class="addr-line">${a.addressLine1}</div>
      <div class="addr-sub">${a.city}, ${a.province}, ${a.postalCode}</div>
      ${a.isDefault ? '<div class="default-badge">Default</div>' : ''}
    </div>`).join('') + `
    <button class="btn-add-addr" onclick="showAddrModal()">
      <i class="ti ti-plus" aria-hidden="true"></i> Add new address
    </button>`;
}

// ── SET DEFAULT ADDRESS ──────────────────────────────────
async function setDefault(addressId) {
    try {
        await fetch(`${API}/user/addresses/${addressId}/default`, {
            method: 'PUT', headers: authHeaders()
        });
        loadProfile();
    } catch (e) {
        console.error('Could not set default', e);
    }
}

// ── EDIT PROFILE MODAL ───────────────────────────────────
function showEditModal() {
    document.getElementById('edit-modal').classList.add('open');
}

function closeEditModal() {
    document.getElementById('edit-modal').classList.remove('open');
}

async function saveProfile() {
    const name = document.getElementById('edit-name').value.trim();
    const phone = document.getElementById('edit-phone').value.trim();
    const errEl = document.getElementById('edit-err');
    const btn = document.getElementById('edit-save-btn');

    if (!name) {
        errEl.textContent = 'Full name is required.';
        errEl.classList.add('show');
        return;
    }

    errEl.classList.remove('show');
    btn.disabled = true;
    btn.textContent = 'Saving...';

    try {
        const res = await fetch(`${API}/user/profile`, {
            method: 'PUT',
            headers: authHeaders(),
            body: JSON.stringify({ fullName: name, phoneNumber: phone })
        });

        if (!res.ok) throw new Error('Failed to update.');

        // Update localStorage user
        const user = getUser();
        user.fullName = name;
        localStorage.setItem('user', JSON.stringify(user));

        closeEditModal();
        loadProfile();

    } catch (e) {
        errEl.textContent = 'Could not update profile. Try again.';
        errEl.classList.add('show');
    } finally {
        btn.disabled = false;
        btn.textContent = 'Save changes';
    }
}

// ── ADD ADDRESS MODAL ────────────────────────────────────
function showAddrModal() {
    document.getElementById('addr-modal').classList.add('open');
}

function closeAddrModal() {
    document.getElementById('addr-modal').classList.remove('open');
}

async function saveAddress() {
    const errEl = document.getElementById('addr-err');
    const btn = document.getElementById('addr-save-btn');

    const body = {
        recipientName: document.getElementById('addr-name').value.trim(),
        addressLine1: document.getElementById('addr-line1').value.trim(),
        addressLine2: document.getElementById('addr-line2').value.trim() || null,
        city: document.getElementById('addr-city').value.trim(),
        province: document.getElementById('addr-province').value,
        postalCode: document.getElementById('addr-postal').value.trim(),
        phoneNumber: document.getElementById('addr-phone').value.trim(),
        addressType: document.getElementById('addr-type').value,
        isDefault: document.getElementById('addr-default').checked
    };

    if (!body.recipientName || !body.addressLine1 || !body.city || !body.province || !body.postalCode || !body.phoneNumber) {
        errEl.textContent = 'Please fill in all required fields.';
        errEl.classList.add('show');
        return;
    }

    errEl.classList.remove('show');
    btn.disabled = true;
    btn.textContent = 'Adding...';

    try {
        const res = await fetch(`${API}/user/addresses`, {
            method: 'POST',
            headers: authHeaders(),
            body: JSON.stringify(body)
        });

        if (!res.ok) throw new Error('Failed to add address.');

        closeAddrModal();
        loadProfile();

        // Clear form
        ['addr-name', 'addr-line1', 'addr-line2', 'addr-city', 'addr-postal', 'addr-phone']
            .forEach(id => document.getElementById(id).value = '');
        document.getElementById('addr-province').value = '';
        document.getElementById('addr-default').checked = false;

    } catch (e) {
        errEl.textContent = 'Could not add address. Try again.';
        errEl.classList.add('show');
    } finally {
        btn.disabled = false;
        btn.textContent = 'Add address';
    }
}
function renderDashboardSwitcher() {
    const user = getUser();
    if (!user || user.role === 'Admin') return;

    const container = document.getElementById('dashboard-switcher');
    if (!container) return;

    // Check which page we're on
    const isSellerPage = window.location.pathname.includes('seller-dashboard');
    const isBuyerPage = window.location.pathname.includes('buyer-dashboard');

    if (user.hasShop) {
        container.innerHTML = `
            <button class="btn-ghost ${isBuyerPage ? 'active' : ''}" 
                    onclick="window.location.href='/pages/buyer-dashboard.html'">
                🛒 Buyer
            </button>
            <button class="btn-ghost ${isSellerPage ? 'active' : ''}" 
                    onclick="window.location.href='/pages/seller-dashboard.html'">
                🏪 Seller
            </button>
        `;
    } else {
        container.innerHTML = `
            <button class="btn-ghost" onclick="window.location.href='/pages/create-shop.html'">
                🏪 Become a seller
            </button>
        `;
    }
}
 // call alongside your existing init calls

// ── INIT ─────────────────────────────────────────────────
loadProfile();
loadOrders();
loadSaved();
renderDashboardSwitcher();