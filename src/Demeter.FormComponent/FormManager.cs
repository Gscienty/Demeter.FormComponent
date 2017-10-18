using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver.Linq;

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

        public async Task<FormResult> DeleteAsync(string id)
        {
            var form = await this.FindByIdAsync(id);
            if (form == null)
            {
                return FormResult.Failed(new FormError
                {
                    Code = "404",
                    Description = "not found"
                });
            }

            return await this.DeleteAsync(form);
        }

        public Task<FormResult> UpdateAsync(TForm entity)
            => this._formStore.UpdateAsync(entity, new CancellationToken());
            
        public Task<TNewForm> QueryAsync<TNewForm>(
            string queryString,
            int count,
            Func<IQueryable<TForm>, TNewForm> queryAction)
            => this._formStore.QueryAsync(queryString, count, queryAction, new CancellationToken());
        
        public Task<TNewForm> QueryAsync<TNewForm>(
            Func<IQueryable<TForm>, TNewForm> queryAction)
            => this._formStore.QueryAsync(queryAction, new CancellationToken());

        public Task<TForm> FindByIdAsync(string id)
            => this._formStore.FindByIdAsync(id, new CancellationToken());
    }
}