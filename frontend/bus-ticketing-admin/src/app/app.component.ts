import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { LoadingService } from './core/services/loading.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, MatProgressBarModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (loading.isLoading()) {
      <mat-progress-bar mode="indeterminate" class="global-progress-bar" />
    }
    <router-outlet />
  `,
  styles: [
    `
      .global-progress-bar {
        position: fixed;
        top: 0;
        left: 0;
        right: 0;
        z-index: 1000;
      }
    `,
  ],
})
export class AppComponent {
  protected readonly loading = inject(LoadingService);
}
