import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCardModule } from '@angular/material/card';
import { ProductService } from '@core/services/product.service';
import { AdminService } from '@core/services/admin.service';
import { Category } from '@core/models/product.models';
import { ToastrService } from 'ngx-toastr';
import { MatTooltipModule } from '@angular/material/tooltip';

@Component({
  selector: 'app-admin-categories',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ReactiveFormsModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatCardModule,
    MatTooltipModule
  ],
  templateUrl: './categories.html',
  styleUrls: ['./categories.scss']
})
export class AdminCategoriesComponent implements OnInit {
  private productService = inject(ProductService);
  private adminService = inject(AdminService);
  private toastr = inject(ToastrService);
  private fb = inject(FormBuilder);
  deleteImageFlag = false;

  categories: Category[] = [];
  loading = false;
  
  displayedColumns = ['image', 'name', 'description', 'displayOrder', 'actions'];
  
  showForm = false;
  editMode = false;
  categoryForm!: FormGroup;
  selectedImage: File | null = null;
  imagePreview: string | null = null;
  currentCategoryId: string | null = null;

  ngOnInit(): void {
    this.initForm();
    this.loadCategories();
  }

  private initForm(): void {
    this.categoryForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(3)]],
      description: ['', [Validators.required]],
      displayOrder: [0, [Validators.required, Validators.min(0)]]
    });
  }

  private loadCategories(): void {
    this.loading = true;

    this.productService.loadCategories().subscribe({
      next: (response) => {
        if (response.isSuccess && response.result) {
          this.categories = response.result.sort((a, b) => a.displayOrder - b.displayOrder);
        }
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  onAddNew(): void {
    this.showForm = true;
    this.editMode = false;
    this.currentCategoryId = null;
    this.categoryForm.reset({ displayOrder: this.categories.length });
    this.selectedImage = null;
    this.imagePreview = null;
  }

  onEdit(category: Category): void {
    this.showForm = true;
    this.editMode = true;
    this.currentCategoryId = category.id;
    
    this.categoryForm.patchValue({
      name: category.name,
      description: category.description,
      displayOrder: category.displayOrder
    });

    this.imagePreview = category.imageUrl || null;
    this.selectedImage = null;
    this.deleteImageFlag = false;
  }

  onImageSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files[0]) {
      const file = input.files[0];
      
      if (!file.type.startsWith('image/')) {
        this.toastr.error('Please select an image file');
        return;
      }

      if (file.size > 5 * 1024 * 1024) {
        this.toastr.error('Image size must be less than 5MB');
        return;
      }

      this.selectedImage = file;
      this.deleteImageFlag = false;

      const reader = new FileReader();
      reader.onload = () => {
        this.imagePreview = reader.result as string;
      };
      reader.readAsDataURL(file);
    }
  }

  onRemoveImage(): void {
    this.imagePreview = null;
    this.selectedImage = null;
    
    if (this.editMode && this.currentCategoryId) {
      this.deleteImageFlag = true;
    }
  }

  onSubmit(): void {
    if (this.categoryForm.invalid) {
      Object.keys(this.categoryForm.controls).forEach(key => {
        this.categoryForm.get(key)?.markAsTouched();
      });
      return;
    }

    this.loading = true;
    const { name, description, displayOrder } = this.categoryForm.value;

    if (this.editMode && this.currentCategoryId) {
      // Update
      if (this.deleteImageFlag && !this.selectedImage) {
        // Delete image first
        this.adminService.deleteCategoryImage(this.currentCategoryId).subscribe({
          next: () => {
            this.updateCategory(name, description, displayOrder, undefined);
          },
          error: () => {
            this.loading = false;
          }
        });
      } else {
        this.updateCategory(name, description, displayOrder, this.selectedImage || undefined);
      }
    } else {
      // Create
      this.adminService.createCategory(
        name,
        description,
        displayOrder,
        this.selectedImage || undefined
      ).subscribe({
        next: (response) => {
          if (response.isSuccess) {
            this.toastr.success('Category created successfully');
            this.showForm = false;
            this.loadCategories();
          }
          this.loading = false;
        },
        error: () => {
          this.loading = false;
        }
      });
    }
  }

  private updateCategory(name: string, description: string, displayOrder: number, image: File | undefined): void {
    this.adminService.updateCategory(
      this.currentCategoryId!,
      name,
      description,
      displayOrder,
      image
    ).subscribe({
      next: (response) => {
        if (response.isSuccess) {
          this.toastr.success('Category updated successfully');
          this.showForm = false;
          this.loadCategories();
        }
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  onCancel(): void {
    this.showForm = false;
    this.categoryForm.reset();
    this.selectedImage = null;
    this.imagePreview = null;
    this.deleteImageFlag = false;
  }

  onDelete(category: Category): void {
    if (!confirm(`Are you sure you want to delete "${category.name}"?`)) {
      return;
    }

    this.loading = true;

    this.adminService.deleteCategory(category.id).subscribe({
      next: (response) => {
        if (response.isSuccess) {
          this.toastr.success('Category deleted successfully');
          this.loadCategories();
        }
        this.loading = false;
      },
      error: (error) => {
        this.loading = false;
        // Error interceptor will show the toast
      }
    });
  }
}