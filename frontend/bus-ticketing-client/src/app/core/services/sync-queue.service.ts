import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, throwError } from 'rxjs';
import { catchError, map, switchMap } from 'rxjs/operators';
import { OfflineDbService, OfflineBooking } from './offline-db.service';

export interface SyncQueueItem {
  id: string;
  type: 'booking' | 'payment' | 'cancel';
  payload: any;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class SyncQueueService {
  private readonly _pendingCount = signal(0);
  private readonly _lastSync = signal<string | null>(null);

  readonly pendingCount = this._pendingCount.asReadonly();
  readonly lastSync = this._lastSync.asReadonly();

  constructor(
    private readonly http: HttpClient,
    private readonly offlineDb: OfflineDbService
  ) {}

  enqueue(item: Omit<SyncQueueItem, 'id' | 'createdAt'>): void {
    const queueItem: SyncQueueItem = {
      ...item,
      id: crypto.randomUUID(),
      createdAt: new Date().toISOString()
    };
    this.offlineDb.saveBooking({ ...queueItem.payload, id: queueItem.id, createdAt: queueItem.createdAt });
    this._pendingCount.update(n => n + 1);
  }

  processQueue(): Observable<void> {
    return this.offlineDb.getAllBookings().pipe(
      switchMap(async (bookings) => {
        for (const booking of bookings) {
          try {
            await this.http.post('/api/v1/booking/sync', booking).toPromise();
            await this.offlineDb.deleteBooking(booking.id);
            this._pendingCount.update(n => Math.max(0, n - 1));
          } catch {
            // keep in queue for next sync
          }
        }
        this._lastSync.set(new Date().toISOString());
      }),
      map(() => undefined),
      catchError(() => of(undefined))
    );
  }

  getPendingItems(): Observable<OfflineBooking[]> {
    return this.offlineDb.getAllBookings();
  }
}
