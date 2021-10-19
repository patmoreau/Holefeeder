import { Injectable } from '@angular/core';
import { ApiService } from '@app/shared/services/api.service';
import { HttpParams } from '@angular/common/http';
import { IAccount, accountDetailFromServer } from '../interfaces/account.interface';
import { map } from 'rxjs/operators';
import { IPagingInfo } from '../interfaces/paging-info.interface';
import { ModifyAccountCommand } from '../accounts/modify-account-command.model';
import { OpenAccountCommand } from '../accounts/open-account-command.model';

@Injectable()
export class AccountsService {
  private basePath = 'api/v2/budgeting-api/accounts';

  constructor(private api: ApiService) {}

  find(
    offset: number,
    limit: number,
    sort: string[],
    filter: string[]
  ): Promise<IPagingInfo<IAccount>> {
    let params = new HttpParams();
    if (offset) {
      params = params.set('offset', `${offset}`);
    }
    if (limit) {
      params = params.set('limit', `${limit}`);
    }
    if (sort) {
      sort.forEach(element => {
        params = params.append('sort', `${element}`);
      });
    }
    if (filter) {
      filter.forEach(element => {
        params = params.append('filter', `${element}`);
      });
    }
    return this.api
      .get(`${this.basePath}/get-accounts`, params)
      .pipe(
        map(data =>
          Object.assign(
            {},
            {
              totalCount: data.totalCount,
              items: Object.assign(
                [],
                data.items.map(accountDetailFromServer)
              )
            }
          )
        )
      )
      .toPromise();
  }

  findOneByIdWithDetails(id: number | string): Promise<IAccount> {
    return this.api
      .get(`${this.basePath}/${id}`)
      .pipe(
        map(accountDetailFromServer)
      )
      .toPromise();
  }

  open(account: OpenAccountCommand): Promise<void> {
    return this.api.post(`${this.basePath}/open-account`, account)
         .toPromise();
  }

  modify(account: ModifyAccountCommand): Promise<void> {
    return this.api.post(`${this.basePath}/modify-account`, account)
         .toPromise();
  }
}
