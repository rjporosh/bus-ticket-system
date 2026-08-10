import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { AppComponent } from './app/app.component';

if ('serviceWorker' in navigator) {
  navigator.serviceWorker.register('ngsw-worker.js').catch(() => {
    // Service worker registration failed
  });
}

bootstrapApplication(AppComponent, appConfig).catch((err) => console.error(err));