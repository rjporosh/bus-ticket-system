import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatDialog, MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { BusesService, RoutesService, SchedulesService } from '../../core/services/feature-services';
import { ToastService } from '../../core/services/toast.service';
import {
  BusDto,
  DayOfWeekFlag,
  DayOfWeekOptions,
  RouteDto,
  ScheduleDto,
  ScheduleStatusLabel,
} from '../../core/models/api-models';

@Component({
  selector: 'app-schedules',
  standalone: true,
  imports: [MatCardModule, MatTableModule, MatPaginatorModule, MatButtonModule, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page-container">
      <div class="page-header">
        <div>
          <h1>Schedules</h1>
          <p class="mono subtitle">{{ totalCount() }} schedule(s)</p>
        </div>
        <button mat-flat-button color="primary" (click)="openCreateForm()">
          <mat-icon>add</mat-icon>
          Add Schedule
        </button>
      </div>

      <mat-card class="card-surface">
        <table mat-table [dataSource]="schedules()">
          <ng-container matColumnDef="bus">
            <th mat-header-cell *matHeaderCellDef>Bus</th>
            <td mat-cell *matCellDef="let s"><span class="mono">{{ s.busNumber }}</span></td>
          </ng-container>
          <ng-container matColumnDef="route">
            <th mat-header-cell *matHeaderCellDef>Route</th>
            <td mat-cell *matCellDef="let s">{{ s.routeName }}</td>
          </ng-container>
          <ng-container matColumnDef="time">
            <th mat-header-cell *matHeaderCellDef>Time</th>
            <td mat-cell *matCellDef="let s"><span class="mono">{{ s.departureTime.slice(0, 5) }} → {{ s.arrivalTime.slice(0, 5) }}</span></td>
          </ng-container>
          <ng-container matColumnDef="fare">
            <th mat-header-cell *matHeaderCellDef>Fare</th>
            <td mat-cell *matCellDef="let s">৳{{ s.fareAmount }}</td>
          </ng-container>
          <ng-container matColumnDef="status">
            <th mat-header-cell *matHeaderCellDef>Status</th>
            <td mat-cell *matCellDef="let s">
              <span class="board-chip" [class.board-chip--available]="s.status !== 3" [class.board-chip--sold]="s.status === 3">
                {{ statusLabel(s.status) }}
              </span>
            </td>
          </ng-container>
          <ng-container matColumnDef="actions">
            <th mat-header-cell *matHeaderCellDef></th>
            <td mat-cell *matCellDef="let s">
              <button mat-icon-button (click)="openEditForm(s)" aria-label="Edit"><mat-icon>edit</mat-icon></button>
              <button mat-icon-button (click)="toggleStatus(s)" [attr.aria-label]="s.status === 3 ? 'Reactivate' : 'Cancel'">
                <mat-icon>{{ s.status === 3 ? 'restart_alt' : 'block' }}</mat-icon>
              </button>
            </td>
          </ng-container>

          <tr mat-header-row *matHeaderRowDef="columns"></tr>
          <tr mat-row *matRowDef="let row; columns: columns"></tr>
        </table>

        @if (schedules().length === 0) {
          <p class="empty-state">No schedules found.</p>
        }

        <mat-paginator
          [length]="totalCount()"
          [pageSize]="pageSize()"
          [pageIndex]="pageIndex()"
          [pageSizeOptions]="[10, 20, 50]"
          (page)="onPage($event)"
        />
      </mat-card>
    </div>
  `,
  styles: [
    `
      .page-header { display: flex; align-items: flex-start; justify-content: space-between; margin-bottom: 16px; }
      .subtitle { color: var(--color-text-muted); }
      table { width: 100%; }
      .empty-state { text-align: center; color: var(--color-text-muted); padding: 24px; }
    `,
  ],
})
export class SchedulesComponent implements OnInit {
  private readonly schedulesService = inject(SchedulesService);
  private readonly toast = inject(ToastService);
  private readonly dialog = inject(MatDialog);

  protected readonly schedules = signal<ScheduleDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly pageIndex = signal(0);
  protected readonly pageSize = signal(20);
  protected readonly columns = ['bus', 'route', 'time', 'fare', 'status', 'actions'];

  ngOnInit(): void {
    this.load();
  }

  private load(): void {
    this.schedulesService.list({ pageNumber: this.pageIndex() + 1, pageSize: this.pageSize() }).subscribe((result) => {
      this.schedules.set(result.items);
      this.totalCount.set(result.totalCount);
    });
  }

  onPage(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.load();
  }

  statusLabel(status: ScheduleDto['status']): string {
    return ScheduleStatusLabel[status];
  }

  toggleStatus(schedule: ScheduleDto): void {
    const cancel = schedule.status !== 3;
    this.schedulesService.setStatus(schedule.id, cancel).subscribe(() => {
      this.toast.success(`Schedule ${cancel ? 'cancelled' : 'reactivated'}.`);
      this.load();
    });
  }

  openCreateForm(): void {
    const ref = this.dialog.open(ScheduleFormDialogComponent, { width: '480px', data: null });
    ref.afterClosed().subscribe((saved) => {
      if (saved) {
        this.toast.success('Schedule created.');
        this.load();
      }
    });
  }

  openEditForm(schedule: ScheduleDto): void {
    const ref = this.dialog.open(ScheduleFormDialogComponent, { width: '480px', data: schedule });
    ref.afterClosed().subscribe((saved) => {
      if (saved) {
        this.toast.success('Schedule updated.');
        this.load();
      }
    });
  }
}

@Component({
  selector: 'app-schedule-form-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    FormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatButtonToggleModule,
  ],
  template: `
    <h2 mat-dialog-title>{{ data ? 'Edit Schedule' : 'Add Schedule' }}</h2>
    <form [formGroup]="form" (ngSubmit)="save()">
      <mat-dialog-content>
        @if (!data) {
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Bus</mat-label>
            <mat-select formControlName="busId">
              @for (b of buses(); track b.id) {
                <mat-option [value]="b.id">{{ b.number }} ({{ b.totalSeats }} seats)</mat-option>
              }
            </mat-select>
          </mat-form-field>
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Route</mat-label>
            <mat-select formControlName="routeId">
              @for (r of routes(); track r.id) {
                <mat-option [value]="r.id">{{ r.name }}</mat-option>
              }
            </mat-select>
          </mat-form-field>
        }

        <div class="grid-2">
          <mat-form-field appearance="outline">
            <mat-label>Departure</mat-label>
            <input matInput type="time" formControlName="departureTime" />
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>Arrival</mat-label>
            <input matInput type="time" formControlName="arrivalTime" />
          </mat-form-field>
        </div>

        <label class="days-label mono">Runs on</label>
        <mat-button-toggle-group multiple [(ngModel)]="selectedDayValues" [ngModelOptions]="{ standalone: true }" class="days-group">
          @for (day of dayOptions; track day.value) {
            <mat-button-toggle [value]="day.value">{{ day.label }}</mat-button-toggle>
          }
        </mat-button-toggle-group>

        <div class="grid-2">
          <mat-form-field appearance="outline">
            <mat-label>Effective From</mat-label>
            <input matInput type="date" formControlName="effectiveFrom" />
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>Effective To (optional)</mat-label>
            <input matInput type="date" formControlName="effectiveTo" />
          </mat-form-field>
        </div>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Fare Amount (৳)</mat-label>
          <input matInput type="number" formControlName="fareAmount" />
        </mat-form-field>
      </mat-dialog-content>
      <mat-dialog-actions align="end">
        <button mat-button type="button" mat-dialog-close>Cancel</button>
        <button mat-flat-button color="primary" type="submit" [disabled]="form.invalid || saving()">Save</button>
      </mat-dialog-actions>
    </form>
  `,
  styles: [
    `
      .full-width { width: 100%; }
      .grid-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
      .days-label { display: block; font-size: 0.75rem; color: var(--color-text-muted); text-transform: uppercase; margin: 4px 0 8px; }
      .days-group { display: flex; flex-wrap: wrap; margin-bottom: 16px; }
    `,
  ],
})
export class ScheduleFormDialogComponent implements OnInit {
  protected readonly data = inject<ScheduleDto | null>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<ScheduleFormDialogComponent>);
  private readonly fb = inject(FormBuilder);
  private readonly schedulesService = inject(SchedulesService);
  private readonly busesService = inject(BusesService);
  private readonly routesService = inject(RoutesService);

  protected readonly saving = signal(false);
  protected readonly buses = signal<BusDto[]>([]);
  protected readonly routes = signal<RouteDto[]>([]);
  protected readonly dayOptions = DayOfWeekOptions;
  protected selectedDayValues: DayOfWeekFlag[] = this.data ? this.decomposeFlags(this.data.daysOfWeek) : [];

  protected readonly form = this.fb.nonNullable.group({
    busId: [this.data?.busId ?? '', this.data ? [] : Validators.required],
    routeId: [this.data?.routeId ?? '', this.data ? [] : Validators.required],
    departureTime: [this.data?.departureTime.slice(0, 5) ?? '07:00', Validators.required],
    arrivalTime: [this.data?.arrivalTime.slice(0, 5) ?? '13:00', Validators.required],
    effectiveFrom: [this.data?.effectiveFrom ?? new Date().toISOString().slice(0, 10), Validators.required],
    effectiveTo: [this.data?.effectiveTo ?? ''],
    fareAmount: [this.data?.fareAmount ?? 800, [Validators.required, Validators.min(1)]],
  });

  ngOnInit(): void {
    this.busesService.list({ isActive: true, pageSize: 200 }).subscribe((result) => this.buses.set(result.items));
    this.routesService.list({ isActive: true, pageSize: 200 }).subscribe((result) => this.routes.set(result.items));
  }

  private decomposeFlags(flags: DayOfWeekFlag): DayOfWeekFlag[] {
    return DayOfWeekOptions.map((o) => o.value).filter((v) => (flags & v) === v);
  }

  private composeFlags(): number {
    return this.selectedDayValues.reduce((acc, v) => acc | v, 0);
  }

  save(): void {
    if (this.form.invalid || this.selectedDayValues.length === 0) return;
    this.saving.set(true);
    const value = this.form.getRawValue();

    const payload = {
      departureTime: `${value.departureTime}:00`,
      arrivalTime: `${value.arrivalTime}:00`,
      daysOfWeek: this.composeFlags(),
      effectiveFrom: value.effectiveFrom,
      effectiveTo: value.effectiveTo || null,
      fareAmount: value.fareAmount,
    };

    const request$ = this.data
      ? this.schedulesService.update(this.data.id, payload)
      : this.schedulesService.create({ busId: value.busId, routeId: value.routeId, ...payload });

    request$.subscribe({
      next: () => this.dialogRef.close(true),
      error: () => this.saving.set(false),
    });
  }
}
