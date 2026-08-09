import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class LoadingService {
  private readonly counter = signal(0);
  readonly loading = this.counter.asReadonly();

  start(): void {
    this.counter.update(v => v + 1);
  }

  stop(): void {
    this.counter.update(v => Math.max(0, v - 1));
  }
}
