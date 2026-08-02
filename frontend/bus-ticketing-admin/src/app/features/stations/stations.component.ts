import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatDialog, MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { StationsService } from '../../core/services/feature-services';
import { ToastService } from '../../core/services/toast.service';
import { StationDto } from '../../core/models/api-models';

@Component({
  selector: 'app-stations',
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
          <h1>Stations</h1>
          <p class="mono subtitle">{{ totalCount() }} station(s)</p>
        </div>
        <button mat-flat-button color="primary" (click)="openForm()">
          <mat-icon>add</mat-icon>
          Add Station
        </button>
      </div>

      <mat-card class="card-surface">
        <div class="toolbar">
          <mat-form-field appearance="outline" class="search-field">
            <mat-label>Search</mat-label>
            <input matInput [formControl]="search" placeholder="Name or city" />
            <mat-icon matSuffix>search</mat-icon>
          </mat-form-field>
        </div>

        <table mat-table [dataSource]="stations()">
          <ng-container matColumnDef="name">
            <th mat-header-cell *matHeaderCellDef>Name</th>
            <td mat-cell *matCellDef="let s">{{ s.name }}</td>
          </ng-container>
          <ng-container matColumnDef="city">
            <th mat-header-cell *matHeaderCellDef>City</th>
            <td mat-cell *matCellDef="let s">{{ s.city }}</td>
          </ng-container>
          <ng-container matColumnDef="address">
            <th mat-header-cell *matHeaderCellDef>Address</th>
            <td mat-cell *matCellDef="let s">{{ s.address || '—' }}</td>
          </ng-container>
          <ng-container matColumnDef="status">
            <th mat-header-cell *matHeaderCellDef>Active</th>
            <td mat-cell *matCellDef="let s">
              <mat-slide-toggle [checked]="s.isActive" (change)="toggleActive(s)" />
            </td>
          </ng-container>
          <ng-container matColumnDef="actions">
            <th mat-header-cell *matHeaderCellDef></th>
            <td mat-cell *matCellDef="let s">
              <button mat-icon-button (click)="openForm(s)" aria-label="Edit"><mat-icon>edit</mat-icon></button>
            </td>
          </ng-container>

          <tr mat-header-row *matHeaderRowDef="columns"></tr>
          <tr mat-row *matRowDef="let row; columns: columns"></tr>
        </table>

        @if (stations().length === 0) {
          <p class="empty-state">No stations found.</p>
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
      .page-header {
        display: flex;
        align-items: flex-start;
        justify-content: space-between;
        margin-bottom: 16px;
      }
      .subtitle {
        color: var(--color-text-muted);
      }
      .toolbar {
        padding: 16px 16px 0;
      }
      .search-field {
        width: 320px;
      }
      table {
        width: 100%;
      }
      .empty-state {
        text-align: center;
        color: var(--color-text-muted);
        padding: 24px;
      }
    `,
  ],
})
export class StationsComponent implements OnInit {
  private readonly stationsService = inject(StationsService);
  private readonly toast = inject(ToastService);
  private readonly dialog = inject(MatDialog);
  private readonly fb = inject(FormBuilder);

  protected readonly search = this.fb.nonNullable.control('');
  protected readonly stations = signal<StationDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly pageIndex = signal(0);
  protected readonly pageSize = signal(20);
  protected readonly columns = ['name', 'city', 'address', 'status', 'actions'];

  ngOnInit(): void {
    this.load();
    this.search.valueChanges.subscribe(() => {
      this.pageIndex.set(0);
      this.load();
    });
  }

  private load(): void {
    this.stationsService
      .list({ search: this.search.value, pageNumber: this.pageIndex() + 1, pageSize: this.pageSize() })
      .subscribe((result) => {
        this.stations.set(result.items);
        this.totalCount.set(result.totalCount);
      });
  }

  onPage(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.load();
  }

  toggleActive(station: StationDto): void {
    this.stationsService.setActive(station.id, !station.isActive).subscribe(() => {
      this.toast.success(`${station.name} ${!station.isActive ? 'activated' : 'deactivated'}.`);
      this.load();
    });
  }

  openForm(station?: StationDto): void {
    const ref = this.dialog.open(StationFormDialogComponent, { width: '420px', data: station ?? null });
    ref.afterClosed().subscribe((saved) => {
      if (saved) {
        this.toast.success(station ? 'Station updated.' : 'Station created.');
        this.load();
      }
    });
  }
}

@Component({
  selector: 'app-station-form-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>{{ data ? 'Edit Station' : 'Add Station' }}</h2>
    <form [formGroup]="form" (ngSubmit)="save()">
      <mat-dialog-content>
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Name</mat-label>
          <input matInput formControlName="name" />
        </mat-form-field>
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>City</mat-label>
          <input matInput formControlName="city" />
        </mat-form-field>
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Address (optional)</mat-label>
          <input matInput formControlName="address" />
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
export class StationFormDialogComponent {
  protected readonly data = inject<StationDto | null>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<StationFormDialogComponent>);
  private readonly fb = inject(FormBuilder);
  private readonly stationsService = inject(StationsService);

  protected readonly saving = signal(false);
  protected readonly form = this.fb.nonNullable.group({
    name: [this.data?.name ?? '', Validators.required],
    city: [this.data?.city ?? '', Validators.required],
    address: [this.data?.address ?? ''],
  });

  save(): void {
    if (this.form.invalid) return;
    this.saving.set(true);
    const value = this.form.getRawValue();

    const request$ = this.data
      ? this.stationsService.update(this.data.id, value)
      : this.stationsService.create(value);

    request$.subscribe({
      next: () => this.dialogRef.close(true),
      error: () => this.saving.set(false),
    });
  }
}
