import { ActionReducerMap, MetaReducer } from '@ngrx/store';
import { authReducer, AuthState } from './auth/auth.reducer';
import { cartReducer, CartState } from './cart/cart.reducer';
import { productReducer, ProductState } from './product/product.reducer';

export interface AppState {
  auth: AuthState;
  cart: CartState;
  product: ProductState;
}

export const appReducers: ActionReducerMap<AppState> = {
  auth: authReducer,
  cart: cartReducer,
  product: productReducer,
};

export const metaReducers: MetaReducer<AppState>[] = [];
