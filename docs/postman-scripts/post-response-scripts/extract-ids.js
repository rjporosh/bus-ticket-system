// Extract IDs from create/update responses
// Attach this to POST/PUT responses to auto-capture IDs for chained requests

if (pm.response.code === 200 || pm.response.code === 201) {
    const json = pm.response.json();
    
    if (json.id) {
        const resourceType = pm.request.headers.get('x-resource-type') || 'unknown';
        let envKey = resourceType + '_id';
        
        pm.environment.set(envKey, json.id);
        console.log(`Stored ${envKey}: ${json.id}`);
    }
    
    if (json.ticketNumber) {
        pm.environment.set('ticket_number', json.ticketNumber);
        console.log(`Stored ticket_number: ${json.ticketNumber}`);
    }
    
    if (json.paymentId) {
        pm.environment.set('payment_id', json.paymentId);
        console.log(`Stored payment_id: ${json.paymentId}`);
    }
}
