import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminService } from '@core/services/admin.service';
import { 
  AdminUserListItem, 
  AdminUserFilter, 
  UserStats 
} from '@core/models/admin.models';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './users.html',
  styleUrl: './users.scss'
})
export class AdminUsersComponent implements OnInit {
  private adminService = inject(AdminService);

  users: AdminUserListItem[] = [];
  stats: UserStats | null = null;
  roles: { id: string; name: string }[] = [];
  loading = false;
  error = '';

  // Filter state
  filter: AdminUserFilter = {
    page: 1,
    pageSize: 10,
    sortBy: 'CreatedAt',
    sortDescending: true,
    searchTerm: '',
    role: '',
    emailConfirmed: undefined,
    isLocked: undefined
  };

  totalCount = 0;
  totalPages = 0;

  // Selected user for detail/edit
  selectedUser: AdminUserListItem | null = null;
  showUserDetail = false;

  // Role management
  showRoleModal = false;
  selectedRole = '';
  roleAction: 'add' | 'remove' = 'add';

  // Lock management
  showLockModal = false;
  lockReason = '';

  ngOnInit(): void {
    this.loadStats();
    this.loadRoles();
    this.loadUsers();
  }

  loadStats(): void {
    this.adminService.getUserStats().subscribe({
      next: (response) => {
        if (response.isSuccess) {
          this.stats = response.result!;
        }
      },
      error: (err) => console.error('Failed to load stats', err)
    });
  }

  loadRoles(): void {
    this.adminService.getAllRoles().subscribe({
      next: (response) => {
        if (response.isSuccess) {
          this.roles = response.result as { id: string; name: string }[];
        }
      },
      error: (err) => console.error('Failed to load roles', err)
    });
  }

  loadUsers(): void {
    this.loading = true;
    this.error = '';

    this.adminService.getAllUsers(this.filter).subscribe({
      next: (response) => {
        this.loading = false;
        if (response.isSuccess && response.result) {
          const result = response.result as any;
          this.users = result.users;
          this.totalCount = result.totalCount;
          this.totalPages = result.totalPages;
        }
      },
      error: (err) => {
        this.loading = false;
        this.error = 'Failed to load users';
        console.error(err);
      }
    });
  }

  onSearch(): void {
    this.filter.page = 1;
    this.loadUsers();
  }

  onFilterChange(): void {
    this.filter.page = 1;
    this.loadUsers();
  }

  onPageChange(page: number): void {
    this.filter.page = page;
    this.loadUsers();
  }

  onSort(column: string): void {
    if (this.filter.sortBy === column) {
      this.filter.sortDescending = !this.filter.sortDescending;
    } else {
      this.filter.sortBy = column;
      this.filter.sortDescending = true;
    }
    this.loadUsers();
  }

  viewUser(user: AdminUserListItem): void {
    this.selectedUser = user;
    this.showUserDetail = true;
  }

  closeUserDetail(): void {
    this.showUserDetail = false;
    this.selectedUser = null;
  }

  // Role Management
  openRoleModal(user: AdminUserListItem, action: 'add' | 'remove'): void {
    this.selectedUser = user;
    this.roleAction = action;
    this.selectedRole = '';
    this.showRoleModal = true;
  }

  closeRoleModal(): void {
    this.showRoleModal = false;
    this.selectedRole = '';
  }

  confirmRoleChange(): void {
    if (!this.selectedUser || !this.selectedRole) return;

    const isAdd = this.roleAction === 'add';
    
    this.adminService.updateUserRole(
      this.selectedUser.id, 
      this.selectedRole, 
      isAdd
    ).subscribe({
      next: (response) => {
        if (response.isSuccess) {
          this.loadUsers();
          this.loadStats();
          this.closeRoleModal();
        }
      },
      error: (err) => console.error('Failed to update role', err)
    });
  }

  getAvailableRolesToAdd(user: AdminUserListItem): { id: string; name: string }[] {
    return this.roles.filter(r => !user.roles.includes(r.name!));
  }

  getAvailableRolesToRemove(user: AdminUserListItem): string[] {
    return user.roles;
  }

  // Lock Management
  openLockModal(user: AdminUserListItem): void {
    this.selectedUser = user;
    this.lockReason = '';
    this.showLockModal = true;
  }

  closeLockModal(): void {
    this.showLockModal = false;
    this.lockReason = '';
  }

  confirmLockToggle(): void {
    if (!this.selectedUser) return;

    const shouldLock = !this.selectedUser.isLocked;
    
    this.adminService.toggleUserLock(
      this.selectedUser.id,
      shouldLock,
      shouldLock ? this.lockReason : undefined
    ).subscribe({
      next: (response) => {
        if (response.isSuccess) {
          this.loadUsers();
          this.loadStats();
          this.closeLockModal();
          if (this.showUserDetail && this.selectedUser) {
            this.selectedUser.isLocked = shouldLock;
          }
        }
      },
      error: (err) => console.error('Failed to toggle lock', err)
    });
  }

  // Quick unlock without modal
  quickUnlock(user: AdminUserListItem): void {
    this.adminService.toggleUserLock(user.id, false).subscribe({
      next: (response) => {
        if (response.isSuccess) {
          this.loadUsers();
          this.loadStats();
        }
      },
      error: (err) => console.error('Failed to unlock user', err)
    });
  }

  get pages(): number[] {
    const pages: number[] = [];
    const start = Math.max(1, this.filter.page - 2);
    const end = Math.min(this.totalPages, start + 4);
    for (let i = start; i <= end; i++) {
      pages.push(i);
    }
    return pages;
  }

  formatDate(date: string | undefined): string {
    if (!date) return 'Never';
    return new Date(date).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }
}