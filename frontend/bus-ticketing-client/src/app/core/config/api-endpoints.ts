export const API_ENDPOINTS = {
  auth: {
    login: '/auth/login',
    register: '/auth/register',
    refresh: '/auth/refresh',
    logout: '/auth/logout',
  },
  trips: {
    forDate: '/schedules/trips',
    search: '/schedules/search/trips',
  },
  booking: {
    availableSeats: (scheduleId: string) => `/booking/schedules/${scheduleId}/seats`,
    sellTicket: '/booking/tickets',
    sellTickets: '/booking/tickets/batch',
    cancelTicket: (ticketId: string) => `/booking/tickets/${ticketId}/cancel`,
    search: '/booking/tickets',
    myTickets: '/booking/my-tickets',
  },
  tickets: {
    myTickets: '/booking/my-tickets',
    cancel: (ticketId: string) => `/booking/tickets/${ticketId}/cancel`,
    qrCode: (ticketId: string) => `/booking/tickets/${ticketId}/qrcode`,
    print: (ticketId: string) => `/booking/tickets/${ticketId}/print`,
  },
  release: {
    current: '/release/current',
    notes: '/release/notes',
  },
} as const;
