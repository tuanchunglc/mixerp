﻿/********************************************************************************
Copyright (C) MixERP Inc. (http://mixof.org).

This file is part of MixERP.

MixERP is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, version 2 of the License.

MixERP is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with MixERP.  If not, see <http://www.gnu.org/licenses/>.
***********************************************************************************/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Web.Script.Services;
using System.Web.Services;
using System.Web.UI.WebControls;
using MixERP.Net.ApplicationState.Cache;
using MixERP.Net.Common.Extensions;
using MixERP.Net.Core.Modules.Finance.Data.Helpers;
using MixERP.Net.Entities.Core;
using MixERP.Net.Entities.Models.Transactions;
using MixERP.Net.Entities.Transactions;
using MixERP.Net.Framework;
using MixERP.Net.i18n.Resources;
using MixERP.Net.WebControls.Common.Helpers;

namespace MixERP.Net.Core.Modules.Finance.Services.DayOperation
{
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ToolboxItem(false)]
    [ScriptService]
    public class EOY : WebService
    {
        [WebMethod]
        public List<DbGetPlAppropriationDataResult> GetAppropriationData()
        {
            string catalog = AppUsers.GetCurrentUserDB();
            int officeId = AppUsers.GetCurrent().View.OfficeId.ToInt();
            bool isAdmin = AppUsers.GetCurrent().View.IsAdmin.ToBool();

            if (!isAdmin)
            {
                throw new MixERPException(Warnings.AccessIsDenied);
            }

            return Data.DayOperation.EOY.GetPlAppropriationData(catalog, officeId).ToList();
        }

        [WebMethod]
        public bool PostPLAppropriation(string referenceNumber, int costCenterId, string accountNumber, string statementReference)
        {
            bool isAdmin = AppUsers.GetCurrent().View.IsAdmin.ToBool();

            if (!isAdmin)
            {
                throw new MixERPException(Warnings.AccessIsDenied);
            }

            if (string.IsNullOrWhiteSpace(accountNumber))
            {
                throw new MixERPException(Warnings.InvalidAccount);
            }

            string catalog = AppUsers.GetCurrentUserDB();
            int officeId = AppUsers.GetCurrent().View.OfficeId.ToInt();
            int userId = AppUsers.GetCurrent().View.UserId.ToInt();
            long loginId = AppUsers.GetCurrent().View.LoginId.ToLong();
            string currencyCode = AppUsers.GetCurrent().View.CurrencyCode;

            DateTime valueDate = DatePersister.GetFrequencyDates(catalog, officeId).Today;
            DateTime bookDate = valueDate;

            Collection<JournalDetail> details = new Collection<JournalDetail>();

            foreach (var item in this.GetAppropriationData())
            {
                details.Add(new JournalDetail
                {
                    Account = item.AccountName,
                    AccountNumber = item.AccountNumber,
                    Credit = item.Credit,
                    Debit = item.Debit,
                    CashRepositoryCode= string.Empty,
                    CurrencyCode = currencyCode,
                    ExchangeRate = 1,
                    LocalCurrencyCredit = item.Credit,
                    LocalCurrencyDebit = item.Debit,
                    StatementReference = statementReference
                });

                //Reverse side to balance the journal
                decimal debit = item.Credit;
                decimal credit = item.Debit;


                details.Add(new JournalDetail
                {
                    Account = string.Empty,
                    AccountNumber = accountNumber,
                    Credit = credit,
                    Debit = debit,
                    CashRepositoryCode = string.Empty,
                    CurrencyCode = currencyCode,
                    ExchangeRate = 1,
                    LocalCurrencyCredit = credit,
                    LocalCurrencyDebit = debit,
                    StatementReference = statementReference
                });

            }

            Transaction.Add(catalog, valueDate, bookDate, officeId, userId, loginId, costCenterId,
                referenceNumber, details, new Collection<Attachment>());

            return true;
        }

        [WebMethod]
        public Collection<ListItem> PLAppropriationAccounts()
        {
            if (AppUsers.GetCurrent().View.IsAdmin.ToBool())
            {
                return GetValues(AccountHelper.GetPLAppropriationAccounts(AppUsers.GetCurrentUserDB()));
            }

            return null;
        }

        private static Collection<ListItem> GetValues(IEnumerable<Account> accounts)
        {
            Collection<ListItem> values = new Collection<ListItem>();

            foreach (Account account in accounts)
            {
                values.Add(new ListItem(account.AccountName, account.AccountNumber));
            }

            return values;
        }

    }
}