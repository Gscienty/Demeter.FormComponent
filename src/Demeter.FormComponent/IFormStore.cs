using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Linq;
using System.Collections.Generic;

namespace Demeter.FormComponent
{
    public interface IFormStore<TForm> : IDisposable where TForm : DemeterForm
    {
        Task<FormResult> CreateAsync(TForm form, CancellationToken cancellationToken);

        Task<FormResult> DeleteAsync(TForm form, CancellationToken cancellationToken);

        Task<FormResult> UpdateAsync(TForm form, CancellationToken cancellationToken);

        Task<TForm> FindByIdAsync(string id, CancellationToken cancellationToken);

        Task<TNewForm> QueryAsync<TNewForm>(
            string queryString,
            int count,
            Func<IQueryable<TForm>, TNewForm> queryAction,
            CancellationToken cancellationToken);

        Task<TNewForm> QueryAsync<TNewForm>(
            Func<IQueryable<TForm>, TNewForm> queryAction,
            CancellationToken cancellationToken);
    }
}