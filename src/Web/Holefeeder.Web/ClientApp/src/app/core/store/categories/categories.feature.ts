import { createFeature, createReducer, createSelector, on } from '@ngrx/store';
import { createEntityAdapter, EntityState } from '@ngrx/entity';
import { CategoriesActions } from './categories.actions';
import { Category } from '@app/shared/models';
import { CallState } from '@app/core/store/call-state.type';

export interface CategoriesState extends EntityState<Category> {
  callState: CallState;
  error: string;
}

// adapter
export function selectId(a: Category): string {
  return a.id;
}

export function sortByFavoriteAndName(ob1: Category, ob2: Category): number {
  return +ob2.favorite - +ob1.favorite || ob1.name.localeCompare(ob2.name);
}

export const categoryAdapter = createEntityAdapter<Category>({
  selectId: selectId,
  sortComparer: sortByFavoriteAndName,
});

export const initialCategoriesState: CategoriesState = categoryAdapter.getInitialState({
  callState: 'init',
  error: '',
});

export const CategoriesFeature = createFeature({
  name: 'categories',
  reducer: createReducer(
    initialCategoriesState,
    on(CategoriesActions.loadCategories, state => ({
      ...state,
      callState: 'loading' as const,
    })),
    on(CategoriesActions.loadCategoriesSuccess, (state, { categories }) => ({
      ...state,
      ...categoryAdapter.setAll(categories.concat(), state),
      callState: 'loaded' as const,
    })),
    on(CategoriesActions.loadCategoriesFailure, (state, { error }) => ({
      ...state,
      error,
      callState: 'loaded' as const,
    })),
    on(CategoriesActions.clearCategories, state => ({
      ...state,
      ...categoryAdapter.removeAll(state),
      callState: 'init' as const,
    }))
  ),
  extraSelectors: ({
    selectCategoriesState,
    selectEntities,
    selectCallState,
  }) => ({
    ...categoryAdapter.getSelectors(selectCategoriesState),
    selectByIndex: (index: number) =>
      createSelector(
        selectEntities,
        categories => Object.values(categories)[index]
      ),
    selectIsLoading: createSelector(
      selectCallState,
      callState => callState === 'loading'
    ),
  }),
});
