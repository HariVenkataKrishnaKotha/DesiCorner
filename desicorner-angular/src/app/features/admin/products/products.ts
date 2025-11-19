import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { ProductService } from '@core/services/product.service';
import { AdminService } from '@core/services/admin.service';
import { Product, Category, CreateProductRequest } from '@core/models/product.models';
import { ToastrService } from 'ngx-toastr';
import { MatTooltipModule } from '@angular/material/tooltip';

@Component({
  selector: 'app-admin-products',
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
    MatSelectModule,
    MatCheckboxModule,
    MatCardModule,
    MatChipsModule,
    MatTooltipModule
  ],
  templateUrl: './products.html',
  styleUrls: ['./products.scss']
})
export class AdminProductsComponent implements OnInit {
  private productService = inject(ProductService);
  private adminService = inject(AdminService);
  private toastr = inject(ToastrService);
  private dialog = inject(MatDialog);
  private fb = inject(FormBuilder);
  deleteImageFlag = false;

  products: Product[] = [];
  categories: Category[] = [];
  loading = false;
  
  displayedColumns = ['image', 'name', 'category', 'price', 'status', 'actions'];
  
  showForm = false;
  editMode = false;
  productForm!: FormGroup;
  selectedImage: File | null = null;
  imagePreview: string | null = null;
  currentProductId: string | null = null;

  ngOnInit(): void {
    this.initForm();
    this.loadData();
  }

  private initForm(): void {
    this.productForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(3)]],
      description: ['', [Validators.required]],
      price: [0, [Validators.required, Validators.min(0.01)]],
      categoryId: ['', Validators.required],
      isAvailable: [true],
      isVegetarian: [false],
      isVegan: [false],
      isSpicy: [false],
      spiceLevel: [0, [Validators.min(0), Validators.max(5)]],
      allergens: [''],
      preparationTime: [0, [Validators.required, Validators.min(1)]]
    });
  }

  private loadData(): void {
    this.loading = true;

    this.productService.loadProducts().subscribe({
      next: (response) => {
        if (response.isSuccess && response.result) {
          this.products = response.result;
        }
      }
    });

    this.productService.loadCategories().subscribe({
      next: (response) => {
        if (response.isSuccess && response.result) {
          this.categories = response.result;
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
    this.currentProductId = null;
    this.productForm.reset({
      isAvailable: true,
      isVegetarian: false,
      isVegan: false,
      isSpicy: false,
      spiceLevel: 0
    });
    this.selectedImage = null;
    this.imagePreview = null;
  }

  onEdit(product: Product): void {
    this.showForm = true;
    this.editMode = true;
    this.currentProductId = product.id;
    
    this.productForm.patchValue({
      name: product.name,
      description: product.description,
      price: product.price,
      categoryId: product.categoryId,
      isAvailable: product.isAvailable,
      isVegetarian: product.isVegetarian,
      isVegan: product.isVegan,
      isSpicy: product.isSpicy,
      spiceLevel: product.spiceLevel,
      allergens: product.allergens,
      preparationTime: product.preparationTime
    });

    this.imagePreview = product.imageUrl || null;
    this.selectedImage = null;
    this.deleteImageFlag = false;
  }

  onImageSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files[0]) {
      const file = input.files[0];
      
      // Validate file type
      if (!file.type.startsWith('image/')) {
        this.toastr.error('Please select an image file');
        return;
      }

      // Validate file size (5MB)
      if (file.size > 5 * 1024 * 1024) {
        this.toastr.error('Image size must be less than 5MB');
        return;
      }

      this.selectedImage = file;
      this.deleteImageFlag = false;

      // Preview
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
    
    // If we're editing and there was an existing image, mark it for deletion
    if (this.editMode && this.currentProductId) {
      this.deleteImageFlag = true;
    }
  }

  onSubmit(): void {
    if (this.productForm.invalid) {
      Object.keys(this.productForm.controls).forEach(key => {
        this.productForm.get(key)?.markAsTouched();
      });
      return;
    }

    this.loading = true;

    if (this.editMode && this.currentProductId) {
      // Update
      const productData = {
        id: this.currentProductId,
        ...this.productForm.value
      };

      // If user clicked X to delete image
      if (this.deleteImageFlag && !this.selectedImage) {
        // First delete the image
        this.adminService.deleteProductImage(this.currentProductId).subscribe({
          next: () => {
            // Then update product without image
            this.updateProduct(productData, undefined);
          },
          error: () => {
            this.loading = false;
          }
        });
      } else {
        // Normal update
        this.updateProduct(productData, this.selectedImage || undefined);
      }
    } else {
      // Create
      const productData: CreateProductRequest = this.productForm.value;

      this.adminService.createProduct(productData, this.selectedImage || undefined).subscribe({
        next: (response) => {
          if (response.isSuccess) {
            this.toastr.success('Product created successfully');
            this.showForm = false;
            this.loadData();
          }
          this.loading = false;
        },
        error: () => {
          this.loading = false;
        }
      });
    }
  }

  private updateProduct(productData: any, image: File | undefined): void {
    this.adminService.updateProduct(this.currentProductId!, productData, image).subscribe({
      next: (response) => {
        if (response.isSuccess) {
          this.toastr.success('Product updated successfully');
          this.showForm = false;
          this.loadData();
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
    this.productForm.reset();
    this.selectedImage = null;
    this.imagePreview = null;
    this.deleteImageFlag = false;
  }

  onDelete(product: Product): void {
    if (!confirm(`Are you sure you want to delete "${product.name}"?`)) {
      return;
    }

    this.loading = true;

    this.adminService.deleteProduct(product.id).subscribe({
      next: (response) => {
        if (response.isSuccess) {
          this.toastr.success('Product deleted successfully');
          this.loadData();
        }
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  getCategoryName(categoryId: string): string {
    return this.categories.find(c => c.id === categoryId)?.name || 'Unknown';
  }
}