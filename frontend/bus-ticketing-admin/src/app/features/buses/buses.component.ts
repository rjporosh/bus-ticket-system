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
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog, MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { BusesService } from '../../core/services/feature-services';
import { ToastService } from '../../core/services/toast.service';
import { BusDto, SeatClassLabel, SeatDto, SeatLayoutDto } from '../../core/models/api-models';

@Component({
  selector: 'app-buses',
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
    MatTooltipModule,
    ReactiveFormsModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page-container">
      <div class="page-header">
        <div>
          <h1>Fleet</h1>
          <p class="mono subtitle">{{ totalCount() }} bus(es)</p>
        </div>
        <button mat-flat-button color="primary" (click)="openCreateForm()">
          <mat-icon>add</mat-icon>
          Add Bus
        </button>
      </div>

      <mat-card class="card-surface">
        <div class="toolbar">
          <mat-form-field appearance="outline" class="search-field">
            <mat-label>Search</mat-label>
            <input matInput [formControl]="search" placeholder="Number or registration" />
            <mat-icon matSuffix>search</mat-icon>
          </mat-form-field>
        </div>

        <table mat-table [dataSource]="buses()">
          <ng-container matColumnDef="number">
            <th mat-header-cell *matHeaderCellDef>Bus</th>
            <td mat-cell *matCellDef="let b"><span class="mono">{{ b.number }}</span></td>
          </ng-container>
          <ng-container matColumnDef="registration">
            <th mat-header-cell *matHeaderCellDef>Registration</th>
            <td mat-cell *matCellDef="let b"><span class="mono">{{ b.registrationNumber }}</span></td>
          </ng-container>
          <ng-container matColumnDef="operator">
            <th mat-header-cell *matHeaderCellDef>Operator</th>
            <td mat-cell *matCellDef="let b">{{ b.operatorName }}</td>
          </ng-container>
          <ng-container matColumnDef="seats">
            <th mat-header-cell *matHeaderCellDef>Layout</th>
            <td mat-cell *matCellDef="let b">{{ b.totalSeats }} seats ({{ b.seatLayoutRows }}×{{ b.seatLayoutColumns }})</td>
          </ng-container>
          <ng-container matColumnDef="status">
            <th mat-header-cell *matHeaderCellDef>Active</th>
            <td mat-cell *matCellDef="let b">
              <mat-slide-toggle [checked]="b.isActive" (change)="toggleActive(b)" />
            </td>
          </ng-container>
          <ng-container matColumnDef="actions">
            <th mat-header-cell *matHeaderCellDef></th>
            <td mat-cell *matCellDef="let b">
              <button mat-icon-button (click)="viewSeatMap(b)" matTooltip="View seat map" aria-label="View seat map">
                <mat-icon>event_seat</mat-icon>
              </button>
              <button mat-icon-button (click)="openEditForm(b)" aria-label="Edit"><mat-icon>edit</mat-icon></button>
            </td>
          </ng-container>

          <tr mat-header-row *matHeaderRowDef="columns"></tr>
          <tr mat-row *matRowDef="let row; columns: columns"></tr>
        </table>

        @if (buses().length === 0) {
          <p class="empty-state">No buses found.</p>
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
export class BusesComponent implements OnInit {
  private readonly busesService = inject(BusesService);
  private readonly toast = inject(ToastService);
  private readonly dialog = inject(MatDialog);
  private readonly fb = inject(FormBuilder);

  protected readonly search = this.fb.nonNullable.control('');
  protected readonly buses = signal<BusDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly pageIndex = signal(0);
  protected readonly pageSize = signal(20);
  protected readonly columns = ['number', 'registration', 'operator', 'seats', 'status', 'actions'];

  ngOnInit(): void {
    this.load();
    this.search.valueChanges.subscribe(() => {
      this.pageIndex.set(0);
      this.load();
    });
  }

  private load(): void {
    this.busesService
      .list({ search: this.search.value, pageNumber: this.pageIndex() + 1, pageSize: this.pageSize() })
      .subscribe((result) => {
        this.buses.set(result.items);
        this.totalCount.set(result.totalCount);
      });
  }

  onPage(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.load();
  }

  toggleActive(bus: BusDto): void {
    this.busesService.setActive(bus.id, !bus.isActive).subscribe(() => {
      this.toast.success(`${bus.number} ${!bus.isActive ? 'activated' : 'deactivated'}.`);
      this.load();
    });
  }

  openCreateForm(): void {
    const ref = this.dialog.open(BusFormDialogComponent, { width: '440px', data: null });
    ref.afterClosed().subscribe((saved) => {
      if (saved) {
        this.toast.success('Bus created with seat layout generated.');
        this.load();
      }
    });
  }

  openEditForm(bus: BusDto): void {
    const ref = this.dialog.open(BusFormDialogComponent, { width: '440px', data: bus });
    ref.afterClosed().subscribe((saved) => {
      if (saved) {
        this.toast.success('Bus updated.');
        this.load();
      }
    });
  }

  viewSeatMap(bus: BusDto): void {
    this.dialog.open(SeatMapDialogComponent, { width: '480px', data: bus });
  }
}

@Component({
  selector: 'app-bus-form-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>{{ data ? 'Edit Bus' : 'Add Bus' }}</h2>
    <form [formGroup]="form" (ngSubmit)="save()">
      <mat-dialog-content>
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Bus Number</mat-label>
          <input matInput formControlName="number" placeholder="e.g. Bus-7" />
        </mat-form-field>
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Operator Name</mat-label>
          <input matInput formControlName="operatorName" />
        </mat-form-field>

        @if (!data) {
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Registration Number</mat-label>
            <input matInput formControlName="registrationNumber" />
          </mat-form-field>
          <div class="grid-2">
            <mat-form-field appearance="outline">
              <mat-label>Rows</mat-label>
              <input matInput type="number" formControlName="rows" />
            </mat-form-field>
            <mat-form-field appearance="outline">
              <mat-label>Columns</mat-label>
              <input matInput type="number" formControlName="columns" />
            </mat-form-field>
          </div>
          <p class="hint mono">Layout will generate {{ seatCount() }} seats (A1..{{ lastSeatLabel() }}).</p>
        }
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
      .hint { color: var(--color-text-muted); font-size: 0.8rem; margin-top: -8px; }
    `,
  ],
})
export class BusFormDialogComponent {
  protected readonly data = inject<BusDto | null>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<BusFormDialogComponent>);
  private readonly fb = inject(FormBuilder);
  private readonly busesService = inject(BusesService);

  protected readonly saving = signal(false);
  protected readonly form = this.fb.nonNullable.group({
    number: [this.data?.number ?? '', Validators.required],
    operatorName: [this.data?.operatorName ?? '', Validators.required],
    registrationNumber: ['', this.data ? [] : Validators.required],
    rows: [6, [Validators.required, Validators.min(1), Validators.max(26)]],
    columns: [4, [Validators.required, Validators.min(1), Validators.max(10)]],
  });

  seatCount(): number {
    return (this.form.controls.rows.value || 0) * (this.form.controls.columns.value || 0);
  }

  lastSeatLabel(): string {
    const rows = this.form.controls.rows.value || 1;
    const cols = this.form.controls.columns.value || 1;
    const rowLetter = String.fromCharCode('A'.charCodeAt(0) + Math.max(0, rows - 1));
    return `${rowLetter}${cols}`;
  }

  save(): void {
    if (this.form.invalid) return;
    this.saving.set(true);
    const value = this.form.getRawValue();

    const request$ = this.data
      ? this.busesService.update(this.data.id, { number: value.number, operatorName: value.operatorName })
      : this.busesService.create({
          number: value.number,
          operatorName: value.operatorName,
          registrationNumber: value.registrationNumber,
          rows: value.rows,
          columns: value.columns,
          defaultSeatClass: 0,
        });

    request$.subscribe({
      next: () => this.dialogRef.close(true),
      error: () => this.saving.set(false),
    });
  }
}

@Component({
  selector: 'app-seat-map-dialog',
  standalone: true,
  imports: [MatDialogModule, MatButtonModule, MatIconModule, MatTooltipModule],
  template: `
    <h2 mat-dialog-title>{{ data.number }} — Seat Map</h2>
    <mat-dialog-content>
      @if (layout(); as l) {
        <div class="legend">
          <span class="board-chip board-chip--available">Active</span>
          <span class="board-chip board-chip--muted">Out of Service</span>
        </div>

        <div class="seat-grid" [style.gridTemplateColumns]="gridColumns(l.columns)">
          @for (row of rowLabels(l); track row) {
            @for (col of columnNumbers(l.columns); track col) {
              @if (seatAt(l, row, col); as seat) {
                <button
                  type="button"
                  class="seat"
                  [class.seat--inactive]="!seat.isActive"
                  [matTooltip]="seatClassLabel(seat)"
                  (click)="toggleSeat(seat)"
                >
                  {{ seat.seatNumber }}
                </button>
              }
              @if (isAisle(l.columns, col)) {
                <div class="aisle"></div>
              }
            }
          }
        </div>
      }
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Close</button>
    </mat-dialog-actions>
  `,
  styles: [
    `
      .legend {
        display: flex;
        gap: 8px;
        margin-bottom: 16px;
      }
      .seat-grid {
        display: grid;
        gap: 8px;
        justify-content: center;
      }
      .seat {
        font-family: var(--font-mono);
        font-size: 0.75rem;
        font-weight: 600;
        width: 44px;
        height: 40px;
        border-radius: var(--radius-sm);
        border: 1px solid var(--color-available);
        background: var(--color-available-bg);
        color: var(--color-available);
        cursor: pointer;
      }
      .seat--inactive {
        border-color: var(--color-outofservice);
        background: var(--color-outofservice-bg);
        color: var(--color-outofservice);
      }
      .aisle {
        width: 20px;
      }
    `,
  ],
})
export class SeatMapDialogComponent implements OnInit {
  protected readonly data = inject<BusDto>(MAT_DIALOG_DATA);
  private readonly busesService = inject(BusesService);
  private readonly toast = inject(ToastService);

  protected readonly layout = signal<SeatLayoutDto | null>(null);

  ngOnInit(): void {
    this.busesService.getSeatLayout(this.data.id).subscribe((layout) => this.layout.set(layout));
  }

  rowLabels(layout: SeatLayoutDto): string[] {
    return [...new Set(layout.seats.map((s) => s.rowLabel))].sort();
  }

  columnNumbers(columns: number): number[] {
    return Array.from({ length: columns }, (_, i) => i + 1);
  }

  seatAt(layout: SeatLayoutDto, row: string, col: number): SeatDto | undefined {
    return layout.seats.find((s) => s.rowLabel === row && s.columnNumber === col);
  }

  // Visual center-aisle gap, like a real 2+2 coach layout, placed after the second column.
  isAisle(totalColumns: number, col: number): boolean {
    return totalColumns === 4 && col === 2;
  }

  gridColumns(columns: number): string {
    return `repeat(${columns}, 44px)`;
  }

  seatClassLabel(seat: SeatDto): string {
    return `${SeatClassLabel[seat.class]} · ${seat.isActive ? 'In service' : 'Out of service'}`;
  }

  toggleSeat(seat: SeatDto): void {
    this.busesService.setSeatStatus(this.data.id, seat.id, !seat.isActive).subscribe(() => {
      this.toast.success(`Seat ${seat.seatNumber} marked ${!seat.isActive ? 'in service' : 'out of service'}.`);
      this.busesService.getSeatLayout(this.data.id).subscribe((layout) => this.layout.set(layout));
    });
  }
}
