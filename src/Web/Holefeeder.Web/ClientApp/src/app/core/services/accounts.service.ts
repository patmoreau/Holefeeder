import { HttpClient, HttpParams } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import { MessageService } from '@app/core/services';
import {
  Account,
  accountTypeMultiplier,
  categoryTypeMultiplier,
  MessageType,
  PagingInfo,
  Upcoming,
} from '@app/shared/models';
import { catchError, filter, map, Observable } from 'rxjs';
import { AccountAdapter, accountType } from '@app/core/adapters';
import { formatErrors, mapToPagingInfo } from '../utils/api.utils';
import { StateService } from './state.service';

const apiRoute = 'accounts';

interface AccountState {
  accounts: Account[];
  selected: Account | null;
}

const initialState: AccountState = {
  accounts: [],
  selected: null,
};

@Injectable({ providedIn: 'root' })
export class AccountsService extends StateService<AccountState> {
  inactiveAccounts$: Observable<Account[]> = this.select(state =>
    state.accounts.filter(x => x.inactive)
  );
  activeAccounts$: Observable<Account[]> = this.select(state =>
    state.accounts.filter(x => !x.inactive)
  );
  selectedAccount$: Observable<Account | null> = this.select(
    state => state.selected
  );

  constructor(
    private http: HttpClient,
    @Inject('BASE_API_URL') private apiUrl: string,
    private messages: MessageService,
    private adapter: AccountAdapter
  ) {
    super(initialState);

    this.messages.listen
      .pipe(
        filter(
          message =>
            message.type === MessageType.account ||
            message.type === MessageType.transaction
        )
      )
      .subscribe(() => {
        this.load();
      });

    this.load();
  }

  findById(id: string): Observable<Account | undefined> {
    return this.select(state =>
      state.accounts.find(account => account.id === id)
    );
  }

  selectAccount(account: Account) {
    this.setState({ selected: account });
  }

  findOneByIndex(index: number): Account | null {
    if (index < 0 || index > this.state.accounts.length) {
      return null;
    }
    return this.state.accounts[index];
  }

  private load() {
    this.getAll().subscribe(pagingInfo =>
      this.setState({ accounts: pagingInfo.items })
    );
  }

  private getAll(): Observable<PagingInfo<Account>> {
    const params = new HttpParams()
      .append('sort', '-favorite')
      .append('sort', 'name');

    return this.http
      .get<accountType[]>(`${this.apiUrl}/${apiRoute}`, {
        observe: 'response',
        params: params,
      })
      .pipe(
        map(resp => mapToPagingInfo(resp, this.adapter)),
        catchError(formatErrors)
      );
  }
  public getUpcomingBalance(account: Account, cashflows: Upcoming[]): number {
    return (
      account.balance +
      (cashflows
        ? cashflows
            .filter(cashflow => cashflow.account.id === account.id)
            .map(cashflow => {
              return (
                cashflow.amount *
                categoryTypeMultiplier(cashflow.category.type) *
                accountTypeMultiplier(account.type)
              );
            })
            .reduce((sum, current) => sum + current, 0)
        : 0)
    );
  }
}
