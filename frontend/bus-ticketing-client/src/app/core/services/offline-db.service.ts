export interface OfflineTicket {
  id: string;
  ticketId: string;
  ticketNumber: string;
  passengerName: string;
  routeName: string;
  busNumber: string;
  seatNumber: string;
  travelDate: string;
  departureTime: string;
  fareAmount: number;
  status: string;
  qrCode?: string;
  createdAt: string;
  syncedAt?: string;
}

export interface OfflineBooking {
  id: string;
  scheduleId: string;
  seatId: string;
  passengerName: string;
  mobileNumber: string;
  fareAmount: number;
  paymentMethod: string;
  createdAt: string;
  syncedAt?: string;
}

const DB_NAME = 'bus-ticketing-offline';
const DB_VERSION = 1;

function openDatabase(): Promise<IDBDatabase> {
  return new Promise((resolve, reject) => {
    const request = indexedDB.open(DB_NAME, DB_VERSION);

    request.onerror = () => reject(request.error);
    request.onsuccess = () => resolve(request.result);

    request.onupgradeneeded = (event) => {
      const db = (event.target as IDBOpenDBRequest).result;

      if (!db.objectStoreNames.contains('tickets')) {
        const ticketStore = db.createObjectStore('tickets', { keyPath: 'id' });
        ticketStore.createIndex('ticketId', 'ticketId', { unique: true });
        ticketStore.createIndex('createdAt', 'createdAt', { unique: false });
      }

      if (!db.objectStoreNames.contains('bookings')) {
        const bookingStore = db.createObjectStore('bookings', { keyPath: 'id' });
        bookingStore.createIndex('scheduleId', 'scheduleId', { unique: false });
        bookingStore.createIndex('createdAt', 'createdAt', { unique: false });
      }
    };
  });
}

export class OfflineDbService {
  async saveTicket(ticket: OfflineTicket): Promise<void> {
    const db = await openDatabase();
    return new Promise((resolve, reject) => {
      const tx = db.transaction('tickets', 'readwrite');
      const store = tx.objectStore('tickets');
      const request = store.put(ticket);
      request.onsuccess = () => resolve();
      request.onerror = () => reject(request.error);
    });
  }

  async getTicket(id: string): Promise<OfflineTicket | undefined> {
    const db = await openDatabase();
    return new Promise((resolve, reject) => {
      const tx = db.transaction('tickets', 'readonly');
      const store = tx.objectStore('tickets');
      const request = store.get(id);
      request.onsuccess = () => resolve(request.result);
      request.onerror = () => reject(request.error);
    });
  }

  async getAllTickets(): Promise<OfflineTicket[]> {
    const db = await openDatabase();
    return new Promise((resolve, reject) => {
      const tx = db.transaction('tickets', 'readonly');
      const store = tx.objectStore('tickets');
      const request = store.getAll();
      request.onsuccess = () => resolve(request.result || []);
      request.onerror = () => reject(request.error);
    });
  }

  async deleteTicket(id: string): Promise<void> {
    const db = await openDatabase();
    return new Promise((resolve, reject) => {
      const tx = db.transaction('tickets', 'readwrite');
      const store = tx.objectStore('tickets');
      const request = store.delete(id);
      request.onsuccess = () => resolve();
      request.onerror = () => reject(request.error);
    });
  }

  async saveBooking(booking: OfflineBooking): Promise<void> {
    const db = await openDatabase();
    return new Promise((resolve, reject) => {
      const tx = db.transaction('bookings', 'readwrite');
      const store = tx.objectStore('bookings');
      const request = store.put(booking);
      request.onsuccess = () => resolve();
      request.onerror = () => reject(request.error);
    });
  }

  async getAllBookings(): Promise<OfflineBooking[]> {
    const db = await openDatabase();
    return new Promise((resolve, reject) => {
      const tx = db.transaction('bookings', 'readonly');
      const store = tx.objectStore('bookings');
      const request = store.getAll();
      request.onsuccess = () => resolve(request.result || []);
      request.onerror = () => reject(request.error);
    });
  }

  async deleteBooking(id: string): Promise<void> {
    const db = await openDatabase();
    return new Promise((resolve, reject) => {
      const tx = db.transaction('bookings', 'readwrite');
      const store = tx.objectStore('bookings');
      const request = store.delete(id);
      request.onsuccess = () => resolve();
      request.onerror = () => reject(request.error);
    });
  }
}
