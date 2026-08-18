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

const DOT_COLORS = {
    'Clothing': '#1D9E75', 'Electronics': '#185FA5',
    'Food & Drinks': '#993C1D', 'Furniture': '#854F0B',
    'Hardware': '#534AB7', 'Sport & Fitness': '#3B6D11',
    'Beauty & Health': '#993556', 'Other': '#5F5E5A'
};

// ── STATE ────────────────────────────────────────────────
let state = {
    search: '', categorySlug: '', minPrice: '',
    maxPrice: '', condition: '', isUrgent: false,
    sortBy: 'newest', page: 1, pageSize: 12
};

// ── HELPERS ──────────────────────────────────────────────
function fmt(n) { return 'R' + Number(n).toLocaleString('en-ZA'); }

function daysLeft(dateStr) {
    const diff = new Date(dateStr) - new Date();
    const d = Math.ceil(diff / 86400000);
    if (d <= 0) return 'Expired';
    if (d === 1) return '1d left';
    return d + 'd left';
}

function getToken() { return localStorage.getItem('token'); }

function getUser() {
    const u = localStorage.getItem('user');
    return u ? JSON.parse(u) : null;
}

// ── NAV AUTH CHECK ───────────────────────────────────────
function checkAuth() {
    const user = getUser();
    const navRight = document.getElementById('nav-right');
    if (!user) return;

    const isAdmin = user.role === 'Admin';
    let dashboardLinks = '';

    if (isAdmin) {
        dashboardLinks = `<button class="btn-solid" onclick="window.location.href='/pages/admin-dashboard.html'">Dashboard</button>`;
    } else {
        dashboardLinks += `<button class="btn-ghost" onclick="window.location.href='/pages/buyer-dashboard.html'">Buyer dashboard</button>`;
        dashboardLinks += user.hasShop
            ? `<button class="btn-solid" onclick="window.location.href='/pages/seller-dashboard.html'">Seller dashboard</button>`
            : `<button class="btn-solid" onclick="window.location.href='/pages/register.html?role=Seller'">Become a seller</button>`;
    }

    navRight.innerHTML = `
      <span style="font-size:13px;color:rgba(255,255,255,0.6)">Hi, ${user.fullName.split(' ')[0]}</span>
      ${dashboardLinks}`;
}


// ── FILTER HELPERS ───────────────────────────────────────
function selectCat(el) {
    document.querySelectorAll('.filter-cat').forEach(c => c.classList.remove('active'));
    el.classList.add('active');
    state.categorySlug = el.dataset.slug || '';
}

function selectCond(el, val) {
    document.querySelectorAll('.cond-btn').forEach(b => b.classList.remove('active'));
    el.classList.add('active');
    state.condition = val;
}

function clearFilters() {
    state = { ...state, search: '', categorySlug: '', minPrice: '', maxPrice: '', condition: '', isUrgent: false, page: 1 };
    document.getElementById('search-input').value = '';
    document.getElementById('min-price').value = '';
    document.getElementById('max-price').value = '';
    document.getElementById('urgent-toggle').classList.remove('on');
    document.querySelectorAll('.filter-cat').forEach((c, i) => c.classList.toggle('active', i === 0));
    document.querySelectorAll('.cond-btn').forEach((b, i) => b.classList.toggle('active', i === 0));
    loadDeals();
    updateActiveTags();
}

function applyFilters() {
    state.search = document.getElementById('search-input').value.trim();
    state.minPrice = document.getElementById('min-price').value;
    state.maxPrice = document.getElementById('max-price').value;
    state.isUrgent = document.getElementById('urgent-toggle').classList.contains('on');
    state.sortBy = document.getElementById('sort-select').value;
    state.page = 1;
    loadDeals();
    updateActiveTags();
}

function updateActiveTags() {
    const tags = [];
    if (state.search) tags.push({ label: 'Search: ' + state.search, key: 'search' });
    if (state.categorySlug) tags.push({ label: state.categorySlug, key: 'categorySlug' });
    if (state.condition) tags.push({ label: state.condition, key: 'condition' });
    if (state.isUrgent) tags.push({ label: 'Urgent only', key: 'isUrgent' });
    if (state.minPrice) tags.push({ label: 'Min R' + state.minPrice, key: 'minPrice' });
    if (state.maxPrice) tags.push({ label: 'Max R' + state.maxPrice, key: 'maxPrice' });

    document.getElementById('active-filters').innerHTML = tags.map(t => `
    <div class="filter-tag" onclick="removeTag('${t.key}')">
      ${t.label} <i class="ti ti-x" aria-hidden="true"></i>
    </div>`).join('');
}

function removeTag(key) {
    if (key === 'isUrgent') {
        state.isUrgent = false;
        document.getElementById('urgent-toggle').classList.remove('on');
    } else {
        state[key] = '';
        if (key === 'search') document.getElementById('search-input').value = '';
        if (key === 'minPrice') document.getElementById('min-price').value = '';
        if (key === 'maxPrice') document.getElementById('max-price').value = '';
        if (key === 'categorySlug') {
            document.querySelectorAll('.filter-cat').forEach((c, i) => c.classList.toggle('active', i === 0));
        }
        if (key === 'condition') {
            document.querySelectorAll('.cond-btn').forEach((b, i) => b.classList.toggle('active', i === 0));
        }
    }
    state.page = 1;
    loadDeals();
    updateActiveTags();
}

// ── LOAD CATEGORIES INTO SIDEBAR ─────────────────────────
async function loadCategories() {
    try {
        const res = await fetch(`${API}/categories`);
        const data = await res.json();
        const grid = document.getElementById('filter-cats');

        data.forEach(c => {
            const el = document.createElement('div');
            el.className = 'filter-cat';
            el.dataset.slug = c.slug;

            // Auto select if URL param matches
            if (state.categorySlug === c.slug) el.classList.add('active');

            el.innerHTML = `
        <div class="filter-cat-dot" style="background:${DOT_COLORS[c.name] || '#6b7280'}"></div>
        <span>${c.name}</span>`;
            el.onclick = () => selectCat(el);
            grid.appendChild(el);
        });
    } catch (e) {
        console.error('Could not load categories', e);
    }
}

// ── LOAD DEALS ───────────────────────────────────────────
async function loadDeals() {
    // Show skeletons while loading
    document.getElementById('deals-grid').innerHTML =
        '<div class="deal-skeleton"></div>'.repeat(6);

    const params = new URLSearchParams();
    if (state.search) params.set('search', state.search);
    if (state.categorySlug) params.set('categorySlug', state.categorySlug);
    if (state.minPrice) params.set('minPrice', state.minPrice);
    if (state.maxPrice) params.set('maxPrice', state.maxPrice);
    if (state.condition) params.set('condition', state.condition);
    if (state.isUrgent) params.set('isUrgent', 'true');
    params.set('sortBy', state.sortBy);
    params.set('page', state.page);
    params.set('pageSize', state.pageSize);

    try {
        const res = await fetch(`${API}/products?${params}`);
        const data = await res.json();

        document.getElementById('result-count').textContent =
            data.totalCount.toLocaleString() + ' deals';
        document.getElementById('showing-count').textContent =
            data.items.length + ' of ' + data.totalCount;

        if (data.items.length === 0) {
            document.getElementById('deals-grid').innerHTML = `
        <div class="empty-state">
          <div class="empty-icon">🔍</div>
          <h3>No deals found</h3>
          <p>Try adjusting your filters or search term</p>
        </div>`;
            document.getElementById('pagination').innerHTML = '';
            return;
        }

        document.getElementById('deals-grid').innerHTML = data.items.map(p => `
      <div class="deal" onclick='openModal(${JSON.stringify(p)})'>
        <div class="deal-img" style="background:${CAT_COLORS[p.categoryName] || '#f5f5f3'}">
          ${EMOJIS[p.categoryName] || '📦'}
          <div class="deal-badges">
            <div class="badge-off">-${Math.round(p.discountPercentage)}%</div>
            ${p.isUrgent
                ? `<div class="badge-urgent">
                   <i class="ti ti-clock" aria-hidden="true"></i>
                   ${daysLeft(p.listingEndDate)}
                 </div>`
                : ''}
          </div>
          <button class="deal-save-btn" onclick="event.stopPropagation();handleSave(${p.id})">
            <i class="ti ti-heart" aria-hidden="true"></i>
          </button>
        </div>
        <div class="deal-body">
          <div class="deal-cat">${p.categoryName || 'General'}</div>
          <div class="deal-title">${p.title}</div>
          <div class="deal-shop">
            <i class="ti ti-building-store" style="font-size:11px" aria-hidden="true"></i>
            ${p.shopName}${p.shopCity ? ', ' + p.shopCity : ''}
          </div>
          <div class="deal-pricing">
            <span class="deal-price">${fmt(p.salePrice)}</span>
            <span class="deal-orig">${fmt(p.originalPrice)}</span>
          </div>
          <div class="deal-foot">
            <span class="deal-stock">
              <span class="stock-dot"></span>
              ${p.remainingQuantity} left
            </span>
            <span style="font-size:11px;color:#bbb">${daysLeft(p.listingEndDate)}</span>
          </div>
        </div>
      </div>`).join('');

        renderPagination(data);

    } catch (e) {
        document.getElementById('deals-grid').innerHTML = `
      <p style="font-size:13px;color:#666;padding:1rem;grid-column:1/-1">
        Could not load deals. Make sure the API is running.
      </p>`;
    }
}

// ── PAGINATION ───────────────────────────────────────────
function renderPagination(data) {
    if (data.totalPages <= 1) {
        document.getElementById('pagination').innerHTML = '';
        return;
    }

    let html = `
    <button class="page-btn" onclick="goPage(${data.page - 1})" ${data.page === 1 ? 'disabled' : ''}>
      <i class="ti ti-arrow-left" aria-hidden="true"></i>
    </button>`;

    for (let i = 1; i <= data.totalPages; i++) {
        if (i === 1 || i === data.totalPages || Math.abs(i - data.page) <= 1) {
            html += `<button class="page-btn ${i === data.page ? 'active' : ''}" onclick="goPage(${i})">${i}</button>`;
        } else if (Math.abs(i - data.page) === 2) {
            html += `<span class="page-info">...</span>`;
        }
    }

    html += `
    <button class="page-btn" onclick="goPage(${data.page + 1})" ${data.page === data.totalPages ? 'disabled' : ''}>
      <i class="ti ti-arrow-right" aria-hidden="true"></i>
    </button>`;

    document.getElementById('pagination').innerHTML = html;
}

function goPage(p) {
    state.page = p;
    loadDeals();
    window.scrollTo(0, 0);
}

// ── MODAL ────────────────────────────────────────────────
function openModal(p) {
    const modalHero = document.getElementById('m-img');
    modalHero.style.background = CAT_COLORS[p.categoryName] || '#f5f5f3';
    const closeBtn = modalHero.querySelector('.modal-close');
    modalHero.innerHTML = '';
    modalHero.appendChild(closeBtn);
    modalHero.insertAdjacentText('beforeend', EMOJIS[p.categoryName] || '📦');

    document.getElementById('m-cat').textContent = p.categoryName || 'General';
    document.getElementById('m-title').textContent = p.title;
    document.getElementById('m-price').textContent = fmt(p.salePrice);
    document.getElementById('m-orig').textContent = fmt(p.originalPrice);
    document.getElementById('m-disc').textContent = '-' + Math.round(p.discountPercentage) + '% off';
    document.getElementById('m-cond').textContent = p.condition;
    document.getElementById('m-stock').textContent = p.remainingQuantity + ' units';
    document.getElementById('m-city').textContent = p.shopCity || 'South Africa';
    document.getElementById('m-ends').textContent = daysLeft(p.listingEndDate);
    document.getElementById('m-shop').textContent = p.shopName;
    document.getElementById('m-shoploc').textContent = p.shopCity || '';
    document.getElementById('m-shop-row').onclick = () => {
        window.location.href = '/pages/shop-profile.html?id=' + p.shopId;
    };
    document.getElementById('m-verified').innerHTML =
        '<i class="ti ti-shield-check" aria-hidden="true"></i> Verified';
    document.getElementById('m-desc').textContent =
        p.shortDescription || 'No description available.';

    const buyBtn = document.getElementById('m-buy');
    buyBtn.textContent = 'Buy now — ' + fmt(p.salePrice);
    buyBtn.onclick = () => {
        if (!getToken()) window.location.href = '/pages/login.html';
        else window.location.href = '/pages/checkout.html?slug=' + p.slug;
    };

    document.getElementById('m-save').onclick = () => {
        if (!getToken()) window.location.href = '/pages/login.html';
        else handleSave(p.id);
    };

    document.getElementById('modal').classList.add('open');
}

function closeModal() {
    document.getElementById('modal').classList.remove('open');
}



// ── INIT ─────────────────────────────────────────────────

// Read URL params on load
const urlParams = new URLSearchParams(window.location.search);
if (urlParams.get('category')) state.categorySlug = urlParams.get('category');
if (urlParams.get('sort')) state.sortBy = urlParams.get('sort');
if (urlParams.get('search')) {
    state.search = urlParams.get('search');
    document.getElementById('search-input').value = state.search;
}
// ── SAVE/UNSAVE PRODUCT ────────────────────────────────
async function handleSave(productId) {
    if (!getToken()) {
        window.location.href = '/pages/login.html';
        return;
    }

    try {
        const res = await fetch(`${API}/saved-products`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': 'Bearer ' + getToken()
            },
            body: JSON.stringify(productId)
        });

        if (res.ok) {
            // Toggle heart icon
            const hearts = document.querySelectorAll(`.deal-save-btn[data-product-id="${productId}"] i, .btn-save-p i`);
            hearts.forEach(heart => {
                heart.classList.toggle('ti-heart');
                heart.classList.toggle('ti-heart-filled');
                heart.style.color = heart.classList.contains('ti-heart-filled') ? '#E24B4A' : '#6b7280';
            });

            // Show feedback
            showToast('Product saved! ❤️');
        } else if (res.status === 409) {
            // Already saved - unsave it
            await handleUnsave(productId);
        }
    } catch (e) {
        console.error('Save error:', e);
    }
}

async function handleUnsave(productId) {
    try {
        const res = await fetch(`${API}/saved-products/${productId}`, {
            method: 'DELETE',
            headers: {
                'Authorization': 'Bearer ' + getToken()
            }
        });

        if (res.ok) {
            const hearts = document.querySelectorAll(`.deal-save-btn[data-product-id="${productId}"] i, .btn-save-p i`);
            hearts.forEach(heart => {
                heart.classList.remove('ti-heart-filled');
                heart.classList.add('ti-heart');
                heart.style.color = '#6b7280';
            });
            showToast('Removed from saved 🤍');
        }
    } catch (e) {
        console.error('Unsave error:', e);
    }
}

// ── TOAST NOTIFICATION ───────────────────────────────────
function showToast(message) {
    let toast = document.getElementById('toast');
    if (!toast) {
        toast = document.createElement('div');
        toast.id = 'toast';
        toast.style.cssText = `
            position: fixed; bottom: 2rem; left: 50%; transform: translateX(-50%);
            background: #0a0a0a; color: #fff; padding: 10px 24px;
            border-radius: 12px; font-size: 14px; z-index: 999;
            transition: opacity 0.3s; opacity: 0;
            pointer-events: none;
        `;
        document.body.appendChild(toast);
    }
    toast.textContent = message;
    toast.style.opacity = '1';
    setTimeout(() => { toast.style.opacity = '0'; }, 2500);
}

// ── CHECK IF PRODUCT IS SAVED ───────────────────────────
async function checkSavedStatus(productId) {
    if (!getToken()) return;
    try {
        const res = await fetch(`${API}/saved-products`, {
            headers: { 'Authorization': 'Bearer ' + getToken() }
        });
        const saved = await res.json();
        const isSaved = saved.some(s => s.productId === productId);
        if (isSaved) {
            const hearts = document.querySelectorAll(`.deal-save-btn[data-product-id="${productId}"] i, .btn-save-p i`);
            hearts.forEach(heart => {
                heart.classList.remove('ti-heart');
                heart.classList.add('ti-heart-filled');
                heart.style.color = '#E24B4A';
            });
        }
    } catch (e) {
        console.error('Check saved error:', e);
    }
}
checkAuth();
loadCategories();
loadDeals();