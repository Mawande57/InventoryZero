const API = 'https://localhost:7237/api';

// ── SHARED HELPERS ──────────────────────────────────────
function showError(msg) {
    const el = document.getElementById('err-msg');
    el.textContent = msg;
    el.classList.add('show');
}

function hideError() {
    document.getElementById('err-msg').classList.remove('show');
}

// Permissive on purpose - this only needs to catch obviously malformed input
// ("no @", "no dot"), not enforce the full email spec. The server is the real
// authority on whether an address is valid; this is just a first line of UX.
function isValidEmail(value) {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value);
}

// South African numbers in local format: a leading 0 followed by 9 digits
// (10 digits total) - covers mobile prefixes (06/07/08) and landline area
// codes (01/02/03 etc) alike. Spaces/dashes are stripped before checking so
// "082 123 4567" and "082-123-4567" both validate.
function isValidSaPhone(value) {
    const digitsOnly = value.replace(/[\s-]/g, '');
    return /^0\d{9}$/.test(digitsOnly);
}

// After login/register we save the token and user info
// so every other page can use it
function saveSession(data) {
    localStorage.setItem('token', data.token);
    localStorage.setItem('user', JSON.stringify({
        id: data.userId,
        fullName: data.fullName,
        email: data.email,
        role: data.role,
        hasShop: data.hasShop   // NEW — comes straight from AuthResponseDto.HasShop (real bool, JSON-serialized fine)
    }));
}

function redirectByRole(role, hasShop) {
    if (role === 'Admin') {
        window.location.href = '/pages/admin-dashboard.html';
    } else if (hasShop) {
        window.location.href = '/pages/seller-dashboard.html';
    } else {
        window.location.href = '/pages/index.html';
    }
}

// ── LOGIN PAGE ───────────────────────────────────────────
const loginBtn = document.getElementById('login-btn');
if (loginBtn) {
    loginBtn.addEventListener('click', async () => {
        hideError();

        const email = document.getElementById('email').value.trim();
        const password = document.getElementById('password').value;

        if (!email || !password) return showError('Please fill in all fields.');
        if (!isValidEmail(email)) return showError('Please enter a valid email address.');

        loginBtn.disabled = true;
        loginBtn.textContent = 'Signing in...';

        try {
            // Call POST /api/auth/login
            const res = await fetch(`${API}/auth/login`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ email, password })
            });

            const data = await res.json();

            if (!res.ok) {
                showError(data.message || 'Login failed.');
                return;
            }

            saveSession(data);
            redirectByRole(data.role, data.hasShop);

        } catch (err) {
            showError('Could not connect to server. Try again.');
        } finally {
            loginBtn.disabled = false;
            loginBtn.textContent = 'Sign in';
        }
    });
}

// ── REGISTER PAGE ────────────────────────────────────────
const registerBtn = document.getElementById('register-btn');
if (registerBtn) {

    // Get role from URL param
    const urlParams = new URLSearchParams(window.location.search);
    const roleParam = urlParams.get('role');

    // Set default role
    let selectedRole = 'Buyer';
    if (roleParam === 'Seller') {
        selectedRole = 'Buyer'; // Always store as Buyer
        // Show a message that they'll create a shop after
        const msg = document.createElement('div');
        msg.style.cssText = `
            background: #E1F5EE;
            color: #0F6E56;
            padding: 10px 16px;
            border-radius: 8px;
            margin-bottom: 1rem;
            font-size: 14px;
            text-align: center;
        `;
        msg.innerHTML = `
            <i class="ti ti-building-store" style="margin-right:8px;"></i>
            You'll create your shop right after registration!
        `;
        const form = document.querySelector('.auth-form');
        if (form) {
            form.insertBefore(msg, form.firstChild);
        }
    }

    // Role toggle buttons
    document.querySelectorAll('.role-btn').forEach(btn => {
        btn.addEventListener('click', () => {
            document.querySelectorAll('.role-btn').forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            document.getElementById('role').value = btn.dataset.role;
        });
    });

    registerBtn.addEventListener('click', async () => {
        hideError();

        const fullName = document.getElementById('fullName').value.trim();
        const email = document.getElementById('email').value.trim();
        const phoneRaw = document.getElementById('phone').value.trim();
        const password = document.getElementById('password').value;

        if (!fullName || !email || !password) return showError('Please fill in all required fields.');
        if (fullName.length < 2) return showError('Please enter your full name.');
        if (!isValidEmail(email)) return showError('Please enter a valid email address.');
        if (password.length < 8) return showError('Password must be at least 8 characters.');

        // Phone stays optional (matches RegisterDto/AuthService, which never
        // require it) - only validated if the field wasn't left blank.
        if (phoneRaw && !isValidSaPhone(phoneRaw)) {
            return showError('Please enter a valid South African phone number, e.g. 082 123 4567.');
        }

        // Send digits only - whatever spacing/dashes the user typed doesn't
        // need to end up stored in the database.
        const phone = phoneRaw.replace(/[\s-]/g, '');

        registerBtn.disabled = true;
        registerBtn.textContent = 'Creating account...';

        try {
            const res = await fetch(`${API}/auth/register`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ fullName, email, password, phoneNumber: phone, role: 'Buyer' })
            });

            const data = await res.json();

            if (!res.ok) {
                showError(data.message || 'Registration failed.');
                return;
            }

            saveSession(data);

            // If they came from "Become a seller" link, redirect to create shop
            if (roleParam === 'Seller') {
                window.location.href = '/pages/create-shop.html';
            } else {
                redirectByRole(data.role, data.hasShop);
            }

        } catch (err) {
            showError('Could not connect to server. Try again.');
        } finally {
            registerBtn.disabled = false;
            registerBtn.textContent = 'Create account';
        }
    });
}