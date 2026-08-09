// Validate response schema against expected structure
// Attach this to any request to validate the response shape

const expectedSchema = pm.request.headers.get('x-expected-schema');
if (!expectedSchema) return;

let isValid = true;
const json = pm.response.json();

switch (expectedSchema) {
    case 'TicketDto':
        const requiredTicketFields = ['id', 'ticketNumber', 'scheduleId', 'busNumber', 'routeName', 'seatId', 'seatNumber', 'travelDate', 'departureTime', 'passengerName', 'mobileNumber', 'fareAmount', 'status'];
        requiredTicketFields.forEach(field => {
            if (!(field in json)) {
                pm.test(`Missing field: ${field}`, () => { pm.expect(json).to.have.property(field); });
                isValid = false;
            }
        });
        break;
        
    case 'SeatAvailabilityDto':
        const requiredSeatFields = ['seatId', 'seatNumber', 'rowLabel', 'columnNumber', 'class', 'isInService', 'isSold'];
        requiredSeatFields.forEach(field => {
            if (!(field in json)) {
                pm.test(`Missing field: ${field}`, () => { pm.expect(json).to.have.property(field); });
                isValid = false;
            }
        });
        break;
        
    case 'DashboardSummaryDto':
        const requiredDashboardFields = ['date', 'totalSeats', 'soldSeats', 'availableSeats', 'totalSales', 'routeWiseSales', 'busWiseSeatStatus'];
        requiredDashboardFields.forEach(field => {
            if (!(field in json)) {
                pm.test(`Missing field: ${field}`, () => { pm.expect(json).to.have.property(field); });
                isValid = false;
            }
        });
        break;
        
    case 'AuthResponse':
        const requiredAuthFields = ['accessToken', 'refreshToken', 'user'];
        requiredAuthFields.forEach(field => {
            if (!(field in json)) {
                pm.test(`Missing field: ${field}`, () => { pm.expect(json).to.have.property(field); });
                isValid = false;
            }
        });
        break;
        
    default:
        console.log('Unknown schema:', expectedSchema);
}

if (isValid) {
    pm.test(`Schema validation passed for ${expectedSchema}`, () => {
        pm.expect(true).to.be.true;
    });
}
