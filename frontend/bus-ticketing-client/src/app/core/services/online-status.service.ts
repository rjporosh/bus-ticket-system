import { Injectable, signal, effect } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, fromEvent, merge, of } from 'rxjs';
import { map, startWith, shareReplay } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class OnlineStatusService {
  private readonly _isOnline = signal<boolean>(typeof navigator !== 'undefined' ? navigator.onLine : true);
  private readonly _wasOffline = signal<boolean>(false);

  readonly isOnline = this._isOnline.asReadonly();
  readonly wasOffline = this._wasOffline.asReadonly();

  constructor() {
    if (typeof window !== 'undefined') {
      const online$ = fromEvent(window, 'online');
      const offline$ = fromEvent(window, 'offline');

      merge(online$, offline$).subscribe(() => {
        const online = navigator.onLine;
        this._isOnline.set(online);
        if (!online) {
          this._wasOffline.set(true);
        }
      });
    }
  }

  getStatusChanges(): Observable<boolean> {
    if (typeof window === 'undefined') {
      return of(true);
    }
    return merge(
      fromEvent(window, 'online').pipe(map(() => true)),
      fromEvent(window, 'offline').pipe(map(() => false))
    ).pipe(startWith(navigator.onLine), shareReplay(1));
  }
}
