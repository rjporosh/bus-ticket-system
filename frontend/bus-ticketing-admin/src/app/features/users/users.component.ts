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
import { RolesService, UsersService } from '../../core/services/feature-services';
import { ToastService } from '../../core/services/toast.service';
import { RoleDto, UserDto } from '../../core/models/api-models';

@Component({
  selector: 'app-users',
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
          <h1>Users</h1>
          <p class="mono subtitle">{{ totalCount() }} user(s)</p>
        </div>
        <button mat-flat-button color="primary" (click)="openCreateForm()">
          <mat-icon>person_add</mat-icon>
          Add User
        </button>
      </div>

      <mat-card class="card-surface">
        <div class="toolbar">
          <mat-form-field appearance="outline" class="search-field">
            <mat-label>Search</mat-label>
            <input matInput [formControl]="search" placeholder="Username, email or name" />
            <mat-icon matSuffix>search</mat-icon>
          </mat-form-field>
        </div>

        <table mat-table [dataSource]="users()">
          <ng-container matColumnDef="username">
            <th mat-header-cell *matHeaderCellDef>Username</th>
            <td mat-cell *matCellDef="let u"><span class="mono">{{ u.username }}</span></td>
          </ng-container>
          <ng-container matColumnDef="fullName">
            <th mat-header-cell *matHeaderCellDef>Full Name</th>
            <td mat-cell *matCellDef="let u">{{ u.fullName }}</td>
          </ng-container>
          <ng-container matColumnDef="role">
            <th mat-header-cell *matHeaderCellDef>Role</th>
            <td mat-cell *matCellDef="let u">{{ u.roleName }}</td>
          </ng-container>
          <ng-container matColumnDef="booth">
            <th mat-header-cell *matHeaderCellDef>Booth</th>
            <td mat-cell *matCellDef="let u">{{ u.boothName || '—' }}</td>
          </ng-container>
          <ng-container matColumnDef="status">
            <th mat-header-cell *matHeaderCellDef>Active</th>
            <td mat-cell *matCellDef="let u">
              <mat-slide-toggle [checked]="u.isActive" (change)="toggleActive(u)" />
            </td>
          </ng-container>
          <ng-container matColumnDef="actions">
            <th mat-header-cell *matHeaderCellDef></th>
            <td mat-cell *matCellDef="let u">
              <button mat-icon-button (click)="openEditForm(u)" aria-label="Edit"><mat-icon>edit</mat-icon></button>
            </td>
          </ng-container>

          <tr mat-header-row *matHeaderRowDef="columns"></tr>
          <tr mat-row *matRowDef="let row; columns: columns"></tr>
        </table>

        @if (users().length === 0) {
          <p class="empty-state">No users found.</p>
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
export class UsersComponent implements OnInit {
  private readonly usersService = inject(UsersService);
  private readonly toast = inject(ToastService);
  private readonly dialog = inject(MatDialog);
  private readonly fb = inject(FormBuilder);

  protected readonly search = this.fb.nonNullable.control('');
  protected readonly users = signal<UserDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly pageIndex = signal(0);
  protected readonly pageSize = signal(20);
  protected readonly columns = ['username', 'fullName', 'role', 'booth', 'status', 'actions'];

  ngOnInit(): void {
    this.load();
    this.search.valueChanges.subscribe(() => {
      this.pageIndex.set(0);
      this.load();
    });
  }

  private load(): void {
    this.usersService
      .list({ search: this.search.value, pageNumber: this.pageIndex() + 1, pageSize: this.pageSize() })
      .subscribe((result) => {
        this.users.set(result.items);
        this.totalCount.set(result.totalCount);
      });
  }

  onPage(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.load();
  }

  toggleActive(user: UserDto): void {
    this.usersService.setActive(user.id, !user.isActive).subscribe(() => {
      this.toast.success(`${user.username} ${!user.isActive ? 'activated' : 'deactivated'}.`);
      this.load();
    });
  }

  openCreateForm(): void {
    const ref = this.dialog.open(UserFormDialogComponent, { width: '460px', data: null });
    ref.afterClosed().subscribe((saved) => {
      if (saved) {
        this.toast.success('User created.');
        this.load();
      }
    });
  }

  openEditForm(user: UserDto): void {
    const ref = this.dialog.open(UserFormDialogComponent, { width: '460px', data: user });
    ref.afterClosed().subscribe((saved) => {
      if (saved) {
        this.toast.success('User updated.');
        this.load();
      }
    });
  }
}

@Component({
  selector: 'app-user-form-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>{{ data ? 'Edit User' : 'Add User' }}</h2>
    <form [formGroup]="form" (ngSubmit)="save()">
      <mat-dialog-content>
        @if (!data) {
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Username</mat-label>
            <input matInput formControlName="username" />
          </mat-form-field>
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Email</mat-label>
            <input matInput type="email" formControlName="email" />
          </mat-form-field>
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Password</mat-label>
            <input matInput type="password" formControlName="password" />
          </mat-form-field>
        }

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Full Name</mat-label>
          <input matInput formControlName="fullName" />
        </mat-form-field>
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Phone (optional)</mat-label>
          <input matInput formControlName="phoneNumber" />
        </mat-form-field>
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Booth (optional)</mat-label>
          <input matInput formControlName="boothName" placeholder="e.g. Dhaka" />
        </mat-form-field>
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Role</mat-label>
          <mat-select formControlName="roleId">
            @for (r of roles(); track r.id) {
              <mat-option [value]="r.id">{{ r.name }}</mat-option>
            }
          </mat-select>
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
export class UserFormDialogComponent implements OnInit {
  protected readonly data = inject<UserDto | null>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<UserFormDialogComponent>);
  private readonly fb = inject(FormBuilder);
  private readonly usersService = inject(UsersService);
  private readonly rolesService = inject(RolesService);

  protected readonly saving = signal(false);
  protected readonly roles = signal<RoleDto[]>([]);

  protected readonly form = this.fb.nonNullable.group({
    username: [this.data?.username ?? '', this.data ? [] : [Validators.required, Validators.minLength(3)]],
    email: [this.data?.email ?? '', this.data ? [] : [Validators.required, Validators.email]],
    password: ['', this.data ? [] : [Validators.required, Validators.minLength(8)]],
    fullName: [this.data?.fullName ?? '', Validators.required],
    phoneNumber: [this.data?.phoneNumber ?? ''],
    boothName: [this.data?.boothName ?? ''],
    roleId: [this.data?.roleId ?? '', Validators.required],
  });

  ngOnInit(): void {
    this.rolesService.list().subscribe((roles) => this.roles.set(roles));
  }

  save(): void {
    if (this.form.invalid) return;
    this.saving.set(true);
    const value = this.form.getRawValue();

    const request$ = this.data
      ? this.usersService.update(this.data.id, {
          fullName: value.fullName,
          phoneNumber: value.phoneNumber || null,
          boothName: value.boothName || null,
          roleId: value.roleId,
        })
      : this.usersService.create({
          username: value.username,
          email: value.email,
          password: value.password,
          fullName: value.fullName,
          roleId: value.roleId,
          phoneNumber: value.phoneNumber || null,
          boothName: value.boothName || null,
        });

    request$.subscribe({
      next: () => this.dialogRef.close(true),
      error: () => this.saving.set(false),
    });
  }
}
