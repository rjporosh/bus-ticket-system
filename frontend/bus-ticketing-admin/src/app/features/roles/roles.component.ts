import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog, MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { RolesService } from '../../core/services/feature-services';
import { ToastService } from '../../core/services/toast.service';
import { RoleDto } from '../../core/models/api-models';

@Component({
  selector: 'app-roles',
  standalone: true,
  imports: [MatCardModule, MatTableModule, MatButtonModule, MatIconModule, MatTooltipModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page-container">
      <div class="page-header">
        <div>
          <h1>Roles</h1>
          <p class="mono subtitle">{{ roles().length }} role(s)</p>
        </div>
        <button mat-flat-button color="primary" (click)="openCreateForm()">
          <mat-icon>add</mat-icon>
          Add Role
        </button>
      </div>

      <mat-card class="card-surface">
        <table mat-table [dataSource]="roles()">
          <ng-container matColumnDef="name">
            <th mat-header-cell *matHeaderCellDef>Name</th>
            <td mat-cell *matCellDef="let r">
              {{ r.name }}
              @if (r.isSystemRole) {
                <mat-icon class="lock-icon" matTooltip="System role - cannot be modified" inline>lock</mat-icon>
              }
            </td>
          </ng-container>
          <ng-container matColumnDef="description">
            <th mat-header-cell *matHeaderCellDef>Description</th>
            <td mat-cell *matCellDef="let r">{{ r.description || '—' }}</td>
          </ng-container>
          <ng-container matColumnDef="actions">
            <th mat-header-cell *matHeaderCellDef></th>
            <td mat-cell *matCellDef="let r">
              <button mat-icon-button [disabled]="r.isSystemRole" (click)="openEditForm(r)" aria-label="Edit">
                <mat-icon>edit</mat-icon>
              </button>
            </td>
          </ng-container>

          <tr mat-header-row *matHeaderRowDef="columns"></tr>
          <tr mat-row *matRowDef="let row; columns: columns"></tr>
        </table>
      </mat-card>
    </div>
  `,
  styles: [
    `
      .page-header { display: flex; align-items: flex-start; justify-content: space-between; margin-bottom: 16px; }
      .subtitle { color: var(--color-text-muted); }
      table { width: 100%; }
      .lock-icon { font-size: 16px; width: 16px; height: 16px; vertical-align: middle; margin-left: 4px; color: var(--color-text-muted); }
    `,
  ],
})
export class RolesComponent implements OnInit {
  private readonly rolesService = inject(RolesService);
  private readonly toast = inject(ToastService);
  private readonly dialog = inject(MatDialog);

  protected readonly roles = signal<RoleDto[]>([]);
  protected readonly columns = ['name', 'description', 'actions'];

  ngOnInit(): void {
    this.load();
  }

  private load(): void {
    this.rolesService.list().subscribe((roles) => this.roles.set(roles));
  }

  openCreateForm(): void {
    const ref = this.dialog.open(RoleFormDialogComponent, { width: '400px', data: null });
    ref.afterClosed().subscribe((saved) => {
      if (saved) {
        this.toast.success('Role created.');
        this.load();
      }
    });
  }

  openEditForm(role: RoleDto): void {
    const ref = this.dialog.open(RoleFormDialogComponent, { width: '400px', data: role });
    ref.afterClosed().subscribe((saved) => {
      if (saved) {
        this.toast.success('Role updated.');
        this.load();
      }
    });
  }
}

@Component({
  selector: 'app-role-form-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>{{ data ? 'Edit Role' : 'Add Role' }}</h2>
    <form [formGroup]="form" (ngSubmit)="save()">
      <mat-dialog-content>
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Name</mat-label>
          <input matInput formControlName="name" />
        </mat-form-field>
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Description (optional)</mat-label>
          <input matInput formControlName="description" />
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
export class RoleFormDialogComponent {
  protected readonly data = inject<RoleDto | null>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<RoleFormDialogComponent>);
  private readonly fb = inject(FormBuilder);
  private readonly rolesService = inject(RolesService);

  protected readonly saving = signal(false);
  protected readonly form = this.fb.nonNullable.group({
    name: [this.data?.name ?? '', Validators.required],
    description: [this.data?.description ?? ''],
  });

  save(): void {
    if (this.form.invalid) return;
    this.saving.set(true);
    const value = this.form.getRawValue();

    const request$ = this.data
      ? this.rolesService.update(this.data.id, value)
      : this.rolesService.create(value);

    request$.subscribe({
      next: () => this.dialogRef.close(true),
      error: () => this.saving.set(false),
    });
  }
}
