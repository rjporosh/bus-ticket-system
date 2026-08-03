import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatDialog, MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { RoutesService, StationsService } from '../../core/services/feature-services';
import { ToastService } from '../../core/services/toast.service';
import { RouteDto, StationDto } from '../../core/models/api-models';

@Component({
  selector: 'app-routes-mgmt',
  standalone: true,
  imports: [
    MatCardModule,
    MatTableModule,
    MatPaginatorModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatSlideToggleModule,
    ReactiveFormsModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page-container">
      <div class="page-header">
        <div>
          <h1>Routes</h1>
          <p class="mono subtitle">{{ totalCount() }} route(s)</p>
        </div>
        <button mat-flat-button color="primary" (click)="openForm()">
          <mat-icon>add</mat-icon>
          Add Route
        </button>
      </div>

      <mat-card class="card-surface">
        <div class="toolbar">
          <mat-form-field appearance="outline" class="search-field">
            <mat-label>Search</mat-label>
            <input matInput [formControl]="search" placeholder="Route name" />
            <mat-icon matSuffix>search</mat-icon>
          </mat-form-field>
        </div>

        <table mat-table [dataSource]="routes()">
          <ng-container matColumnDef="name">
            <th mat-header-cell *matHeaderCellDef>Name</th>
            <td mat-cell *matCellDef="let r">{{ r.name }}</td>
          </ng-container>
          <ng-container matColumnDef="path">
            <th mat-header-cell *matHeaderCellDef>Path</th>
            <td mat-cell *matCellDef="let r">{{ r.originStationName }} → {{ r.destinationStationName }}</td>
          </ng-container>
          <ng-container matColumnDef="distance">
            <th mat-header-cell *matHeaderCellDef>Distance</th>
            <td mat-cell *matCellDef="let r"><span class="mono">{{ r.distanceKm }} km</span></td>
          </ng-container>
          <ng-container matColumnDef="duration">
            <th mat-header-cell *matHeaderCellDef>Duration</th>
            <td mat-cell *matCellDef="let r"><span class="mono">{{ formatDuration(r.estimatedDurationMinutes) }}</span></td>
          </ng-container>
          <ng-container matColumnDef="status">
            <th mat-header-cell *matHeaderCellDef>Active</th>
            <td mat-cell *matCellDef="let r">
              <mat-slide-toggle [checked]="r.isActive" (change)="toggleActive(r)" />
            </td>
          </ng-container>
          <ng-container matColumnDef="actions">
            <th mat-header-cell *matHeaderCellDef></th>
            <td mat-cell *matCellDef="let r">
              <button mat-icon-button (click)="openForm(r)" aria-label="Edit"><mat-icon>edit</mat-icon></button>
            </td>
          </ng-container>

          <tr mat-header-row *matHeaderRowDef="columns"></tr>
          <tr mat-row *matRowDef="let row; columns: columns"></tr>
        </table>

        @if (routes().length === 0) {
          <p class="empty-state">No routes found.</p>
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
      .toolbar { padding: 16px 16px 0; }
      .search-field { width: 320px; }
      table { width: 100%; }
      .empty-state { text-align: center; color: var(--color-text-muted); padding: 24px; }
    `,
  ],
})
export class RoutesManagementComponent implements OnInit {
  private readonly routesService = inject(RoutesService);
  private readonly toast = inject(ToastService);
  private readonly dialog = inject(MatDialog);
  private readonly fb = inject(FormBuilder);

  protected readonly search = this.fb.nonNullable.control('');
  protected readonly routes = signal<RouteDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly pageIndex = signal(0);
  protected readonly pageSize = signal(20);
  protected readonly columns = ['name', 'path', 'distance', 'duration', 'status', 'actions'];

  ngOnInit(): void {
    this.load();
    this.search.valueChanges.subscribe(() => {
      this.pageIndex.set(0);
      this.load();
    });
  }

  private load(): void {
    this.routesService
      .list({ search: this.search.value, pageNumber: this.pageIndex() + 1, pageSize: this.pageSize() })
      .subscribe((result) => {
        this.routes.set(result.items);
        this.totalCount.set(result.totalCount);
      });
  }

  onPage(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.load();
  }

  formatDuration(minutes: number): string {
    const h = Math.floor(minutes / 60);
    const m = minutes % 60;
    return `${h}h ${m}m`;
  }

  toggleActive(route: RouteDto): void {
    this.routesService.setActive(route.id, !route.isActive).subscribe(() => {
      this.toast.success(`${route.name} ${!route.isActive ? 'activated' : 'deactivated'}.`);
      this.load();
    });
  }

  openForm(route?: RouteDto): void {
    const ref = this.dialog.open(RouteFormDialogComponent, { width: '460px', data: route ?? null });
    ref.afterClosed().subscribe((saved) => {
      if (saved) {
        this.toast.success(route ? 'Route updated.' : 'Route created.');
        this.load();
      }
    });
  }
}

@Component({
  selector: 'app-route-form-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>{{ data ? 'Edit Route' : 'Add Route' }}</h2>
    <form [formGroup]="form" (ngSubmit)="save()">
      <mat-dialog-content>
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Route Name</mat-label>
          <input matInput formControlName="name" placeholder="e.g. Dhaka -> Chittagong" />
        </mat-form-field>

        @if (!data) {
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Origin Station</mat-label>
            <mat-select formControlName="originStationId">
              @for (s of stations(); track s.id) {
                <mat-option [value]="s.id">{{ s.name }} ({{ s.city }})</mat-option>
              }
            </mat-select>
          </mat-form-field>
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Destination Station</mat-label>
            <mat-select formControlName="destinationStationId">
              @for (s of stations(); track s.id) {
                <mat-option [value]="s.id">{{ s.name }} ({{ s.city }})</mat-option>
              }
            </mat-select>
          </mat-form-field>
        }

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Distance (km)</mat-label>
          <input matInput type="number" formControlName="distanceKm" />
        </mat-form-field>
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Estimated Duration (minutes)</mat-label>
          <input matInput type="number" formControlName="estimatedDurationMinutes" />
        </mat-form-field>
      </mat-dialog-content>
      <mat-dialog-actions align="end">
        <button mat-button type="button" mat-dialog-close>Cancel</button>
        <button mat-flat-button color="primary" type="submit" [disabled]="form.invalid || saving()">Save</button>
      </mat-dialog-actions>
    </form>
  `,
  styles: [`.full-width { width: 100%; }`],
})
export class RouteFormDialogComponent implements OnInit {
  protected readonly data = inject<RouteDto | null>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<RouteFormDialogComponent>);
  private readonly fb = inject(FormBuilder);
  private readonly routesService = inject(RoutesService);
  private readonly stationsService = inject(StationsService);

  protected readonly saving = signal(false);
  protected readonly stations = signal<StationDto[]>([]);

  protected readonly form = this.fb.nonNullable.group({
    name: [this.data?.name ?? '', Validators.required],
    originStationId: [this.data?.originStationId ?? '', Validators.required],
    destinationStationId: [this.data?.destinationStationId ?? '', Validators.required],
    distanceKm: [this.data?.distanceKm ?? 0, [Validators.required, Validators.min(0.1)]],
    estimatedDurationMinutes: [this.data?.estimatedDurationMinutes ?? 0, [Validators.required, Validators.min(1)]],
  });

  ngOnInit(): void {
    this.stationsService.list({ isActive: true, pageSize: 200 }).subscribe((result) => this.stations.set(result.items));
  }

  save(): void {
    if (this.form.invalid) return;
    this.saving.set(true);
    const value = this.form.getRawValue();

    const request$ = this.data
      ? this.routesService.update(this.data.id, {
          name: value.name,
          distanceKm: value.distanceKm,
          estimatedDurationMinutes: value.estimatedDurationMinutes,
        })
      : this.routesService.create(value);

    request$.subscribe({
      next: () => this.dialogRef.close(true),
      error: () => this.saving.set(false),
    });
  }
}
