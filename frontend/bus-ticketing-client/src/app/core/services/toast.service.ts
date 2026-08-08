import { Injectable } from '@angular/core';
import { MatSnackBar, MatSnackBarConfig } from '@angular/material/snack-bar';

@Injectable({ providedIn: 'root' })
export class ToastService {
  constructor(private readonly snackBar: MatSnackBar) {}

  success(message: string, duration = 3000): void {
    this.show(message, 'Close', {
      panelClass: ['toast-success'],
      duration,
      horizontalPosition: 'end',
      verticalPosition: 'top',
    });
  }

  error(message: string, duration = 5000): void {
    this.show(message, 'Close', {
      panelClass: ['toast-error'],
      duration,
      horizontalPosition: 'end',
      verticalPosition: 'top',
    });
  }

  info(message: string, duration = 3000): void {
    this.show(message, 'Close', {
      panelClass: ['toast-info'],
      duration,
      horizontalPosition: 'end',
      verticalPosition: 'top',
    });
  }

  private show(message: string, action: string, config: MatSnackBarConfig): void {
    this.snackBar.open(message, action, config);
  }
}
