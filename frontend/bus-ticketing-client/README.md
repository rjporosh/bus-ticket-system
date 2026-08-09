# BusTicketing Client Portal

Customer-facing web application for searching and booking bus tickets online.

## Features

- **Home Page**: Welcome page with feature highlights and how-it-works guide
- **Trip Search**: Search available trips by route and travel date
- **Seat Selection**: Interactive seat map for choosing seats
- **Booking**: Passenger details form with fare calculation
- **My Tickets**: View and manage booked tickets
- **Login**: Optional authentication for returning customers

## Development

```bash
# Install dependencies
npm install

# Run development server
npm start

# Build for production
npm run build:prod

# Run tests
npm test
```

## Configuration

- Development API: `http://localhost:5000/api/v1`
- Production API: `/api/v1` (proxied through nginx)

## Architecture

- Angular 21 with standalone components
- Signals for reactive state management
- Angular Material for UI components
- Lazy-loaded feature modules
- Responsive design with mobile-first approach

## Build & Deploy

The client portal is containerized using Docker multi-stage builds:

```bash
docker build -t bus-ticketing-client ./frontend/bus-ticketing-client
docker run -p 80:80 bus-ticketing-client
```

## Project Structure

```
frontend/bus-ticketing-client/
├── src/
│   ├── app/
│   │   ├── core/           # Shared services, models
│   │   ├── features/       # Feature modules (home, search, booking, my-tickets, auth)
│   │   └── layout/         # Shell layout component
│   ├── assets/             # Styles and static assets
│   └── environments/       # Environment configs
├── Dockerfile
├── nginx.conf
└── package.json