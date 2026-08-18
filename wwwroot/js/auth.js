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
    if (role === 'Admin') window.location.href = '/pages/admin-dashboard.html';
    else if (hasShop) window.location.href = '/pages/seller-dashboard.html';
    else window.location.href = '/pages/index.html'
        alert("You were not identified as  a user please wait patiently your identity will be resolved soon...");
}

// ── LOGIN PAGE ───────────────────────────────────────────
const loginBtn = document.getElementById('login-btn');
if (loginBtn) {
    loginBtn.addEventListener('click', async () => {
        hideError();

        const email = document.getElementById('email').value.trim();
        const password = document.getElementById('password').value;

        if (!email || !password) return showError('Please fill in all fields.');

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
        const phone = document.getElementById('phone').value.trim();
        const password = document.getElementById('password').value;
        const role = document.getElementById('role').value;

        if (!fullName || !email || !password) return showError('Please fill in all required fields.');
        if (password.length < 8) return showError('Password must be at least 8 characters.');
        


        registerBtn.disabled = true;
        registerBtn.textContent = 'Creating account...';

        try {
            // Call POST /api/auth/register
            const res = await fetch(`${API}/auth/register`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ fullName, email, password, phoneNumber: phone, role })
            });

            const data = await res.json();

            if (!res.ok) {
                showError(data.message || 'Registration failed.');
                return;
            }

            saveSession(data);
            redirectByRole(data.role, data.hasShop);

        } catch (err) {
            showError('Could not connect to server. Try again.');
        } finally {
            registerBtn.disabled = false;
            registerBtn.textContent = 'Create account';
        }
    });
}