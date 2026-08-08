// Extract tokens and IDs from responses
// Attach this to Auth responses to store tokens

if (pm.response.code === 200 || pm.response.code === 201) {
    const json = pm.response.json();
    
    if (json.accessToken) {
        const role = pm.request.headers.get('x-role') || 'unknown';
        let envKey = 'admin_token';
        
        if (role === 'staff') envKey = 'staff_token';
        if (role === 'customer') envKey = 'customer_token';
        
        pm.environment.set(envKey, json.accessToken);
        pm.environment.set('refresh_token', json.refreshToken);
        console.log(`Stored ${envKey} from ${pm.request.name}`);
    }
}
