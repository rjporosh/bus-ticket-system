// Auto-login pre-request script
// Attach this to any request that requires authentication
// Usage: In the request's "Pre-request Script" tab, paste this script

const role = pm.request.headers.get('x-role');
if (!role) {
    console.log('No x-role header set, skipping auto-login');
    return;
}

let envKey, usernameVar, passwordVar;

switch (role) {
    case 'admin':
        envKey = 'admin_token';
        usernameVar = 'admin_username';
        passwordVar = 'admin_password';
        break;
    case 'staff':
        envKey = 'staff_token';
        usernameVar = 'staff_username';
        passwordVar = 'staff_password';
        break;
    case 'customer':
        envKey = 'customer_token';
        usernameVar = 'customer_username';
        passwordVar = 'customer_password';
        break;
    default:
        console.log('Unknown role:', role);
        return;
}

const username = pm.environment.get(usernameVar);
const password = pm.environment.get(passwordVar);
const baseUrl = pm.environment.get('base_url').replace('/api/v1', '');

pm.sendRequest({
    url: `${baseUrl}/api/v1/auth/login`,
    method: 'POST',
    header: {
        'Content-Type': 'application/json'
    },
    body: {
        mode: 'raw',
        raw: JSON.stringify({ username, password })
    }
}, function (err, res) {
    if (err) {
        console.error('Auto-login failed:', err);
        return;
    }

    const json = res.json();
    if (json.accessToken) {
        pm.environment.set(envKey, json.accessToken);
        pm.environment.set('refresh_token', json.refreshToken);
        console.log(`Auto-login successful for ${role}. Token set.`);
    } else {
        console.error('Auto-login failed: no accessToken in response', json);
    }
});
