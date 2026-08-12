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
    if (user) {
        const dash = user.role === 'Seller'
            ? '/pages/seller-dashboard.html'
            : user.role === 'Admin'
                ? '/pages/admin-dashboard.html'
                : '/pages/buyer-dashboard.html';
        navRight.innerHTML = `
      <span style="font-size:13px;color:rgba(255,255,255,0.6)">
        Hi, ${user.fullName.split(' ')[0]}
      </span>
      <button class="btn-solid" onclick="window.location.href='${dash}'">Dashboard</button>`;
    }
}

// ── LOAD SHOP PROFILE ────────────────────────────────────
async function loadShop(shopId) {
    try {
        const res = await fetch(`${API}/shops/${shopId}`);

        if (!res.ok) {
            document.getElementById('sh-name').textContent = 'Shop not found';
            return;
        }

        const shop = await res.json();

        // Fill in shop details
        document.getElementById('sh-name').textContent = shop.shopName;
        document.title = shop.shopName + ' — InventoryZero';

        if (shop.isVerified) {
            document.getElementById('sh-verified').style.display = 'flex';
        }

        if (shop.city) {
            document.getElementById('sh-location').style.display = 'flex';
            document.getElementById('sh-city').textContent =
                shop.city + (shop.province ? ', ' + shop.province : '');
        }

        // Join date
        const joined = new Date(shop.createdAt);
        document.getElementById('sh-join-date').textContent =
            'Joined ' + joined.toLocaleDateString('en-ZA', { month: 'long', year: 'numeric' });

        // Rating
        document.getElementById('sh-rating').textContent =
            shop.ownerRating > 0 ? shop.ownerRating.toFixed(1) : 'New';

        // Stats
        document.getElementById('sh-sales').textContent =
            shop.totalSales.toLocaleString();
        document.getElementById('sh-revenue').textContent =
            fmt(shop.totalRevenue);
        document.getElementById('sh-reviews').textContent =
            shop.ownerTotalReviews.toLocaleString();

        // Description
        if (shop.shopDescription) {
            document.getElementById('sh-description').textContent = shop.shopDescription;
            document.getElementById('shop-desc').style.display = 'block';
        }

        // Tags
        const tags = [];
        if (shop.city) tags.push(shop.city);
        if (shop.isVerified) tags.push('Verified business');
        tags.push('Fast shipping');
        document.getElementById('sh-tags').innerHTML = tags
            .map(t => `<span class="shop-tag">${t}</span>`).join('');

    } catch (e) {
        document.getElementById('sh-name').textContent = 'Could not load shop';
        console.error(e);
    }
}

// ── LOAD SHOP PRODUCTS ───────────────────────────────────
async function loadProducts(shopId) {
    try {
        const res = await fetch(`${API}/shops/${shopId}/products`);
        const products = await res.json();

        // Update listing count in stats and header
        document.getElementById('sh-listings').textContent = products.length;
        document.getElementById('sh-listing-count').textContent =
            products.length + ' deal' + (products.length !== 1 ? 's' : '') + ' available right now';

        if (products.length === 0) {
            document.getElementById('deals-grid').innerHTML = `
        <div class="empty-state">
          <div class="empty-icon">📭</div>
          <h3>No active listings</h3>
          <p>This shop has no deals right now. Check back soon.</p>
        </div>`;
            return;
        }

        document.getElementById('deals-grid').innerHTML = products.map(p => `
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
          <div class="deal-shop">${p.shopName}</div>
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

    } catch (e) {
        document.getElementById('deals-grid').innerHTML = `
      <p style="font-size:13px;color:#666;padding:1rem;grid-column:1/-1">
        Could not load listings.
      </p>`;
    }
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

function handleSave(productId) {
    if (!getToken()) {
        window.location.href = '/pages/login.html';
        return;
    }
    console.log('Save product', productId);
}

function handleContact() {
    if (!getToken()) {
        window.location.href = '/pages/login.html';
    } else {
        alert('Messaging coming in Phase 6.');
    }
}

// ── INIT ───────────────────────────────────t ──────────────

// Get shopId from URL — shop-profile.html?id=1
const urlParams = new URLSearchParams(window.location.search);
const shopId = urlParams.get('id');

if (!shopId) {
    document.getElementById('sh-name').textContent = 'No shop specified';
} else {
    checkAuth();
    loadShop(shopId);
    loadProducts(shopId);
}