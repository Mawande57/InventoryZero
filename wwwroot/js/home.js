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

const STEPS = {
    buyer: [
        { n: '1', h: 'Browse deals', p: 'Filter by category, location, discount or time left.' },
        { n: '2', h: 'Buy securely', p: 'Pay by card. Money held safely until delivery confirmed.' },
        { n: '3', h: 'Receive or collect', p: 'Delivery in 3–7 days or collect from the shop.' },
        { n: '4', h: 'Rate the seller', p: 'Your review builds trust for the whole community.' }
    ],
    seller: [
        { n: '1', h: 'Create your shop', p: 'Register your business and verify your bank account.' },
        { n: '2', h: 'List your stock', p: 'Upload photos, set liquidation price, pick expiry date.' },
        { n: '3', h: 'Get orders', p: 'Buyers purchase instantly. You get notified immediately.' },
        { n: '4', h: 'Get paid in 24h', p: '85% of the sale lands in your bank within 24 hours.' }
    ]
};

// ── HELPERS ──────────────────────────────────────────────

function daysLeft(dateStr) {
    const diff = new Date(dateStr) - new Date();
    const d = Math.ceil(diff / 86400000);
    if (d <= 0) return 'Expired';
    if (d === 1) return '1d left';
    if (d <= 3) return d + 'd left';
    return d + ' days';
}

function fmt(num) {
    return 'R' + Number(num).toLocaleString('en-ZA');
}

function getUser() {
    const u = localStorage.getItem('user');
    return u ? JSON.parse(u) : null;
}

function getToken() {
    return localStorage.getItem('token');
}

// ── NAV — show user info if logged in ────────────────────

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
      <span style="font-size:13px;color:#666">Hi, ${user.fullName.split(' ')[0]}</span>
      ${dashboardLinks}`;
}

// ── HOW IT WORKS TABS ────────────────────────────────────

function renderSteps(tab) {
    document.getElementById('how-steps').innerHTML = STEPS[tab]
        .map(s => `
      <div class="how-step">
        <div class="how-num">${s.n}</div>
        <h3>${s.h}</h3>
        <p>${s.p}</p>
      </div>`).join('');
}

function switchTab(tab, el) {
    document.querySelectorAll('.how-tab')
        .forEach(t => t.classList.remove('active'));
    el.classList.add('active');
    renderSteps(tab);
}

// ── MODAL ────────────────────────────────────────────────

function openModal(p) {
    const bg = CAT_COLORS[p.categoryName] || '#f5f5f3';
    const modalHero = document.getElementById('m-img');
    modalHero.style.background = bg;
    // clear children except close button then set emoji
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
    document.getElementById('m-verified').innerHTML = '<i class="ti ti-shield-check" aria-hidden="true"></i> Verified';
    document.getElementById('m-desc').textContent = p.shortDescription || 'No description available.';

    const buyBtn = document.getElementById('m-buy');
    buyBtn.textContent = 'Buy now — ' + fmt(p.salePrice);
    buyBtn.onclick = () => {
        if (!getToken()) window.location.href = '/pages/login.html';
        else window.location.href = '/pages/checkout.html?slug=' + p.slug;
    };

    document.getElementById('m-save').onclick = () => {
        if (!getToken()) window.location.href = '/pages/login.html';
        else saveProduct(p.id);
    };

    document.getElementById('modal').classList.add('open');
}

function closeModal(e) {
    if (e.target.id === 'modal') {
        document.getElementById('modal').classList.remove('open');
    }
}

function closeModal(e) {
    if (e.target.id === 'modal') {
        document.getElementById('modal').classList.remove('open');
    }
}

async function saveProduct(productId) {
    try {
        const res = await fetch(`${API}/saved-products`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': 'Bearer ' + getToken()
            },
            body: JSON.stringify({ productId })
        });
        if (res.ok) {
            // Visual feedback on save button
            const btn = document.getElementById('m-save');
            btn.style.borderColor = '#D4537E';
            btn.style.color = '#D4537E';
        }
    } catch (e) {
        console.error('Save failed', e);
    }
}

// ── LOAD CATEGORIES ──────────────────────────────────────

async function loadCategories() {
    try {
        const res = await fetch(`${API}/categories`);
        const data = await res.json();

        document.getElementById('cats-grid').innerHTML = data.map(c => `
      <div class="cat" onclick="window.location.href='/pages/browse.html?category=${c.slug}'">
        <div class="cat-icon" style="background:${CAT_COLORS[c.name] || '#F1EFE8'}">
          ${EMOJIS[c.name] || '📦'}
        </div>
        <span>${c.name}</span>
      </div>`).join('');

    } catch (e) {
        document.getElementById('cats-grid').innerHTML =
            '<p style="font-size:13px;color:#666;grid-column:1/-1">Could not load categories.</p>';
    }
}

// ── LOAD DEALS ───────────────────────────────────────────

async function loadDeals() {
    try {
        const res = await fetch(`${API}/products?sortBy=ending-soon&pageSize=8`);
        const data = await res.json();

        // Update stat
        document.getElementById('s-deals').textContent =
            data.totalCount.toLocaleString() + '+';

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
        <span style="font-size:11px;color:#bbb">${daysLeft(p.listingEndDate)} left</span>
      </div>
    </div>
  </div>`).join('');

    } catch (e) {
        document.getElementById('deals-grid').innerHTML =
            '<p style="font-size:13px;color:#666;padding:1rem">Could not load deals. Make sure the API is running on ' + API + '</p>';
    }
}

function handleSave(productId) {
    if (!getToken()) {
        window.location.href = '/pages/login.html';
    } else {
        saveProduct(productId);
    }
}

// ── INIT ─────────────────────────────────────────────────

renderSteps('buyer');
checkAuth();
loadCategories();
loadDeals();