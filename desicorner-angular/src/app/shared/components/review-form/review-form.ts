import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { Review, CreateReviewRequest, UpdateReviewRequest } from '@core/models/review.models';
import { StarRatingComponent } from '../star-rating/star-rating';

@Component({
  selector: 'app-review-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    StarRatingComponent
  ],
  template: `
    <form [formGroup]="form" (ngSubmit)="onSubmit()" class="review-form">
      <h3>{{ editMode ? 'Edit Your Review' : 'Write a Review' }}</h3>

      <div class="rating-input">
        <label>Your Rating *</label>
        <app-star-rating
          [rating]="form.get('rating')?.value || 0"
          [readonly]="false"
          (ratingChange)="onRatingChange($event)">
        </app-star-rating>
        @if (form.get('rating')?.touched && form.get('rating')?.hasError('required')) {
          <span class="error">Please select a rating</span>
        }
        @if (form.get('rating')?.hasError('min')) {
          <span class="error">Rating must be at least 1 star</span>
        }
      </div>

      <mat-form-field appearance="outline" class="full-width">
        <mat-label>Review Title (optional)</mat-label>
        <input matInput formControlName="title" maxlength="200" placeholder="Summarize your experience">
        <mat-hint align="end">{{ form.get('title')?.value?.length || 0 }}/200</mat-hint>
      </mat-form-field>

      <mat-form-field appearance="outline" class="full-width">
        <mat-label>Your Review (optional)</mat-label>
        <textarea 
          matInput 
          formControlName="comment" 
          rows="4"
          maxlength="2000"
          placeholder="Share your experience with this dish...">
        </textarea>
        <mat-hint align="end">{{ form.get('comment')?.value?.length || 0 }}/2000</mat-hint>
      </mat-form-field>

      <div class="form-actions">
        <button 
          mat-button 
          type="button" 
          (click)="onCancel()">
          Cancel
        </button>
        <button 
          mat-raised-button 
          color="primary" 
          type="submit"
          [disabled]="form.invalid || submitting">
          {{ submitting ? 'Submitting...' : (editMode ? 'Update Review' : 'Submit Review') }}
        </button>
      </div>
    </form>
  `,
  styles: [`
    .review-form {
      padding: 16px;
      background: #f9f9f9;
      border-radius: 8px;
    }

    h3 {
      margin: 0 0 16px 0;
      color: #333;
    }

    .rating-input {
      margin-bottom: 16px;
    }

    .rating-input label {
      display: block;
      margin-bottom: 8px;
      font-weight: 500;
      color: #333;
    }

    .error {
      display: block;
      color: #f44336;
      font-size: 0.75rem;
      margin-top: 4px;
    }

    .full-width {
      width: 100%;
      margin-bottom: 16px;
    }

    .form-actions {
      display: flex;
      justify-content: flex-end;
      gap: 12px;
    }
  `]
})
export class ReviewFormComponent implements OnInit {
  @Input() productId!: string;
  @Input() existingReview?: Review;
  @Input() submitting = false;
  @Output() submitReview = new EventEmitter<CreateReviewRequest | UpdateReviewRequest>();
  @Output() cancel = new EventEmitter<void>();

  form!: FormGroup;

  get editMode(): boolean {
    return !!this.existingReview;
  }

  constructor(private fb: FormBuilder) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      rating: [this.existingReview?.rating || 0, [Validators.required, Validators.min(1), Validators.max(5)]],
      title: [this.existingReview?.title || '', Validators.maxLength(200)],
      comment: [this.existingReview?.comment || '', Validators.maxLength(2000)]
    });
  }

  onRatingChange(rating: number): void {
    this.form.patchValue({ rating });
    this.form.get('rating')?.markAsTouched();
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const formValue = this.form.value;

    if (this.editMode && this.existingReview) {
      const updateRequest: UpdateReviewRequest = {
        id: this.existingReview.id,
        rating: formValue.rating,
        title: formValue.title || undefined,
        comment: formValue.comment || undefined
      };
      this.submitReview.emit(updateRequest);
    } else {
      const createRequest: CreateReviewRequest = {
        productId: this.productId,
        rating: formValue.rating,
        title: formValue.title || undefined,
        comment: formValue.comment || undefined
      };
      this.submitReview.emit(createRequest);
    }
  }

  onCancel(): void {
    this.cancel.emit();
  }
}