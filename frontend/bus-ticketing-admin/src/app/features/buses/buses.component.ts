import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, FormArray, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
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
import { MatCheckboxModule } from '@angular/material/checkbox';
import { BusesService } from '../../core/services/feature-services';
import { ToastService } from '../../core/services/toast.service';
import { BusDto, SeatClassLabel, SeatDto, SeatLayoutDto, LayoutType } from '../../core/models/api-models';

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
          <ng-container matColumnDef="layout">
            <th mat-header-cell *matHeaderCellDef>Type</th>
            <td mat-cell *matCellDef="let b">{{ b.seatLayoutType === 1 ? 'Real Bus' : 'Standard Grid' }}</td>
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
  protected readonly columns = ['number', 'registration', 'operator', 'seats', 'layout', 'status', 'actions'];

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
  imports: [ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatSelectModule, MatCheckboxModule],
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
              <input matInput type="number" formControlName="rows" (change)="onRowsChange()" />
            </mat-form-field>
            <mat-form-field appearance="outline">
              <mat-label>Columns</mat-label>
              <input matInput type="number" formControlName="columns" />
            </mat-form-field>
          </div>
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Layout Type</mat-label>
            <mat-select formControlName="layoutType">
              <mat-option [value]="0">Standard Grid</mat-option>
              <mat-option [value]="1">Real Bus</mat-option>
            </mat-select>
          </mat-form-field>
          @if (form.controls.layoutType.value === 1) {
            <div class="real-bus-config">
              <mat-checkbox formControlName="driverSeat" class="full-width">Driver Seat</mat-checkbox>
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>Aisle Gap</mat-label>
                <input matInput type="number" formControlName="aisleGap" min="0" max="3" />
              </mat-form-field>
              <p class="hint mono">Seats per row (Left | Right)</p>
              <div class="row-configs">
                @for (row of rowConfigs(); track row.label) {
                  <div class="row-config">
                    <span class="row-label">Row {{ row.label }}</span>
                    <input type="number" [value]="row.left" (input)="updateRowConfig($index, 'left', $any($event.target).value)" min="0" max="4" />
                    <span class="row-sep">|</span>
                    <input type="number" [value]="row.right" (input)="updateRowConfig($index, 'right', $any($event.target).value)" min="0" max="4" />
                  </div>
                }
              </div>
              <div class="last-row-config">
                <mat-checkbox [checked]="useLastRowOverride()" (change)="onLastRowOverrideChange()">Override last row seats</mat-checkbox>
                @if (useLastRowOverride()) {
                  <div class="row-config">
                    <span class="row-label">Last</span>
                    <input type="number" [value]="lastRowConfig()?.left ?? 2" (input)="updateLastRowConfig('left', $any($event.target).value)" min="0" max="4" />
                    <span class="row-sep">|</span>
                    <input type="number" [value]="lastRowConfig()?.right ?? 2" (input)="updateLastRowConfig('right', $any($event.target).value)" min="0" max="4" />
                  </div>
                }
              </div>
            </div>
          }
          @if (form.controls['layoutType'].value !== 1) {
            <p class="hint mono">Layout will generate {{ seatCount() }} seats (A1..{{ lastSeatLabel() }}).</p>
          }
          @if (form.controls['layoutType'].value === 1) {
            <p class="hint mono">Layout will generate {{ realBusSeatCount() }} seats with real bus arrangement.</p>
          }
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
      .real-bus-config { margin-top: 12px; padding: 12px; border: 1px solid var(--color-border); border-radius: var(--radius-sm); }
      .row-configs { display: flex; flex-direction: column; gap: 8px; margin-top: 8px; }
      .row-config { display: flex; align-items: center; gap: 8px; }
      .row-label { width: 50px; font-size: 0.85rem; color: var(--color-text-muted); }
      .row-config input { width: 50px; text-align: center; }
      .row-sep { color: var(--color-text-muted); }
      .last-row-config { margin-top: 8px; padding-top: 8px; border-top: 1px dashed var(--color-border); }
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
    layoutType: [this.data?.seatLayoutType ?? 0, Validators.required],
    driverSeat: [true],
    aisleGap: [1, [Validators.required, Validators.min(0), Validators.max(3)]],
  });

  protected readonly rowConfigs = signal<{ label: string; left: number; right: number }[]>([]);
  protected readonly lastRowConfig = signal<{ left: number; right: number } | null>(null);
  protected readonly useLastRowOverride = signal(false);

  constructor() {
    this.initRowConfigs();
  }

  initRowConfigs(): void {
    const rows = this.form.controls.rows.value || 6;
    const config = this.data?.seatLayoutConfig ? JSON.parse(this.data.seatLayoutConfig) : null;
    const rowSeats = config?.SeatsPerRow ?? [];
    this.rowConfigs.set(
      Array.from({ length: rows }, (_, i) => {
        const label = String.fromCharCode('A'.charCodeAt(0) + i);
        const rs = rowSeats[i];
        return { label, left: rs?.Left ?? 2, right: rs?.Right ?? 2 };
      })
    );
    const lastRow = config?.LastRowConfig;
    if (lastRow) {
      this.lastRowConfig.set({ left: lastRow.Left, right: lastRow.Right });
      this.useLastRowOverride.set(true);
    } else {
      this.lastRowConfig.set(null);
      this.useLastRowOverride.set(false);
    }
  }

  onRowsChange(): void {
    this.initRowConfigs();
  }

  updateRowConfig(index: number, field: 'left' | 'right', value: string): void {
    const num = Math.max(0, Math.min(4, parseInt(value) || 0));
    const current = this.rowConfigs();
    const updated = [...current];
    updated[index] = { ...updated[index], [field]: num };
    this.rowConfigs.set(updated);
  }

  onLastRowOverrideChange(): void {
    if (this.useLastRowOverride() && !this.lastRowConfig()) {
      this.lastRowConfig.set({ left: 2, right: 2 });
    }
    if (!this.useLastRowOverride()) {
      this.lastRowConfig.set(null);
    }
  }

  updateLastRowConfig(field: 'left' | 'right', value: string): void {
    const num = Math.max(0, Math.min(4, parseInt(value) || 0));
    const current = this.lastRowConfig() ?? { left: 2, right: 2 };
    this.lastRowConfig.set({ ...current, [field]: num });
  }

  seatCount(): number {
    return (this.form.controls.rows.value || 0) * (this.form.controls.columns.value || 0);
  }

  lastSeatLabel(): string {
    const rows = this.form.controls.rows.value || 1;
    const cols = this.form.controls.columns.value || 1;
    const rowLetter = String.fromCharCode('A'.charCodeAt(0) + Math.max(0, rows - 1));
    return `${rowLetter}${cols}`;
  }

  realBusSeatCount(): number {
    const rows = this.form.controls.rows.value || 0;
    const config = this.form.getRawValue();
    const useLastRow = this.useLastRowOverride() && this.lastRowConfig() != null;
    const lastRow = useLastRow ? this.lastRowConfig()! : null;
    let count = 0;
    const rowConfigs = this.rowConfigs();
    for (let i = 0; i < rows; i++) {
      let left = rowConfigs[i]?.left ?? 2;
      let right = rowConfigs[i]?.right ?? 2;
      if (i === rows - 1 && lastRow != null) {
        left = lastRow.left;
        right = lastRow.right;
      }
      count += left + right + (i === 0 && config.driverSeat ? 1 : 0);
    }
    return count;
  }

  buildLayoutConfigJson(): string | null {
    if (this.form.controls.layoutType.value !== 1) return null;
    const config: any = {
      DriverSeat: this.form.controls.driverSeat.value,
      AisleGap: this.form.controls.aisleGap.value,
      SeatsPerRow: this.rowConfigs().map(r => ({ Left: r.left, Right: r.right })),
    };
    if (this.useLastRowOverride() && this.lastRowConfig()) {
      config.LastRowConfig = { Left: this.lastRowConfig()!.left, Right: this.lastRowConfig()!.right };
    }
    return JSON.stringify(config);
  }

  save(): void {
    if (this.form.invalid) return;
    this.saving.set(true);
    const value = this.form.getRawValue();
    const layoutConfigJson = this.buildLayoutConfigJson();

    const request$ = this.data
      ? this.busesService.update(this.data.id, { number: value.number, operatorName: value.operatorName })
      : this.busesService.create({
          number: value.number,
          operatorName: value.operatorName,
          registrationNumber: value.registrationNumber,
          rows: value.rows,
          columns: value.columns,
          defaultSeatClass: 0,
          layoutType: value.layoutType,
          layoutConfigJson,
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
          @if (l.layoutType === 1) {
            <span class="board-chip driver-chip">Driver</span>
          }
        </div>

        @if (l.layoutType === 1) {
          <div class="bus-body">
            <div class="bus-front">
              <span class="bus-front-label">FRONT</span>
            </div>
            <div class="bus-interior">
              <div class="row-labels-grid" [style.gridTemplateRows]="realBusGridRows(l)">
                @for (item of getRealBusRowLabels(l); track item.visualRow) {
                  <div class="row-label" [style.gridRow]="item.visualRow">{{ item.label }}</div>
                }
              </div>
              <div class="real-bus-seat-grid" [style.gridTemplateColumns]="realBusGridColumns(l)" [style.gridTemplateRows]="realBusGridRows(l)">
                @for (seat of l.seats; track seat.id) {
                  <button
                    type="button"
                    class="seat"
                    [class.seat--inactive]="!seat.isActive"
                    [class.seat--driver]="seat.isDriver"
                    [matTooltip]="seatClassLabel(seat)"
                    (click)="toggleSeat(seat)"
                    [style.grid-row]="seat.visualRow"
                    [style.grid-column]="seat.visualCol"
                  >
                    @if (seat.isDriver) {
                      <span class="driver-icon">&#x1F69A;</span>
                    } @else {
                      {{ seat.seatNumber }}
                    }
                  </button>
                }
              </div>
            </div>
            <div class="bus-rear">
              <span class="bus-rear-label">REAR</span>
            </div>
          </div>
        } @else {
          <div class="bus-body">
            <div class="bus-front">
              <span class="bus-front-label">FRONT</span>
            </div>
            <div class="bus-interior standard-grid-interior">
              <div class="row-labels-grid" [style.gridTemplateRows]="gridRows(l.rows)">
                @for (row of rowLabels(l); track row) {
                  <div class="row-label">{{ row }}</div>
                }
              </div>
              <div class="seat-grid" [style.gridTemplateColumns]="gridColumns(l.columns)" [style.gridTemplateRows]="gridRows(l.rows)">
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
                      <div class="aisle-gap"></div>
                    }
                  }
                }
              </div>
            </div>
            <div class="bus-rear">
              <span class="bus-rear-label">REAR</span>
            </div>
          </div>
        }
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
      .bus-body {
        background: #f8f9fa;
        border: 2px solid #dee2e6;
        border-radius: 12px;
        padding: 16px;
        display: inline-flex;
        flex-direction: column;
        align-items: center;
        gap: 8px;
      }
      .bus-front {
        width: 100%;
        text-align: center;
        padding-bottom: 8px;
        border-bottom: 2px dashed #ced4da;
      }
      .bus-front-label {
        font-size: 0.7rem;
        font-weight: 700;
        color: #6c757d;
        letter-spacing: 0.15em;
      }
      .bus-rear {
        width: 100%;
        text-align: center;
        padding-top: 8px;
        border-top: 2px dashed #ced4da;
      }
      .bus-rear-label {
        font-size: 0.7rem;
        font-weight: 700;
        color: #6c757d;
        letter-spacing: 0.15em;
      }
      .bus-interior {
        display: flex;
        align-items: center;
        gap: 12px;
        padding: 8px 0;
      }
      .standard-grid-interior {
        flex-direction: column;
      }
      .row-labels-grid {
        display: grid;
        gap: 8px;
      }
      .row-label {
        width: 28px;
        height: 40px;
        display: flex;
        align-items: center;
        justify-content: center;
        font-family: var(--font-mono);
        font-size: 0.75rem;
        font-weight: 700;
        color: #495057;
      }
      .seat-grid {
        display: grid;
        gap: 8px;
        justify-content: center;
      }
      .real-bus-seat-grid {
        display: grid;
        gap: 6px;
        justify-content: center;
      }
      .seat {
        font-family: var(--font-mono);
        font-size: 0.7rem;
        font-weight: 600;
        width: 44px;
        height: 40px;
        border-radius: var(--radius-sm);
        border: 1px solid var(--color-available);
        background: var(--color-available-bg);
        color: var(--color-available);
        cursor: pointer;
        display: flex;
        align-items: center;
        justify-content: center;
      }
      .seat:hover:not(.seat--inactive):not(.seat--driver) {
        transform: scale(1.05);
        box-shadow: 0 2px 6px rgba(0,0,0,0.1);
      }
      .seat--inactive {
        border-color: var(--color-outofservice);
        background: var(--color-outofservice-bg);
        color: var(--color-outofservice);
      }
      .seat--driver {
        border-color: #ff9800;
        background: #fff3e0;
        color: #e65100;
        cursor: default;
      }
      .driver-icon {
        font-size: 1.1rem;
        line-height: 1;
      }
      .driver-chip {
        background: #fff3e0;
        color: #e65100;
        border-color: #ff9800;
      }
      .aisle-gap {
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

  getRealBusRowLabels(layout: SeatLayoutDto): { visualRow: number; label: string }[] {
    const map = new Map<number, string>();
    layout.seats.forEach((s) => {
      if (!s.isDriver && s.visualRow != null && !map.has(s.visualRow)) {
        map.set(s.visualRow, s.rowLabel);
      }
    });
    return [...map.entries()].sort((a, b) => a[0] - b[0]).map(([visualRow, label]) => ({ visualRow, label }));
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

  gridRows(rows: number): string {
    return `repeat(${rows}, 40px)`;
  }

  seatClassLabel(seat: SeatDto): string {
    return `${SeatClassLabel[seat.class]} · ${seat.isActive ? 'In service' : 'Out of service'}`;
  }

  realBusGridColumns(layout: SeatLayoutDto): string {
    if (layout.layoutType !== 1 || !layout.seats.length) return 'repeat(5, 44px)';
    const maxCol = Math.max(...layout.seats.map(s => s.visualCol ?? 1));
    return `repeat(${maxCol}, 44px)`;
  }

  realBusGridRows(layout: SeatLayoutDto): string {
    if (layout.layoutType !== 1 || !layout.seats.length) return 'repeat(1, 40px)';
    const maxRow = Math.max(...layout.seats.map(s => s.visualRow ?? 1));
    return `repeat(${maxRow}, 40px)`;
  }

  toggleSeat(seat: SeatDto): void {
    this.busesService.setSeatStatus(this.data.id, seat.id, !seat.isActive).subscribe(() => {
      this.toast.success(`Seat ${seat.seatNumber} marked ${!seat.isActive ? 'in service' : 'out of service'}.`);
      this.busesService.getSeatLayout(this.data.id).subscribe((layout) => this.layout.set(layout));
    });
  }
}
