import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminService } from '@core/services/admin.service';
import { 
  Coupon, 
  CreateCouponRequest, 
  UpdateCouponRequest,
  CouponFilter, 
  CouponStats 
} from '@core/models/admin.models';

@Component({
  selector: 'app-admin-coupons',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './coupons.html',
  styleUrl: './coupons.scss'
})
export class AdminCouponsComponent implements OnInit {
  private adminService = inject(AdminService);

  coupons: Coupon[] = [];
  stats: CouponStats | null = null;
  loading = false;
  error = '';
  successMessage = '';

  // Filter state
  filter: CouponFilter = {
    page: 1,
    pageSize: 10,
    searchTerm: '',
    isActive: undefined,
    isExpired: undefined
  };

  // Modal states
  showCreateModal = false;
  showEditModal = false;
  showDeleteModal = false;

  // Form data
  couponForm: CreateCouponRequest = this.getEmptyForm();
  editingCoupon: Coupon | null = null;
  deletingCoupon: Coupon | null = null;

  // Form validation
  formErrors: { [key: string]: string } = {};

  ngOnInit(): void {
    this.loadStats();
    this.loadCoupons();
  }

  getEmptyForm(): CreateCouponRequest {
    return {
      code: '',
      description: '',
      discountAmount: 0,
      discountType: 'Fixed',
      minAmount: 0,
      maxDiscount: undefined,
      expiryDate: undefined,
      isActive: true,
      maxUsageCount: 1000
    };
  }

  loadStats(): void {
    this.adminService.getCouponStats().subscribe({
      next: (response) => {
        if (response.isSuccess) {
          this.stats = response.result!;
        }
      },
      error: (err) => console.error('Failed to load stats', err)
    });
  }

  loadCoupons(): void {
    this.loading = true;
    this.error = '';

    this.adminService.getAllCoupons(this.filter).subscribe({
      next: (response) => {
        this.loading = false;
        if (response.isSuccess && response.result) {
          this.coupons = response.result;
        }
      },
      error: (err) => {
        this.loading = false;
        this.error = 'Failed to load coupons';
        console.error(err);
      }
    });
  }

  onSearch(): void {
    this.filter.page = 1;
    this.loadCoupons();
  }

  onFilterChange(): void {
    this.filter.page = 1;
    this.loadCoupons();
  }

  // Create Modal
  openCreateModal(): void {
    this.couponForm = this.getEmptyForm();
    this.formErrors = {};
    this.showCreateModal = true;
  }

  closeCreateModal(): void {
    this.showCreateModal = false;
    this.couponForm = this.getEmptyForm();
  }

  validateForm(): boolean {
    this.formErrors = {};

    if (!this.couponForm.code?.trim()) {
      this.formErrors['code'] = 'Coupon code is required';
    } else if (this.couponForm.code.length < 3) {
      this.formErrors['code'] = 'Code must be at least 3 characters';
    }

    if (this.couponForm.discountAmount <= 0) {
      this.formErrors['discountAmount'] = 'Discount amount must be greater than 0';
    }

    if (this.couponForm.discountType === 'Percentage' && this.couponForm.discountAmount > 100) {
      this.formErrors['discountAmount'] = 'Percentage cannot exceed 100%';
    }

    if (this.couponForm.minAmount < 0) {
      this.formErrors['minAmount'] = 'Minimum amount cannot be negative';
    }

    if (this.couponForm.maxUsageCount <= 0) {
      this.formErrors['maxUsageCount'] = 'Max usage must be greater than 0';
    }

    return Object.keys(this.formErrors).length === 0;
  }

  createCoupon(): void {
    if (!this.validateForm()) return;

    this.adminService.createCoupon(this.couponForm).subscribe({
      next: (response) => {
        if (response.isSuccess) {
          this.showSuccess('Coupon created successfully!');
          this.closeCreateModal();
          this.loadCoupons();
          this.loadStats();
        } else {
          this.formErrors['general'] = response.message || 'Failed to create coupon';
        }
      },
      error: (err) => {
        this.formErrors['general'] = err.error?.message || 'Failed to create coupon';
      }
    });
  }

  // Edit Modal
  openEditModal(coupon: Coupon): void {
    this.editingCoupon = coupon;
    this.couponForm = {
      code: coupon.code,
      description: coupon.description || '',
      discountAmount: coupon.discountAmount,
      discountType: coupon.discountType,
      minAmount: coupon.minAmount,
      maxDiscount: coupon.maxDiscount,
      expiryDate: coupon.expiryDate ? this.formatDateForInput(coupon.expiryDate) : undefined,
      isActive: coupon.isActive,
      maxUsageCount: coupon.maxUsageCount
    };
    this.formErrors = {};
    this.showEditModal = true;
  }

  closeEditModal(): void {
    this.showEditModal = false;
    this.editingCoupon = null;
    this.couponForm = this.getEmptyForm();
  }

  updateCoupon(): void {
    if (!this.validateForm() || !this.editingCoupon) return;

    const updateRequest: UpdateCouponRequest = {
      id: this.editingCoupon.id,
      ...this.couponForm
    };

    this.adminService.updateCoupon(updateRequest).subscribe({
      next: (response) => {
        if (response.isSuccess) {
          this.showSuccess('Coupon updated successfully!');
          this.closeEditModal();
          this.loadCoupons();
          this.loadStats();
        } else {
          this.formErrors['general'] = response.message || 'Failed to update coupon';
        }
      },
      error: (err) => {
        this.formErrors['general'] = err.error?.message || 'Failed to update coupon';
      }
    });
  }

  // Delete Modal
  openDeleteModal(coupon: Coupon): void {
    this.deletingCoupon = coupon;
    this.showDeleteModal = true;
  }

  closeDeleteModal(): void {
    this.showDeleteModal = false;
    this.deletingCoupon = null;
  }

  confirmDelete(): void {
    if (!this.deletingCoupon) return;

    this.adminService.deleteCoupon(this.deletingCoupon.id).subscribe({
      next: (response) => {
        if (response.isSuccess) {
          this.showSuccess('Coupon deleted successfully!');
          this.closeDeleteModal();
          this.loadCoupons();
          this.loadStats();
        }
      },
      error: (err) => console.error('Failed to delete coupon', err)
    });
  }

  // Toggle Status
  toggleStatus(coupon: Coupon): void {
    this.adminService.toggleCouponStatus(coupon.id).subscribe({
      next: (response) => {
        if (response.isSuccess) {
          coupon.isActive = !coupon.isActive;
          this.loadStats();
        }
      },
      error: (err) => console.error('Failed to toggle status', err)
    });
  }

  // Helpers
  showSuccess(message: string): void {
    this.successMessage = message;
    setTimeout(() => this.successMessage = '', 3000);
  }

  formatDateForInput(dateString: string): string {
    const date = new Date(dateString);
    return date.toISOString().split('T')[0];
  }

  formatDate(dateString: string | undefined): string {
    if (!dateString) return 'No expiry';
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  }

  isExpired(coupon: Coupon): boolean {
    if (!coupon.expiryDate) return false;
    return new Date(coupon.expiryDate) < new Date();
  }

  isFullyUsed(coupon: Coupon): boolean {
    return coupon.usedCount >= coupon.maxUsageCount;
  }

  getDiscountDisplay(coupon: Coupon): string {
    if (coupon.discountType === 'Percentage') {
      return `${coupon.discountAmount}%`;
    }
    return `$${coupon.discountAmount.toFixed(2)}`;
  }

  getStatusClass(coupon: Coupon): string {
    if (!coupon.isActive) return 'status-inactive';
    if (this.isExpired(coupon)) return 'status-expired';
    if (this.isFullyUsed(coupon)) return 'status-depleted';
    return 'status-active';
  }

  getStatusText(coupon: Coupon): string {
    if (!coupon.isActive) return 'Inactive';
    if (this.isExpired(coupon)) return 'Expired';
    if (this.isFullyUsed(coupon)) return 'Depleted';
    return 'Active';
  }
}