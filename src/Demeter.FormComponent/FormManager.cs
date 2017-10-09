using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;

namespace Demeter.FormComponent
{
    public class FormManager<TForm> where TForm : DemeterForm, new()
    {
        private IFormStore<TForm> _formStore;

        public FormManager(IServiceProvider provider)
        {
            this._formStore = provider.GetService<IFormStore<TForm>>();
        }

        public Task<FormResult> CreateAsync(TForm entity)
            => this._formStore.CreateAsync(entity, new CancellationToken());

        public Task<FormResult> DeleteAsync(TForm entity)
            => this._formStore.DeleteAsync(entity, new CancellationToken());

        public Task<FormResult> UpdateAsync(TForm entity)
            => this._formStore.UpdateAsync(entity, new CancellationToken());
            
        public Task<IEnumerable<TForm>> QueryAsync(string queryString, int count)
            => this._formStore.QueryAsync(queryString, count, new CancellationToken());

        public Task<IEnumerable<TForm>> LastestAsync(int count, CancellationToken cancellationToken)
            => this._formStore.LastestAsync(count, new CancellationToken());
    }
}